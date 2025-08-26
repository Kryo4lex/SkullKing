using SkullKingCore.Controller;
using SkullKingCore.Core.Game;
using SkullKingCore.Core.Game.Interfaces;
using SkullKingCore.Core.Game.Scoring.Implementations;
using SkullKingCore.Logging;
using SkullKingCore.Network.TCP.Server;
using SkullKingCore.Network.FileRpc.Server;
using SkullKingCore.Utility;
using SkullKingCore.Network;

namespace SkullKingServerConsole
{

    public class MainServerConsole
    {


        public static async Task Main()
        {
            try
            {
                Console.WriteLine("Select network transport for NET players:");
                Console.WriteLine("  1) TCP (sockets)");
                Console.WriteLine("  2) FileRpc (shared folder)");
                var choice = UserInput.ReadIntUntilValid("Enter 1 or 2", 1, 2);
                var transport = (TransportKind)choice;

                Console.Title = transport switch
                {
                    TransportKind.Tcp => "Skull King Server (TCP)",
                    TransportKind.FileRpc => "Skull King Server (FileRpc)",
                    _ => "Skull King Server"
                };

                // Common game config
                int numLocalHumans = UserInput.ReadIntUntilValid("Enter Number of Local Human Players", 0, 8);
                int numCpus = UserInput.ReadIntUntilValid("Enter Number of Local CPU Players", 0, 8);
                int numNets = UserInput.ReadIntUntilValid("Enter Number of NET Players", 0, 8);
                int startRound = UserInput.ReadIntUntilValid("Enter starting round", 1, 10);
                int maxRounds = UserInput.ReadIntUntilValid("Enter maximum round", 1, 10);

                // Transport-specific inputs
                int basePort = 0;
                string baseFolder = "";

                switch (transport)
                {
                    case TransportKind.Tcp:
                        basePort = UserInput.ReadIntUntilValid("Enter Port (base for NET players)", 0, 65535);
                        break;

                    case TransportKind.FileRpc:

                        Logger.Instance.WriteToConsoleAndLog("Enter base shared folder (e.g., C:\\Temp\\SkullKingShare): ");

                        baseFolder = (Console.ReadLine() ?? string.Empty).Trim();
                        if (string.IsNullOrWhiteSpace(baseFolder))
                        {
                            Logger.Instance.WriteToConsoleAndLog("You must provide a folder. Press Enter to exit.");
                            Console.ReadLine();
                            return;
                        }
                        Directory.CreateDirectory(baseFolder);
                        break;
                }

                int totalPlayers = numLocalHumans + numCpus + numNets;
                if (totalPlayers < 2)
                {
                    Logger.Instance.WriteToConsoleAndLog("You need at least 2 players total. Press Enter to exit.");
                    Console.ReadLine();
                    return;
                }

                var players = new List<Player>(totalPlayers);
                var controllers = new Dictionary<string, IGameController>(totalPlayers);

                // Track hosted network controllers (both types implement IAsyncDisposable)
                var hostedDisposables = new List<IAsyncDisposable>(numNets);

                // Local human players
                for (int i = 1; i <= numLocalHumans; i++)
                {
                    string name = $"Human{i}";
                    var player = new Player(name, name);
                    players.Add(player);
                    controllers[player.Id] = new LocalConsoleHumanController();
                }

                // Local CPU players
                for (int i = 1; i <= numCpus; i++)
                {
                    string name = $"CPU{i}";
                    var player = new Player(name, name);
                    players.Add(player);
                    controllers[player.Id] = new LocalConsoleCPUController(name);
                }

                // NET players (branch per transport)
                for (int i = 1; i <= numNets; i++)
                {
                    string name = $"NET{i}";
                    var player = new Player(name, name);
                    players.Add(player);

                    switch (transport)
                    {
                        case TransportKind.Tcp:
                            {
                                int port = basePort + (i - 1);
                                var hosted = new NetworkTCPHostedGameController(port, player.Id);
                                hostedDisposables.Add(hosted);
                                controllers[player.Id] = hosted;
                                break;
                            }

                        case TransportKind.FileRpc:
                            {
                                // one subfolder per NETi to avoid contention
                                var netFolder = Path.Combine(baseFolder, $"NET{i}");
                                Directory.CreateDirectory(netFolder);

                                var hosted = new NetworkFileHostedGameController(netFolder, player.Id);
                                hostedDisposables.Add(hosted);
                                controllers[player.Id] = hosted;
                                break;
                            }
                    }
                }

                // --------- UI / info ---------
                Logger.Instance.WriteToConsoleAndLog("");
                Logger.Instance.WriteToConsoleAndLog($"=== Server Ready ({transport}) ===");
                Logger.Instance.WriteToConsoleAndLog($"Start Round : {startRound}");
                Logger.Instance.WriteToConsoleAndLog($"Max Rounds  : {maxRounds}");
                Logger.Instance.WriteToConsoleAndLog("Players:");

                foreach (var p in players)
                    Logger.Instance.WriteToConsoleAndLog($"o {p.Name}");

                if (numNets > 0)
                {
                    Logger.Instance.WriteToConsoleAndLog("");
                    if (transport == TransportKind.Tcp)
                    {
                        Logger.Instance.WriteToConsoleAndLog("Network player listeners (TCP):");
                        for (int i = 1; i <= numNets; i++)
                        {
                            int port = basePort + (i - 1);
                            Logger.Instance.WriteToConsoleAndLog($"NET{i}: client connects with: {NetworkUtils.GetLocalIpHint()}:{port}");
                        }
                    }
                    else if (transport == TransportKind.FileRpc)
                    {
                        Logger.Instance.WriteToConsoleAndLog("Network player folders (FileRpc):");
                        for (int i = 1; i <= numNets; i++)
                        {
                            var netFolder = Path.Combine(baseFolder, $"NET{i}");
                            Logger.Instance.WriteToConsoleAndLog($"NET{i}: client enters any ClientId and uses folder: {netFolder}");
                        }
                    }

                    Logger.Instance.WriteToConsoleAndLog("");
                    Logger.Instance.WriteToConsoleAndLog("Start your network clients now, then press Enter to begin the game...");
                    // Console.ReadLine(); // keep commented if you want immediate start
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
                    var handler = new GameHandler(players, startRound, maxRounds, controllers, new SkullKingScoring());
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
                    foreach (var d in hostedDisposables)
                    {
                        try { await d.DisposeAsync(); } catch { /* ignore */ }
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
}