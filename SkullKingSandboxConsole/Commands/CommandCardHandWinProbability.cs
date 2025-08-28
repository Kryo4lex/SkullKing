using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Game;
using SkullKingCore.Logging;
using SkullKingCore.Statistics;
using SkullKingCore.Utility;
using System.Diagnostics;

namespace SkullKingSandboxConsole.Commands
{
    public static class CommandCardHandWinProbability
    {

        public static void Run()
        {

            int playerCount;
            int nSimulations;
            int cardsOnHandCount;
            int maxParallelThreads;

            if (!UserConsoleIO.TryReadInt($"{Environment.NewLine}Enter Player Count:", out playerCount))
            {
                Logger.Instance.WriteToConsoleAndLog("Cancelled.\n");
                return;
            }

            if (!UserConsoleIO.TryReadInt($"{Environment.NewLine}Enter N Simulations:", out nSimulations))
            {
                Logger.Instance.WriteToConsoleAndLog("Cancelled.\n");
                return;
            }

            if (!UserConsoleIO.TryReadInt($"{Environment.NewLine}Enter number of cards on hands:", out cardsOnHandCount))
            {
                Logger.Instance.WriteToConsoleAndLog("Cancelled.\n");
                return;
            }

            if (!UserConsoleIO.TryReadInt($"{Environment.NewLine}Enter Max Parallel Threads:", out maxParallelThreads))
            {
                Logger.Instance.WriteToConsoleAndLog("Cancelled.\n");
                return;
            }

            List<Card> allGameCards = Deck.CreateDeck();

            List<Card> cardHandToTest = new List<Card>();

            for (int cardsOnHandCounter = 0; cardsOnHandCounter < cardsOnHandCount; cardsOnHandCounter++)
            {

                int cardIndex = 0;

                Card.PrintListFancy(allGameCards);

                if (!UserConsoleIO.TryReadInt($"{Environment.NewLine}Enter the index of the card you want to add to the hand {cardsOnHandCounter + 1}/{cardsOnHandCount}:", out cardIndex, 0, allGameCards.Count - 1))
                {
                    Logger.Instance.WriteToConsoleAndLog("Cancelled.\n");
                    return;
                }
                else
                {
                    cardHandToTest.Add(allGameCards[cardIndex]);
                }

            }

            Logger.Instance.WriteToConsoleAndLog("Params:");
            Logger.Instance.WriteToConsoleAndLog($"Player Count: {playerCount}");
            Logger.Instance.WriteToConsoleAndLog($"N Simulations: {nSimulations}");
            Logger.Instance.WriteToConsoleAndLog($"Max Parallel Threads: {maxParallelThreads}");
            Logger.Instance.WriteToConsoleAndLog($"Selected Cards:");
            Card.PrintListFancy(cardHandToTest);

            CardHandWinProbability cardHandWinProbability = new CardHandWinProbability(cardHandToTest, playerCount, nSimulations);

            Stopwatch cardHandWinProbabilityStopwatch = Stopwatch.StartNew();

            cardHandWinProbabilityStopwatch = Stopwatch.StartNew();

            cardHandWinProbability.Calculate(maxParallelThreads);

            cardHandWinProbabilityStopwatch.Stop();

            Logger.Instance.WriteToConsoleAndLog($"Total Run took {cardHandWinProbabilityStopwatch.Elapsed.TotalMilliseconds} ms");

            cardHandWinProbability.PrintResults(Settings.DecimalPlaces);

        }

    }
}
