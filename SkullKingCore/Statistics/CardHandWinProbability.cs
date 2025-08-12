//#define COMPILE_OLDER_METHODS

using SkullKingCore.Cards.Base;
using SkullKingCore.Core;
using SkullKingCore.Extensions;
using SkullKingCore.GameDefinitions;
using SkullKingCore.Logging;
using System.Collections.Concurrent;

namespace SkullKingCore.Statistics
{

    /// <summary>
    /// Uses Monte Carlo simulation to calculate the Win Probability of a card hand
    /// </summary>
    public class CardHandWinProbability
    {

        private static readonly Random rng = new Random();

        public List<BaseCard> CardHandToTest { get; private set; }

        public int PlayerCount { get; private set; }

        public int NSimulations { get; private set; }

        public int Rounds { get; private set; }

        public double? WinRate { get; private set; } = default(double);

        // Tracks how many times you win exactly k tricks (k -> frequency)
        public ConcurrentDictionary<int, int>? WinsDistribution { get; private set; }

        public double? WinPercentage
        {
            get
            {
                return WinRate * 100;
            }
        }

        public double? TricksWon { get; private set; }

        public CardHandWinProbability(List<BaseCard> cardHandToTest, int playerCount, int nSimulations)
        {
            CardHandToTest = cardHandToTest;
            PlayerCount = playerCount;
            NSimulations = nSimulations;
            Rounds = cardHandToTest.Count;
        }

        /// <summary>
        /// Monte Carlo simulation to estimate the number of tricks you will win,
        /// assuming opponents play randomly ("dumb" play, no strategy).
        /// 
        /// Key improvements over original version:
        /// 1. <b>Randomized starting lead</b> each simulation (removes bias from always starting first).
        /// 2. <b>Random card selection each trick</b> from your hand (removes fixed play order bias).
        /// 3. <b>Full hand dealing</b> to opponents before play (models real game state instead of drawing mid-play).
        /// 4. Tracks <b>full distribution of tricks won</b>, not just average.
        /// 5. Optimized to reduce LINQ/allocations in the inner loop for better performance at high simulation counts.
        /// </summary>
        /// <param name="maxDegreeOfParallelism">Maximum parallel threads to use for simulations.</param>
        public void Calculate(int maxDegreeOfParallelism = 8)
        {
            // Total wins across all simulations
            int winsTotal = 0;

            // Tracks how many times you win exactly k tricks (k -> frequency)
            //ConcurrentDictionary<int, int> winsDistribution = new ConcurrentDictionary<int, int>();
            WinsDistribution = new ConcurrentDictionary<int, int>();

            // Parallel execution settings
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };

            // Run simulations in parallel
            Parallel.For(0, NSimulations, options, simulationCounter =>
            {
                // Thread-local random generator
                var random = new Random(Guid.NewGuid().GetHashCode());

                // Copy your hand for this simulation (CardsToTest = your known hand)
                List<BaseCard> playerHand = new List<BaseCard>(CardHandToTest);

                // Create and shuffle full deck
                List<BaseCard> deck = Deck.CreateDeck();
                deck.Shuffle(random);

                // Remove your cards from the deck to get opponent's card pool
                List<BaseCard> deckWithoutOwnCards = new List<BaseCard>(deck);
                RemoveMatchesOneByOne(deckWithoutOwnCards, CardHandToTest);

                // Number of opponents (excluding you)
                int opponentCount = PlayerCount - 1;

                // Preallocate lists for opponent hands
                List<BaseCard>[] opponentHands = new List<BaseCard>[opponentCount];
                for (int i = 0; i < opponentCount; i++)
                    opponentHands[i] = new List<BaseCard>();

                // Deal remaining cards evenly to opponents
                // This ensures each opponent has a fixed hand, like in a real round
                for (int i = 0; i < deckWithoutOwnCards.Count; i++)
                {
                    opponentHands[i % opponentCount].Add(deckWithoutOwnCards[i]);
                }

                // Pick a random player to lead the first trick
                int leadPlayer = random.Next(PlayerCount);

                int localWins = 0; // Your wins in this simulation

                // Play until your hand is empty (end of round)
                while (playerHand.Count > 0)
                {
                    // Store cards played in this trick
                    List<BaseCard> trickToTest = new List<BaseCard>(PlayerCount);

                    // Track which player plays which card
                    BaseCard[] playedCards = new BaseCard[PlayerCount];

                    // Players act in order starting from the current lead
                    for (int i = 0; i < PlayerCount; i++)
                    {
                        int currentPlayer = (leadPlayer + i) % PlayerCount;
                        BaseCard cardPlayed;

                        if (currentPlayer == 0)
                        {
                            // You play a random card from your current hand
                            int myIdx = random.Next(playerHand.Count);
                            cardPlayed = playerHand[myIdx];
                            playerHand.RemoveAt(myIdx);
                        }
                        else
                        {
                            // Opponent plays a random card from THEIR hand
                            var oppHand = opponentHands[currentPlayer - 1];
                            int idx = random.Next(oppHand.Count);
                            cardPlayed = oppHand[idx];
                            oppHand.RemoveAt(idx);
                        }

                        trickToTest.Add(cardPlayed);
                        playedCards[currentPlayer] = cardPlayed;
                    }

                    // Determine trick winner
                    BaseCard? winnerCard = TrickResolver.DetermineTrickWinnerCard(trickToTest);

                    // If trick is a draw, keep the same lead and continue
                    if (winnerCard == null)
                        continue;

                    // Find which player played the winning card (loop to avoid LINQ overhead)
                    int winnerPlayer = -1;
                    for (int p = 0; p < PlayerCount; p++)
                    {
                        BaseCard pc = playedCards[p];
                        if (pc.CardType == winnerCard.CardType && pc.GenericValue == winnerCard.GenericValue)
                        {
                            winnerPlayer = p;
                            break;
                        }
                    }

                    // Count win if you were the winner
                    if (winnerPlayer == 0)
                        localWins++;

                    // Winner of this trick leads the next trick
                    leadPlayer = winnerPlayer;
                }

                // Thread-safe total wins accumulation
                Interlocked.Add(ref winsTotal, localWins);

                // Thread-safe update of win distribution
                WinsDistribution.AddOrUpdate(localWins, 1, (_, count) => count + 1);
            });

            // Average win rate (normalized over total tricks played)
            WinRate = (double)winsTotal / (NSimulations * CardHandToTest.Count);

            // Estimated number of tricks you’ll win in the full round
            TricksWon = WinRate * Rounds;
        }

        public static void RemoveMatchesOneByOne(List<BaseCard> bigList, List<BaseCard> smallList)
        {
            foreach (var item in smallList)
            {
                var match = bigList.FirstOrDefault(b => b.CardType == item.CardType && b.GenericValue == item.GenericValue);
                if (match != null)
                {
                    bigList.Remove(match);
                }
            }
        }

        public void PrintResults(int decimalPlaces)
        {
            string headerCardType = "Card Type";
            string headerSubType = "Sub Type";

            int maxLengthCardType = Math.Max(headerCardType.Length, CardHandToTest.Max(obj => obj.CardType.ToString().Length));
            int maxLengthSubType = Math.Max(headerSubType.Length, CardHandToTest.Max(obj => obj.SubType().Length));

            string separator = "+" +
                new string('-', maxLengthCardType + 2) + "+" +
                new string('-', maxLengthSubType + 2) + "+";

            string headerLine = $"| {headerCardType.PadRight(maxLengthCardType)} " +
                                $"| {headerSubType.PadRight(maxLengthSubType)} |";

            Logger.Instance.WriteToConsoleAndLog(separator);
            Logger.Instance.WriteToConsoleAndLog(headerLine);
            Logger.Instance.WriteToConsoleAndLog(separator);

            foreach (BaseCard card in CardHandToTest)
            {
                string line = $"| {card.CardType.ToString().PadRight(maxLengthCardType)} " +
                              $"| {card.SubType().PadRight(maxLengthSubType)} |";
                Logger.Instance.WriteToConsoleAndLog(line);
            }

            Logger.Instance.WriteToConsoleAndLog(separator);

            string format = $"F{decimalPlaces}";

            Logger.Instance.WriteToConsoleAndLog(
                $"Hand Win Probability: {(WinPercentage.HasValue ? WinPercentage.Value.ToString(format) : "N/A")} %");

            Logger.Instance.WriteToConsoleAndLog(
                $"Theoretical won tricks: {(TricksWon.HasValue ? TricksWon.Value.ToString(format) : "N/A")} / {Rounds}");

            // Wins Distribution Table
            if (WinsDistribution != null && WinsDistribution.Count > 0)
            {
                Logger.Instance.WriteToConsoleAndLog("");
                Logger.Instance.WriteToConsoleAndLog("Wins Distribution:");

                string headerTricksWon = "Tricks Won";
                string headerNSimulations = "N Simulations";
                string headerPctSim = "% of Sims";
                string headerPctWins = "% of Wins";

                var ordered = WinsDistribution.OrderBy(k => k.Key).ToList();

                // Only count "real wins" (1+ tricks)
                int totalWins = WinsDistribution
                    .Where(kvp => kvp.Key > 0)
                    .Sum(kvp => kvp.Value);

                var rows = ordered.Select(kvp =>
                {
                    string keyStr = kvp.Key.ToString();
                    string winsStr = kvp.Value.ToString("N0"); // thousands separator
                    string pctSimStr = (NSimulations > 0)
                        ? ((double)kvp.Value / NSimulations * 100).ToString($"F{decimalPlaces}") + "%"
                        : "N/A";
                    string pctWinsStr = (totalWins > 0 && kvp.Key > 0)
                        ? ((double)kvp.Value / totalWins * 100).ToString($"F{decimalPlaces}") + "%"
                        : (kvp.Key == 0 ? "0.00%" : "N/A");

                    return new
                    {
                        KeyStr = keyStr,
                        WinsStr = winsStr,
                        PctSimStr = pctSimStr,
                        PctWinsStr = pctWinsStr
                    };
                }).ToList();

                int maxLengthTricksWon = Math.Max(headerTricksWon.Length, rows.Max(r => r.KeyStr.Length));
                int maxLengthNSimulation = Math.Max(headerNSimulations.Length, rows.Max(r => r.WinsStr.Length));
                int maxLengthPctSim = Math.Max(headerPctSim.Length, rows.Max(r => r.PctSimStr.Length));
                int maxLengthPctWins = Math.Max(headerPctWins.Length, rows.Max(r => r.PctWinsStr.Length));

                string separatorDist = "+" +
                    new string('-', maxLengthTricksWon + 2) + "+" +
                    new string('-', maxLengthNSimulation + 2) + "+" +
                    new string('-', maxLengthPctSim + 2) + "+" +
                    new string('-', maxLengthPctWins + 2) + "+";

                string headerLineDist = $"| {headerTricksWon.PadRight(maxLengthTricksWon)} " +
                                        $"| {headerNSimulations.PadLeft(maxLengthNSimulation)} " +
                                        $"| {headerPctSim.PadLeft(maxLengthPctSim)} " +
                                        $"| {headerPctWins.PadLeft(maxLengthPctWins)} |";

                Logger.Instance.WriteToConsoleAndLog(separatorDist);
                Logger.Instance.WriteToConsoleAndLog(headerLineDist);
                Logger.Instance.WriteToConsoleAndLog(separatorDist);

                foreach (var r in rows)
                {
                    string line = $"| {r.KeyStr.PadRight(maxLengthTricksWon)} " +
                                  $"| {r.WinsStr.PadLeft(maxLengthNSimulation)} " +
                                  $"| {r.PctSimStr.PadLeft(maxLengthPctSim)} " +
                                  $"| {r.PctWinsStr.PadLeft(maxLengthPctWins)} |";
                    Logger.Instance.WriteToConsoleAndLog(line);
                }

                Logger.Instance.WriteToConsoleAndLog(separatorDist);
            }

        }

        #region Older versions of the caluclation methods

#if COMPILE_OLDER_METHODS

        [Obsolete]
        public void CalculateV2(int maxDegreeOfParallelism = 8)
        {
            int wins = 0;

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };

            Parallel.For(0, NSimulations, options, simulationCounter =>
            {
                var random = new Random(Guid.NewGuid().GetHashCode());

                // Copy and shuffle your hand
                List<BaseCard> playerHand = CardsToTest.ToList();
                playerHand.Shuffle(random);

                // Prepare and shuffle full deck
                List<BaseCard> deck = Deck.CreateDeck();
                deck.Shuffle(random);

                // Remove your cards from deck
                List<BaseCard> deckWithoutOwnCards = deck.ToList();
                RemoveMatchesOneByOne(deckWithoutOwnCards, CardsToTest);

                // Number of opponents (excluding you)
                int opponentCount = PlayerCount - 1;

                // Deal cards evenly to opponents
                List<List<BaseCard>> opponentHands = new List<List<BaseCard>>();
                for (int i = 0; i < opponentCount; i++)
                    opponentHands.Add(new List<BaseCard>());

                for (int i = 0; i < deckWithoutOwnCards.Count; i++)
                {
                    opponentHands[i % opponentCount].Add(deckWithoutOwnCards[i]);
                }

                int leadPlayer = 0; // 0 = you, others = opponents
                int localWins = 0;

                // Play one trick per card in your hand
                while (playerHand.Count > 0)
                {
                    List<BaseCard> trickToTest = new List<BaseCard>();
                    Dictionary<int, BaseCard> playedCards = new Dictionary<int, BaseCard>();

                    // Players play cards in order starting from leadPlayer
                    for (int i = 0; i < PlayerCount; i++)
                    {
                        int currentPlayer = (leadPlayer + i) % PlayerCount;
                        BaseCard cardPlayed;

                        if (currentPlayer == 0)
                        {
                            cardPlayed = playerHand[0];
                            playerHand.RemoveAt(0);
                        }
                        else
                        {
                            var oppHand = opponentHands[currentPlayer - 1];
                            int idx = random.Next(oppHand.Count);
                            cardPlayed = oppHand[idx];
                            oppHand.RemoveAt(idx);
                        }

                        trickToTest.Add(cardPlayed);
                        playedCards[currentPlayer] = cardPlayed;
                    }

                    BaseCard? winnerCard = TrickResolver.DetermineTrickWinnerCard(trickToTest);

                    if (winnerCard == null)
                    {
                        // Draw: no winner this trick, keep lead player same or randomize
                        // For simplicity, lead player stays same
                        continue;
                    }

                    int winnerPlayer = playedCards.First(kvp =>
                        kvp.Value.CardType == winnerCard.CardType &&
                        kvp.Value.GenericValue == winnerCard.GenericValue).Key;

                    if (winnerPlayer == 0)
                    {
                        localWins++;
                    }

                    leadPlayer = winnerPlayer;
                }

                Interlocked.Add(ref wins, localWins);
            });

            WinRate = ((double)wins / (NSimulations * CardsToTest.Count));
            TricksWon = WinRate * Rounds;
        }

        [Obsolete]
        public void CalculateV1(int maxDegreeOfParallelism = 8)
        {
            int wins = 0;

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };

            Parallel.For(0, NSimulations, options, simulationCounter =>
            {
                // Create new Random per thread (thread-safe)
                var random = new Random(Guid.NewGuid().GetHashCode());

                List<BaseCard> playerHand = CardsToTest.ToList();
                playerHand.Shuffle(random);

                List<BaseCard> deck = Deck.CreateDeck();
                deck.Shuffle(random);

                List<BaseCard> deckWithoutOwnCards = deck.ToList();

                RemoveMatchesOneByOne(deckWithoutOwnCards, CardsToTest);

                int localWins = 0;

                while (playerHand.Count > 0)
                {
                    List<BaseCard> trickToTest = new List<BaseCard>();

                    BaseCard firstPlayerCard = playerHand[0];
                    trickToTest.Add(firstPlayerCard);
                    playerHand.RemoveAt(0);

                    for (int playerCounter = 1; playerCounter < PlayerCount; playerCounter++)
                    {
                        BaseCard opponentPlayerCard = deckWithoutOwnCards[0];
                        trickToTest.Add(opponentPlayerCard);
                        deckWithoutOwnCards.RemoveAt(0);
                    }

                    // simulate player not always starting and getting advantage
                    trickToTest.Shuffle(random);

                    BaseCard desiredWinner = trickToTest.First(x => x.CardType == firstPlayerCard.CardType && x.GenericValue == firstPlayerCard.GenericValue);
                    BaseCard? winner = TrickResolver.DetermineTrickWinnerCard(trickToTest);

                    if (winner == desiredWinner)
                    {
                        localWins++;
                    }
                    else
                    {
                        //Logger.Instance.WriteToConsoleAndLog(winner != null ? winner.ToString() : $"Draw, nobody wins!");
                    }
                }

                // Thread-safe increment
                Interlocked.Add(ref wins, localWins);
            });

            WinRate = ((double)wins / (NSimulations * CardsToTest.Count));
            TricksWon = WinRate * Rounds;
        }

#endif

        #endregion

    }
}
