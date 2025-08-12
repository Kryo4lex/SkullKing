using SkullKingConsole.Commands;
using SkullKingCore.Logging;
using SkullKingCore.Utility.UserInput;

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

        static bool running = true;

        public static void Main(string[] args)
        {

            Logger.Instance.Initialize($"{nameof(SkullKingConsole)}_log.txt");

            while (running)
            {

                PrintOptions();

                if (UserInput.TryReadInt($"{Environment.NewLine}Choose an option:", out int choice))
                {
                    if (Options.TryGetValue(choice, out var actionTuple))
                    {
                        Logger.Instance.WriteToConsoleAndLog($"{actionTuple.Description}");

                        actionTuple.Action();

                        Console.ReadLine();
                    }
                    else
                    {
                        Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}Not an option. Try again.{Environment.NewLine}");
                    }
                }
                else
                {
                    Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}Invalid number. Try again.{Environment.NewLine}");
                }

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
            running = false;

        }

    }
}
