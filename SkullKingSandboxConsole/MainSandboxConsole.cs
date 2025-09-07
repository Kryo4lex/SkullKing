using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Game;
using SkullKingCore.Logging;
using SkullKingCore.Utility;
using SkullKingSandboxConsole.Commands;

namespace SkullKingSandboxConsole
{
    public class MainSandboxConsole
    {

        public static readonly List<(string Description, Action Action)> Options = new()
        {
            ("Exit Application", ExitMain),
            ("Win Probability Single Card", CommandSimulationWinProbabilitySingleGameCard.Run),
            ("Win Probability All Game Cards", CommandWinProbabilityOfAllGameCards.Run),
            ("Card Hand Win Probability", CommandCardHandWinProbability.Run),
            ("General Card Mightiness", CommandGeneralCardMightiness.Run),
            ("Trick Simulation", CommandTrickSimulation.Run),
            ("Card Name Generation", CommandCardNameGen.Run),
        };

        public static void Main(string[] args)
        {

            //Logger.Instance.Initialize($"{nameof(SkullKingSandboxConsole)}_log.txt");

            while (true)
            {

                PrintOptions();

                int choice = UserConsoleIO.ReadIntUntilValid($"{Environment.NewLine}Choose an option:", 0, Options.Count - 1);

                var (description, action) = Options[choice];

                Logger.Instance.WriteToConsoleAndLog(description);

                action();

                Console.ReadLine();

            }

        }

        private static void PrintOptions()
        {
            for (int i = 0; i < Options.Count; i++)
            {
                Logger.Instance.WriteToConsoleAndLog($"{i} - {Options[i].Description}");
            }
        }

        private static void ExitMain()
        {

            Logger.Instance.WriteToConsoleAndLog("Exiting...");
            Environment.Exit(0);

        }

    }
}