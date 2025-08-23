using SkullKing.Network.Server;
using SkullKingCore.Controller;
using SkullKingCore.Core.Game;
using SkullKingCore.Core.Game.Interfaces;

internal class MainServerConsole
{
    private static async Task Main()
    {
        Console.Title = "Skull King Server";

        int basePort = 1234;// PromptInt("Base TCP port for network players [5005]: ", 1, 65535, 5005);
        int numHumans = 0;// PromptInt("Number of local human players [1]: ", 0, 8, 1);
        int numCpus = 3;// PromptInt("Number of local CPU players   [0]: ", 0, 8, 0);
        int numNets = 1;// PromptInt("Number of network players     [0]: ", 0, 8, 0);
        int startRound = 5;// PromptInt("Starting round number         [1]: ", 1, 20, 1);
        int maxRounds = 10;// PromptInt("Max rounds to play           [10]: ", 1, 20, 10);

        int totalPlayers = numHumans + numCpus + numNets;
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
        for (int i = 1; i <= numHumans; i++)
        {
            string name = $"Human{i}";
            var player = new Player(name, name);
            players.Add(player);

            var controller = new LocalConsoleHumanController(name);
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
            Console.ReadLine();
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
