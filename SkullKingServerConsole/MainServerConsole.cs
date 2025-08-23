using SkullKing.Network.Server;
using SkullKingCore.Controller;
using SkullKingCore.Core.Game;
using SkullKingCore.Core.Game.Interfaces;
using SkullKingCore.Logging;
using SkullKingCore.Utility.UserInput;

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
                Console.WriteLine("You need at least 2 players total. Press Enter to exit.");
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

            Console.WriteLine();
            Console.WriteLine("=== Server Ready ===");
            Console.WriteLine($"Start Round : {startRound}");
            Console.WriteLine($"Max Rounds  : {maxRounds}");
            Console.WriteLine("Players:");
            foreach (var p in players)
                Console.WriteLine($" - {p.Name} (Id: {p.Id})");

            if (numNets > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Network player listeners:");
                for (int i = 1; i <= numNets; i++)
                {
                    int port = basePort + (i - 1);
                    Console.WriteLine($"  NET{i}: 0.0.0.0:{port}  (client connects with: host {GetLocalIpHint()} port {port})");
                }
                Console.WriteLine();
                Console.WriteLine("Start your network clients now, then press Enter to begin the game...");
                //Console.ReadLine();
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Press Enter to start the game...");
                Console.ReadLine();
            }

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Cancellation requested… will stop after current step.");
            };

            try
            {
                var handler = new GameHandler(players, startRound, maxRounds, controllers);
                await handler.RunGameAsync();
                Console.WriteLine();
                Console.WriteLine("Game finished. Press Enter to exit.");
                Console.ReadLine();
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Server canceled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal error:");
                Console.WriteLine(ex);
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

    private static string GetLocalIpHint()
    {
        try
        {
            // Quick hint for LAN users; not critical if it fails.
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            var ip = host.AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            return ip?.ToString() ?? "127.0.0.1";
        }
        catch { return "127.0.0.1"; }
    }
}
