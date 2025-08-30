using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Game;
using SkullKingCore.Logging;
using SkullKingCore.Statistics;
using SkullKingCore.Utility;
using System.Diagnostics;

namespace SkullKingSandboxConsole.Commands
{ 
    public static class CommandWinProbabilityOfAllGameCards
    {

        public static void Run()
        {

            int playerCount;
            int nSimulations;
            int maxParallelThreads;

            if (!UserConsoleIO.TryReadInt($"{Environment.NewLine}Enter Player Count:", out playerCount, Settings.MinPlayerCount, Settings.MaxPlayerCount))
            {
                Logger.Instance.WriteToConsoleAndLog("Cancelled.\n");
                return;
            }

            if (!UserConsoleIO.TryReadInt($"{Environment.NewLine}Enter N Simulations:", out nSimulations))
            {
                Logger.Instance.WriteToConsoleAndLog("Cancelled.\n");
                return;
            }

            if (!UserConsoleIO.TryReadInt($"{Environment.NewLine}Enter Max Parallel Threads:", out maxParallelThreads, 1))
            {
                Logger.Instance.WriteToConsoleAndLog("Cancelled.\n");
                return;
            }

            Logger.Instance.WriteToConsoleAndLog("Params:");
            Logger.Instance.WriteToConsoleAndLog($"Player Count: {playerCount}");
            Logger.Instance.WriteToConsoleAndLog($"N Simulations: {nSimulations}");
            Logger.Instance.WriteToConsoleAndLog($"Max Parallel Threads: {maxParallelThreads}");

            List<Card> allGameCards = Deck.CreateDeck();
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
