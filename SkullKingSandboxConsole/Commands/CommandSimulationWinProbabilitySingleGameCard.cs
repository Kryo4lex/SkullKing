using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Game;
using SkullKingCore.Logging;
using SkullKingCore.Statistics;
using SkullKingCore.Utility;

namespace SkullKingSandboxConsole.Commands
{
    public class CommandSimulationWinProbabilitySingleGameCard
    {

        public static void Run()
        {

            int cardIndex = 0;
            int playerCount;
            int nSimulations;

            List<Card> allGameCards = Deck.CreateDeck();

            Card.PrintListFancy(allGameCards);

            if (!UserConsoleIO.TryReadInt($"{Environment.NewLine}Enter the index of the card you want to run the Simulation for:", out cardIndex, 0, allGameCards.Count - 1))
            {
                Logger.Instance.WriteToConsoleAndLog("Cancelled.\n");
                return;
            }

            if (!UserConsoleIO.TryReadInt($"{Environment.NewLine}Enter Player Count:", out playerCount, Settings.MinPlayerCount, Settings.MaxPlayerCount))
            {
                Logger.Instance.WriteToConsoleAndLog("Cancelled.\n");
                return;
            }

            if (!UserConsoleIO.TryReadInt($"{Environment.NewLine}Enter N Simulations:", out nSimulations, Settings.MinPlayerCount))
            {
                Logger.Instance.WriteToConsoleAndLog("Cancelled.\n");
                return;
            }

            Logger.Instance.WriteToConsoleAndLog("Params:");
            Logger.Instance.WriteToConsoleAndLog($"Player Count: {playerCount}");
            Logger.Instance.WriteToConsoleAndLog($"N Simulations: {nSimulations}");
            Logger.Instance.WriteToConsoleAndLog($"Selected Card: {allGameCards[cardIndex]}");

            SingleCardWinProbability singleCardWinProbability = new SingleCardWinProbability(allGameCards[cardIndex], playerCount, nSimulations);

            singleCardWinProbability.Calculate();

            SingleCardWinProbability.PrintListFancy(new List<SingleCardWinProbability>() { singleCardWinProbability }, Settings.DecimalPlaces);

        }

    }
}
