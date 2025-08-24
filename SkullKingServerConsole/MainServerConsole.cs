using SkullKing.Network.Server;
using SkullKingCore.Controller;
using SkullKingCore.Core.Game;
using SkullKingCore.Core.Game.Interfaces;
using SkullKingCore.Logging;
using SkullKingCore.Utility;

public class MainServerConsole
{
    public static async Task Main()
    {
        try
        { 
            Console.Title = "Skull King Server";
            /*
            int basePort = 1234;
            int numLocalHumans = 0;
            int numCpus = 3;
            int numNets = 1;
            int startRound = 5;
            int maxRounds = 10;
            */
            
            int basePort = UserInput.ReadIntUntilValid($"Enter Port", 0, 65535);
            int numLocalHumans = UserInput.ReadIntUntilValid($"Enter Number of Local Human Players", 0, 8);
            int numCpus = UserInput.ReadIntUntilValid($"Enter Number of Local CPU Players", 0, 8);
            int numNets = UserInput.ReadIntUntilValid($"Enter Number of NET Players", 0, 8);
            int startRound = UserInput.ReadIntUntilValid($"Enter starting round", 1, 10);
            int maxRounds = UserInput.ReadIntUntilValid($"Enter maximum round", 1, 10);
            
            int totalPlayers = numLocalHumans + numCpus + numNets;
            if (totalPlayers < 2)
            {
                Logger.Instance.WriteToConsoleAndLog("You need at least 2 players total. Press Enter to exit.");
                Console.ReadLine();
                return;
            }

            var players = new List<Player>(totalPlayers);
            var controllers = new Dictionary<string, IGameController>(totalPlayers);
            var hostedNetworkControllers = new List<NetworkHostedGameController>(numNets);

            // Build local human players
            for (int i = 1; i <= numLocalHumans; i++)
            {
                string name = $"Human{i}";
                var player = new Player(name, name);
                players.Add(player);

                var controller = new LocalConsoleHumanController();
                controllers[player.Id] = controller;
            }

            // Build local CPU players
            for (int i = 1; i <= numCpus; i++)
            {
                string name = $"CPU{i}";
                var player = new Player(name, name);
                players.Add(player);

                var controller = new LocalConsoleCPUController(name);
                controllers[player.Id] = controller;
            }

            // Build network players (one server per network player on basePort + offset)
            for (int i = 1; i <= numNets; i++)
            {
                string name = $"NET{i}";
                int port = basePort + (i - 1);

                var player = new Player(name, name);
                players.Add(player);

                var controller = new NetworkHostedGameController(port, player.Id);
                hostedNetworkControllers.Add(controller);
                controllers[player.Id] = controller;
            }

            Logger.Instance.WriteToConsoleAndLog("");
            Logger.Instance.WriteToConsoleAndLog("=== Server Ready ===");
            Logger.Instance.WriteToConsoleAndLog($"Start Round : {startRound}");
            Logger.Instance.WriteToConsoleAndLog($"Max Rounds  : {maxRounds}");
            Logger.Instance.WriteToConsoleAndLog("Players:");

            foreach (var p in players)
                Logger.Instance.WriteToConsoleAndLog($"o {p.Name}");

            if (numNets > 0)
            {
                Logger.Instance.WriteToConsoleAndLog("");
                Logger.Instance.WriteToConsoleAndLog("Network player listeners:");

                for (int i = 1; i <= numNets; i++)
                {
                    int port = basePort + (i - 1);
                    Logger.Instance.WriteToConsoleAndLog($"NET{i}: (client connects with: {NetworkUtils.GetLocalIpHint()}:{port})");
                }

                Logger.Instance.WriteToConsoleAndLog("");
                Logger.Instance.WriteToConsoleAndLog("Start your network clients now, then press Enter to begin the game...");

                //Console.ReadLine();
            }
            else
            {
                Logger.Instance.WriteToConsoleAndLog("");
                Logger.Instance.WriteToConsoleAndLog("Press Enter to start the game...");

                Console.ReadLine();
            }

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Logger.Instance.WriteToConsoleAndLog("Cancellation requested... will stop after current step.");
            };

            try
            {
                var handler = new GameHandler(players, startRound, maxRounds, controllers);

                await handler.RunGameAsync();

                Logger.Instance.WriteToConsoleAndLog();
                Logger.Instance.WriteToConsoleAndLog("Game finished. Press Enter to exit.");
                Console.ReadLine();
            }
            catch (OperationCanceledException)
            {
                Logger.Instance.WriteToConsoleAndLog("Server canceled.");
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteToConsoleAndLog("Fatal error:");
                Logger.Instance.WriteToConsoleAndLog(ex.ToString());
            }
            finally
            {
                foreach (var net in hostedNetworkControllers)
                {
                    try { await net.DisposeAsync(); } catch { /* ignore */ }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}ERROR:{Environment.NewLine}{ex}");
            Console.ReadLine();
        }
    }

}
