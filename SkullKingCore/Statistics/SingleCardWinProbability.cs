using SkullKingCore.Core;
using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Extensions;
using SkullKingCore.GameDefinitions;
using SkullKingCore.Logging;
using SkullKingCore.Utility;

namespace SkullKingCore.Statistics
{
    public class SingleCardWinProbability
    {

        public Card CardToTest { get; private set; }
        public int PlayerCount { get; private set; }
        public int NSimulations { get; private set; }

        public double WinRate { get; private set; } = default(double);

        public double WinPercentage
        {
            get
            {
                return WinRate * 100;
            }
        }

        public SingleCardWinProbability(Card cardToTest, int playerCount, int nSimulations)
        {
            CardToTest = cardToTest;
            PlayerCount = playerCount;
            NSimulations = nSimulations;
        }

        public void Calculate()
        {

            int wins = 0;

            List<Card> deck = Deck.CreateDeck();

            for (int simulationCounter = 0; simulationCounter < NSimulations; simulationCounter++)
            {

                deck.Remove(CardToTest);

                //improves Speed
                deck.Shuffle(new Random());

                List<Card> trick = new List<Card>();

                for (int j = 0; j < PlayerCount - 1; j++)
                {
                    Card c = deck[j];
                    trick.Add(c);
                }

                int insertIndex = RandomHelper.RandomInt(0, PlayerCount);
                trick.Insert(insertIndex, CardToTest);

                Card? winner = TrickResolver.DetermineTrickWinnerCard(trick);

                if (winner == CardToTest)
                {
                    wins++;
                }

                deck.Add(CardToTest);

            }

            WinRate = ((double)wins / NSimulations);

        }

        public static void PrintListFancy(List<SingleCardWinProbability> monteCarloRuns, int decimalPlaces, string headerCardType = "Card Type", string headerSubType = "Sub Type", string headerWinRate = "Win Rate (%)")
        {
            // Determine the max width for each column based on headers and content
            int maxLengthCardType = Math.Max(headerCardType.Length, monteCarloRuns.Max(obj => obj.CardToTest.CardType.ToString().Length));
            int maxLengthSubType = Math.Max(headerSubType.Length, monteCarloRuns.Max(obj => obj.CardToTest.SubType().Length));
            int maxLengthWinRate = Math.Max(headerWinRate.Length, monteCarloRuns.Max(obj => obj.WinPercentage.ToString($"F{decimalPlaces}").Length));

            string separator = "+" +
                new string('-', maxLengthCardType + 2) + "+" +
                new string('-', maxLengthSubType + 2) + "+" +
                new string('-', maxLengthWinRate + 2) + "+";

            // Print the table header
            string headerLine = $"| {headerCardType.PadRight(maxLengthCardType)} | {headerSubType.PadRight(maxLengthSubType)} | {headerWinRate.PadLeft(maxLengthWinRate)} |";

            Logger.Instance.WriteToConsoleAndLog(separator);
            Logger.Instance.WriteToConsoleAndLog(headerLine);
            Logger.Instance.WriteToConsoleAndLog(separator);

            // Print each row
            foreach (SingleCardWinProbability monteCarloRun in monteCarloRuns)
            {
                string line = $"| {monteCarloRun.CardToTest.CardType.ToString().PadRight(maxLengthCardType)} | " +
                              $"{monteCarloRun.CardToTest.SubType().PadRight(maxLengthSubType)} | " +
                              $"{monteCarloRun.WinPercentage.ToString($"F{decimalPlaces}").PadLeft(maxLengthWinRate)} |";
                Logger.Instance.WriteToConsoleAndLog(line);
            }

            Logger.Instance.WriteToConsoleAndLog(separator);
        }

    }
}
