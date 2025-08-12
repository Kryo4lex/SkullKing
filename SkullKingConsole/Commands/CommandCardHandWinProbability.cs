using SkullKingCore.Cards.Base;
using SkullKingCore.GameDefinitions;
using SkullKingCore.Logging;
using SkullKingCore.Statistics;
using SkullKingCore.Utility.UserInput;
using System.Diagnostics;

namespace SkullKingConsole.Commands
{
    public static class CommandCardHandWinProbability
    {

        public static void Run()
        {

            int playerCount;
            int nSimulations;
            int cardsOnHandCount;
            int maxParallelThreads;

            if (!UserInput.TryReadInt($"{Environment.NewLine}Enter Player Count:", out playerCount))
            {
                Logger.Instance.WriteToConsoleAndLog("Cancelled.\n");
                return;
            }

            if (!UserInput.TryReadInt($"{Environment.NewLine}Enter N Simulations:", out nSimulations))
            {
                Logger.Instance.WriteToConsoleAndLog("Cancelled.\n");
                return;
            }

            if (!UserInput.TryReadInt($"{Environment.NewLine}Enter number of cards on hands:", out cardsOnHandCount))
            {
                Logger.Instance.WriteToConsoleAndLog("Cancelled.\n");
                return;
            }

            if (!UserInput.TryReadInt($"{Environment.NewLine}Enter Max Parallel Threads:", out maxParallelThreads))
            {
                Logger.Instance.WriteToConsoleAndLog("Cancelled.\n");
                return;
            }

            List<BaseCard> allGameCards = Deck.CreateDeck();

            List<BaseCard> cardHandToTest = new List<BaseCard>();

            for (int cardsOnHandCounter = 0; cardsOnHandCounter < cardsOnHandCount; cardsOnHandCounter++)
            {

                int cardIndex = 0;

                BaseCard.PrintListFancy(allGameCards);

                if (!UserInput.TryReadInt($"{Environment.NewLine}Enter the index of the card you want to add to the hand {cardsOnHandCounter + 1}/{cardsOnHandCount}:", out cardIndex, 0, allGameCards.Count - 1))
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
            BaseCard.PrintListFancy(cardHandToTest);

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
