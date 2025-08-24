using SkullKingConsole.Commands;
using SkullKingCore.Logging;
using SkullKingCore.Utility;

namespace SkullKingConsole
{
    public class Program
    {

        public static Dictionary<int, (string Description, Action Action)> Options = new Dictionary<int, (string, Action)>
        {
            { 0, ($"Exit Application", ExitMain) },
            { 1, ($"Trick Tests", CommandTrickTests.Run) },
            { 2, ($"Win Probability Single Card", CommandSimulationWinProbabilitySingleGameCard.Run) },
            { 3, ($"Win Probability All Game Cards", CommandWinProbabilityOfAllGameCards.Run) },
            { 4, ($"Card Hand Win Probability", CommandCardHandWinProbability.Run) },
            { 5, ($"General Card Mightiness", CommandGeneralCardMightiness.Run) },
            { 6, ($"Trick Simulation", CommandTrickSimulation.Run) },
        };

        public static void Main(string[] args)
        {

            Logger.Instance.Initialize($"{nameof(SkullKingConsole)}_log.txt");

            while (true)
            {

                PrintOptions();

                int choice = UserInput.ReadIntUntilValid($"{Environment.NewLine}Choose an option:", 0, Options.Count - 1);

                Options.TryGetValue(choice, out var actionTuple);

                Logger.Instance.WriteToConsoleAndLog($"{actionTuple.Description}");

                actionTuple.Action();

                Console.ReadLine();

            }

        }

        private static void PrintOptions()
        {

            foreach (var kvp in Options)
            {
                Logger.Instance.WriteToConsoleAndLog($"{kvp.Key} - {kvp.Value.Description}");
            }

        }

        private static void ExitMain()
        {

            Logger.Instance.WriteToConsoleAndLog("Exiting...");
            Environment.Exit(0);

        }

    }
}