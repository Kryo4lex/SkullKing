using SkullKingCore.Cards.Base;
using SkullKingCore.GameDefinitions;
using SkullKingCore.Logging;
using SkullKingCore.Statistics;
using SkullKingCore.Utility.UserInput;
using System.Diagnostics;

namespace SkullKingConsole.Commands
{ 
    public static class CommandWinProbabilityOfAllGameCards
    {

        public static void Run()
        {

            int playerCount;
            int nSimulations;
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

            if (!UserInput.TryReadInt($"{Environment.NewLine}Enter Max Parallel Threads:", out maxParallelThreads))
            {
                Logger.Instance.WriteToConsoleAndLog("Cancelled.\n");
                return;
            }

            Logger.Instance.WriteToConsoleAndLog("Params:");
            Logger.Instance.WriteToConsoleAndLog($"Player Count: {playerCount}");
            Logger.Instance.WriteToConsoleAndLog($"N Simulations: {nSimulations}");
            Logger.Instance.WriteToConsoleAndLog($"Max Parallel Threads: {maxParallelThreads}");

            List<BaseCard> allGameCards = Deck.CreateDeck();
            List<SingleCardWinProbability> singleCardWinProbabilities = new List<SingleCardWinProbability>();

            Stopwatch totalTimeSingleCardWinProbabilityRun = Stopwatch.StartNew();

            object lockObj = new object();
            int runCounter = 1;

            Parallel.ForEach(
                allGameCards,
                new ParallelOptions { MaxDegreeOfParallelism = maxParallelThreads },
                cardToTest =>
                {
                    Stopwatch singleTimeSingleCardWinProbabilityRun = Stopwatch.StartNew();

                    int currentRun;
                    lock (lockObj)
                    {
                        currentRun = runCounter++;
                        Logger.Instance.WriteToConsoleAndLog($"Calculating {currentRun}/{allGameCards.Count}...");
                    }

                    SingleCardWinProbability singleCardWinProbability = new SingleCardWinProbability(cardToTest, playerCount, nSimulations);
                    singleCardWinProbability.Calculate();

                    lock (lockObj)
                    {
                        singleCardWinProbabilities.Add(singleCardWinProbability);
                    }

                    singleTimeSingleCardWinProbabilityRun.Stop();

                    lock (lockObj)
                    {
                        Logger.Instance.WriteToConsoleAndLog($"Single Run took {singleTimeSingleCardWinProbabilityRun.Elapsed.TotalMilliseconds} ms");
                    }
                });

            totalTimeSingleCardWinProbabilityRun.Stop();

            Logger.Instance.WriteToConsoleAndLog($"Total Run took {totalTimeSingleCardWinProbabilityRun.Elapsed.TotalMilliseconds} ms");

            singleCardWinProbabilities = singleCardWinProbabilities.OrderByDescending(x => x.WinRate).ToList();

            SingleCardWinProbability.PrintListFancy(singleCardWinProbabilities, Settings.DecimalPlaces);

        }

    }
}
