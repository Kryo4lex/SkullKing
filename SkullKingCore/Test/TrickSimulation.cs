using SkullKingCore.Core;
using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Game;
using SkullKingCore.Extensions;
using SkullKingCore.GameDefinitions;
using SkullKingCore.Logging;

namespace SkullKingCore.Test
{
    public class TrickSimulation
    {

        public int PlayerCount { get; private set; }
        public int CurrentRound { get; private set; }
        public int Seed {  get; private set; }

        public TrickSimulation(int playerCount, int currentRound, int seed)
        {
            PlayerCount = playerCount;
            CurrentRound = currentRound;
            Seed = seed;
        }

        public void Play()
        {
            List<Card> gameCards = new List<Card>();

            gameCards = Deck.CreateDeck();

            List<Card> shuffledGameCards = gameCards.ToList().Shuffle(Seed);

            List<Player> players = new List<Player>();

            for (int playerCounter = 0; playerCounter < PlayerCount; playerCounter++)
            {
                Player newPlayer = new Player($"Player_{playerCounter + 1}", $"Player_{playerCounter + 1}");

                newPlayer.Hand = shuffledGameCards.TakeChunk(CurrentRound);

                players.Add(newPlayer);
            }

            List<Card> currentTrick = new List<Card>();

            Logger.Instance.WriteToConsoleAndLog($"Seed: {Seed}");
            Logger.Instance.WriteToConsoleAndLog($"");

            foreach (Player player in players)
            {
                Card playedCardOfPlayer = player.Hand.TakeChunk(1).First();

                currentTrick.Add(playedCardOfPlayer);

                Logger.Instance.WriteToConsoleAndLog($"{player.Name}");
                Logger.Instance.WriteToConsoleAndLog($"{playedCardOfPlayer}");
            }

            int? indexOfWinningCard = TrickResolver.DetermineTrickWinnerIndex(currentTrick);

            if (indexOfWinningCard == null)
            {
                Logger.Instance.WriteToConsoleAndLog("");
                Logger.Instance.WriteToConsoleAndLog($"Draw, nobody wins!");
            }
            else
            {
                Card winningCard = currentTrick[(int)indexOfWinningCard];

                Logger.Instance.WriteToConsoleAndLog("");
                Logger.Instance.WriteToConsoleAndLog("Winner:");
                Logger.Instance.WriteToConsoleAndLog($"{winningCard}");
            }

        }



    }
}
