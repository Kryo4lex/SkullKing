using SkullKingCore.Logging;
using SkullKingCore.Test;
using SkullKingCore.Utility.UserInput;

namespace SkullKingConsole.Commands
{
    public static class CommandTrickSimulation
    {

        public static void Run()
        {

            bool seedGameRunning = true;

            int playerCount;
            int round;

            if (!UserInput.TryReadInt("Enter Player Count:", out playerCount))
            {
                Logger.Instance.WriteToConsoleAndLog("Cancelled.\n");
                return;
            }

            if (!UserInput.TryReadInt("Enter Round:", out round))
            {
                Logger.Instance.WriteToConsoleAndLog("Cancelled.\n");
                return;
            }

            int seed = 0;

            while (seedGameRunning)
            {

                Console.Clear();

                TrickSimulation trickSimulation = new TrickSimulation(playerCount, round, seed);

                trickSimulation.Play();

                var ch = Console.ReadKey(false).Key;

                switch (ch)
                {
                    case ConsoleKey.E:
                        seedGameRunning = false;
                        return;
                    case ConsoleKey.LeftArrow:
                        seed--;
                        break;
                    case ConsoleKey.RightArrow:
                        seed++;
                        break;
                }

            }

        }

    }
}
