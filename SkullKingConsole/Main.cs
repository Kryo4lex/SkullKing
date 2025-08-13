using SkullKingConsole.Commands;
using SkullKingConsole.Controller;
using SkullKingCore.Core.Game;
using SkullKingCore.Core.Game.Interfaces;
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

        public static void Main(string[] args)
        {

            Logger.Instance.Initialize($"{nameof(SkullKingConsole)}_log.txt");

            while (true)
            {

                ConsoleCPUControllerTest();

                Console.ReadLine();

            }

            return;
            
            while (true)
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

        private static async void ConsoleCPUControllerTest()
        {

            // 1. Create players
            var players = new List<Player>();

            for (int i = 1; i <= 4; i++)
            {
                Player player = new Player($"{i}", $"CPU_{i}");

                players.Add(player);
            }

            // 2. Create controllers for each player
            var controllers = new Dictionary<string, IGameController>();
            foreach (var player in players)
            {
                controllers[player.Id] = new LocalConsoleCPUController(player.Name);
            }

            // 3. Create the game state with, e.g., 5 rounds
            var gameState = new GameState(players, startRound: 5, maxRounds: 5, controllers);

            // 4. Run the game
            await gameState.RunGameAsync();
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
