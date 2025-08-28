using SkullKingCore.Controller;
using SkullKingCore.Core.Game;
using SkullKingCore.Core.Game.Interfaces;
using SkullKingCore.Core.Game.Scoring.Implementations;
using SkullKingCore.Logging;
using SkullKingCore.Network;                     // TransportKind + NetworkUtils
using SkullKingCore.Network.FileRpc.Server;     // FileRpc hosted controller
using SkullKingCore.Network.TCP.Server;         // TCP hosted controller
using SkullKingCore.Network.WebRpc.Server;     // WebRpc hosted controller
using SkullKingCore.Utility;

namespace SkullKingServerConsole
{
    public class MainServerConsole
    {
        public static async Task Main(string[] args)
        {

            Console.Title = $"Skull King Server Console";

            try
            {
                if (args == null || args.Length == 0)
                {
                    PrintUsage();
                    Console.ReadLine();
                    return;
                }

                // -------- parse CLI into config --------
                if (!TryParseArgs(args, out var cfg, out var error))
                {
                    Logger.Instance.WriteToConsoleAndLog($"Error: {error}");
                    PrintUsage();
                    Console.ReadLine();
                    return;
                }

                Console.Title = $"Skull King Server Console ({Misc.GetEnumLabel(cfg.Transport)})";

                // Validate totals
                var totalPlayers = cfg.NumLocalHumans + cfg.NumCpus + cfg.NumNets;
                if (totalPlayers < 2)
                {
                    Logger.Instance.WriteToConsoleAndLog("You need at least 2 players total.");
                    return;
                }

                // -------- build players/controllers --------
                var players = new List<Player>(totalPlayers);
                var controllers = new Dictionary<string, IGameController>(totalPlayers);
                var hostedDisposables = new List<IAsyncDisposable>(Math.Max(1, cfg.NumNets));

                // Local humans
                for (int i = 1; i <= cfg.NumLocalHumans; i++)
                {
                    var name = $"Human{i}";
                    var p = new Player(name, name);
                    players.Add(p);
                    controllers[p.Id] = new LocalConsoleHumanController();
                }

                // Local CPUs
                for (int i = 1; i <= cfg.NumCpus; i++)
                {
                    var name = $"CPU{i}";
                    var p = new Player(name, name);
                    players.Add(p);
                    controllers[p.Id] = new LocalConsoleCPUController(name);
                }

                // NET players per transport
                for (int i = 1; i <= cfg.NumNets; i++)
                {
                    var name = $"NET{i}";
                    var p = new Player(name, name);
                    players.Add(p);

                    switch (cfg.Transport)
                    {
                        case TransportKind.Tcp:
                            {
                                var port = cfg.BasePort + (i - 1);
                                var hosted = new NetworkTCPHostedGameController(port, p.Id);
                                hostedDisposables.Add(hosted);
                                controllers[p.Id] = hosted;
                                break;
                            }
                        case TransportKind.FileRpc:
                            {
                                // one subfolder per NETi to avoid contention
                                var netFolder = Path.Combine(cfg.BaseFolder!, $"NET{i}");
                                Directory.CreateDirectory(netFolder);
                                var hosted = new NetworkFileHostedGameController(netFolder, p.Id);
                                hostedDisposables.Add(hosted);
                                controllers[p.Id] = hosted;
                                break;
                            }
                        case TransportKind.WebRpc:
                            {

                                // one controller per remote player, but the underlying Kestrel host is shared per port
                                var hosted = new NetworkWebHostedGameController(cfg.BasePort, p.Id)
                                {
                                    // optional per-player knobs:
                                    EnableHostInfoLogs = false,              // only the first controller on this port actually affects host logging
                                    LogAllCalls = false,                    // set true to log every RPC call/reply
                                    TreatNoTrafficAsDisconnect = false,     // idle human is OK; liveness comes from client long-poll/ping
                                    //ClientIdleDisconnect = TimeSpan.FromMinutes(2),
                                    ConnectTimeout = null                   // wait indefinitely for first contact; or set e.g. TimeSpan.FromMinutes(2)
                                };

                                hostedDisposables.Add(hosted);
                                controllers[p.Id] = hosted;
                                break;
                            }
                    }
                }

                // -------- info banner --------
                Logger.Instance.WriteToConsoleAndLog("");
                Logger.Instance.WriteToConsoleAndLog($"=== Server Ready ({cfg.Transport}) ===");
                Logger.Instance.WriteToConsoleAndLog($"Start Round : {cfg.StartRound}");
                Logger.Instance.WriteToConsoleAndLog($"Max Rounds  : {cfg.MaxRounds}");
                Logger.Instance.WriteToConsoleAndLog("Players:");
                foreach (var p in players) Logger.Instance.WriteToConsoleAndLog($"o {p.Name}");

                if (cfg.NumNets > 0)
                {
                    Logger.Instance.WriteToConsoleAndLog("");
                    if (cfg.Transport == TransportKind.Tcp)
                    {
                        Logger.Instance.WriteToConsoleAndLog("Network player listeners (TCP):");
                        for (int i = 1; i <= cfg.NumNets; i++)
                        {
                            var port = cfg.BasePort + (i - 1);
                            Logger.Instance.WriteToConsoleAndLog($"NET{i}: client connects with: {NetworkUtils.GetLocalIpHint()}:{port}");
                        }
                    }
                    else if (cfg.Transport == TransportKind.FileRpc)
                    {
                        Logger.Instance.WriteToConsoleAndLog("Network player folders (FileRpc):");
                        for (int i = 1; i <= cfg.NumNets; i++)
                        {
                            var netFolder = Path.Combine(cfg.BaseFolder!, $"NET{i}");
                            Logger.Instance.WriteToConsoleAndLog($"NET{i}: client uses folder: {netFolder} (any ClientId)");
                        }
                    }
                    else if (cfg.Transport == TransportKind.WebRpc)
                    {
                        Logger.Instance.WriteToConsoleAndLog("Network player listeners (WebRpc):");
                        for (int i = 1; i <= cfg.NumNets; i++)
                        {
                            Logger.Instance.WriteToConsoleAndLog($"NET{i}: client Base URL: http://127.0.0.1:{cfg.BasePort}/  (any ClientId)");
                        }
                    }
                }

                try
                {
                    var handler = new GameHandler(players, cfg.StartRound, cfg.MaxRounds, controllers, new SkullKingScoring());
                    await handler.RunGameAsync();
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
                        try { await d.DisposeAsync(); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}ERROR:{Environment.NewLine}{ex}");
            }
        }

        // ---------------- parsing ----------------

        private sealed class Config
        {
            public TransportKind Transport;
            public int BasePort;            // for TCP/WebRpc
            public string? BaseFolder;      // for FileRpc
            public int NumLocalHumans;
            public int NumCpus;
            public int NumNets;
            public int StartRound;
            public int MaxRounds;
        }

        private static bool TryParseArgs(string[] args, out Config cfg, out string error)
        {
            cfg = new Config();
            error = string.Empty;

            if (args.Length < 1) { error = "Missing transport."; return false; }

            var t = ParseTransport(args[0]);
            if (t == default) { error = $"Unknown transport '{args[0]}'."; return false; }
            cfg.Transport = t;

            // Expected shapes:
            // tcp  : tcp  <basePort> <numLocalHumans> <numCpus> <numNets> <startRound> <maxRounds>
            // file : file <baseFolder> <numLocalHumans> <numCpus> <numNets> <startRound> <maxRounds>
            // http : http <basePort> <numLocalHumans> <numCpus> <numNets> <startRound> <maxRounds>

            try
            {
                switch (t)
                {
                    case TransportKind.Tcp:
                    case TransportKind.WebRpc:
                        {
                            if (args.Length < 7) { error = "Not enough arguments."; return false; }
                            cfg.BasePort = ParseInt(args[1], 0, 65535, "basePort");
                            cfg.NumLocalHumans = ParseInt(args[2], 0, 8, "numLocalHumans");
                            cfg.NumCpus = ParseInt(args[3], 0, 8, "numCpus");
                            cfg.NumNets = ParseInt(args[4], 0, 8, "numNets");
                            cfg.StartRound = ParseInt(args[5], 1, 10, "startRound");
                            cfg.MaxRounds = ParseInt(args[6], 1, 10, "maxRounds");
                            break;
                        }
                    case TransportKind.FileRpc:
                        {
                            if (args.Length < 7) { error = "Not enough arguments."; return false; }
                            cfg.BaseFolder = args[1].Trim();
                            if (string.IsNullOrWhiteSpace(cfg.BaseFolder)) { error = "baseFolder must not be empty."; return false; }
                            cfg.NumLocalHumans = ParseInt(args[2], 0, 8, "numLocalHumans");
                            cfg.NumCpus = ParseInt(args[3], 0, 8, "numCpus");
                            cfg.NumNets = ParseInt(args[4], 0, 8, "numNets");
                            cfg.StartRound = ParseInt(args[5], 1, 10, "startRound");
                            cfg.MaxRounds = ParseInt(args[6], 1, 10, "maxRounds");
                            Directory.CreateDirectory(cfg.BaseFolder);
                            break;
                        }
                }
            }
            catch (FormatException fe)
            {
                error = fe.Message;
                return false;
            }

            return true;
        }

        private static int ParseInt(string value, int min, int max, string name)
        {
            if (!int.TryParse(value, out var n))
                throw new FormatException($"'{name}' must be an integer.");
            if (n < min || n > max)
                throw new FormatException($"'{name}' must be in range [{min}..{max}].");
            return n;
        }

        private static TransportKind ParseTransport(string value)
        {
            if (int.TryParse(value, out var n))
            {
                return n switch
                {
                    1 => TransportKind.Tcp,
                    2 => TransportKind.FileRpc,
                    3 => TransportKind.WebRpc,
                    _ => default
                };
            }
            var s = value.Trim().ToLowerInvariant();
            return s switch
            {
                "tcp" => TransportKind.Tcp,
                "socket" => TransportKind.Tcp,
                "sockets" => TransportKind.Tcp,

                "file" => TransportKind.FileRpc,
                "filerpc" => TransportKind.FileRpc,

                "http" => TransportKind.WebRpc,
                "WebRpc" => TransportKind.WebRpc,
                "http-longpoll" => TransportKind.WebRpc,
                _ => default
            };
        }

        private static void PrintUsage()
        {
            Logger.Instance.WriteToConsoleAndLog("Usage:");
            Logger.Instance.WriteToConsoleAndLog("  TCP   : SkullKingServerConsole.exe tcp <basePort> <numLocalHumans> <numCpus> <numNets> <startRound> <maxRounds>");
            Logger.Instance.WriteToConsoleAndLog(@"  File  : SkullKingServerConsole.exe file <baseFolder> <numLocalHumans> <numCpus> <numNets> <startRound> <maxRounds>");
            Logger.Instance.WriteToConsoleAndLog("  HTTP  : SkullKingServerConsole.exe http <basePort> <numLocalHumans> <numCpus> <numNets> <startRound> <maxRounds>");
            Logger.Instance.WriteToConsoleAndLog("");
            Logger.Instance.WriteToConsoleAndLog("Examples:");
            Logger.Instance.WriteToConsoleAndLog("  SkullKingServerConsole.exe tcp 1234 0 3 1 5 10");
            Logger.Instance.WriteToConsoleAndLog(@"  SkullKingServerConsole.exe file C:\Temp\SkullKingShare 1 2 1 1 10");
            Logger.Instance.WriteToConsoleAndLog("  SkullKingServerConsole.exe http 5055 0 3 1 5 10");
        }
    }
}
