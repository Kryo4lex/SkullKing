using SkullKingConsole.Controller;
using SkullKingCore.Core.Game;
using SkullKingCore.Core.Game.Interfaces;
using SkullKingCore.Network;
using SkullKingCore.Network.Client;
using SkullKingCore.Network.Server;
using System.Net;
using System.Net.Sockets;

namespace SkullKingConsole
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await RunAsync(args);
        }

        private static async Task RunAsync(string[] args)
        {
            Console.WriteLine("Skull King – Test Harness");
            Console.WriteLine("[H]ost a game  or  [J]oin a game?");
            var mode = ReadKey("H", "J");

            if (mode == "H") await HostFlowAsync();
            else await JoinFlowAsync();
        }

        // ──────────────────────────────────────────────────────────────────
        // HOST FLOW
        // ──────────────────────────────────────────────────────────────────
        private static async Task HostFlowAsync()
        {
            string ip = ReadString("Listen IP", "127.0.0.1");
            int port = ReadInt("Listen port", 5055, min: 1, max: 65535);

            int netSeats = ReadInt("Number of NETWORK players (remote)", 1, min: 0, max: 8);
            int humanSeats = ReadInt("Number of LOCAL HUMAN players", 1, min: 0, max: 8);
            int botSeats = ReadInt("Number of CPU/BOT players", 0, min: 0, max: 8);

            int totalPlayers = netSeats + humanSeats + botSeats;
            if (totalPlayers < 2)
            {
                Console.WriteLine("You need at least 2 total players. Aborting.");
                return;
            }

            int startRound = ReadInt("Start round", 1, 1, 10);
            int maxRounds = ReadInt("Max rounds", Math.Max(3, startRound), startRound, 10);

            // 1) Start listener & accept network players (if any)
            var links = new List<INetworkLink>();
            TcpListener? listener = null;
            if (netSeats > 0)
            {
                listener = new TcpListener(IPAddress.Parse(ip), port);
                listener.Start();
                Console.WriteLine($"[SERVER] Listening on {ip}:{port} — waiting for {netSeats} network player(s) to connect…");

                for (int i = 0; i < netSeats; i++)
                {
                    var link = await TcpJsonLink.AcceptAsync(listener);
                    links.Add(link);
                    _ = link.RunAsync(); // background receive
                    Console.WriteLine($"[SERVER] Network player {i + 1} connected.");
                }
            }

            // 2) Build player objects & controllers
            var players = new List<Player>(totalPlayers);
            var controllers = new Dictionary<string, IGameController>(totalPlayers);

            int seatIndex = 0;

            // Local humans
            for (int i = 0; i < humanSeats; i++, seatIndex++)
            {
                var id = $"P{seatIndex + 1}";
                var name = $"LocalHuman{(i + 1)}";
                players.Add(new Player(id, name));
                controllers[id] = new LocalConsoleHumanController(name);
            }

            // Bots (your LocalConsoleCPUController, or swap to a simple DumbCpuController)
            for (int i = 0; i < botSeats; i++, seatIndex++)
            {
                var id = $"P{seatIndex + 1}";
                var name = $"CPU{i + 1}";
                players.Add(new Player(id, name));
                controllers[id] = new LocalConsoleCPUController(name);
            }

            // Network
            for (int i = 0; i < netSeats; i++, seatIndex++)
            {
                var id = $"P{seatIndex + 1}";
                var name = $"Net{i + 1}";
                players.Add(new Player(id, name));
                controllers[id] = new NetworkController(id, name, links[i]);
            }

            Console.WriteLine($"[SERVER] Seating complete → {players.Count} players.");
            foreach (var p in players)
                Console.WriteLine($"  - {p.Id}: {p.Name} ({controllers[p.Id].GetType().Name})");

            // 3) Run the game
            var handler = new GameHandler(players, startRound, maxRounds, controllers);
            await handler.RunGameAsync();

            // 4) Cleanup
            foreach (var link in links)
                await link.DisposeAsync();
            listener?.Stop();

            Console.WriteLine("[SERVER] Game finished. Press Enter to exit.");
            Console.ReadLine();
        }

        // ──────────────────────────────────────────────────────────────────
        // JOIN FLOW (client)
        // ──────────────────────────────────────────────────────────────────
        private static async Task JoinFlowAsync()
        {
            string host = ReadString("Server IP", "127.0.0.1");
            int port = ReadInt("Server port", 5055, min: 1, max: 65535);
            string playerName = ReadString("Your player name", "Player");

            Console.WriteLine($"[CLIENT] Connecting to {host}:{port} as {playerName}…");
            await using var link = await TcpJsonLink.ConnectAsync(host, port);
            var agent = new ConsoleClientAgent(playerName);
            var adapter = new ControllerNetworkAdapter(link, agent);
            adapter.Register();

            Console.WriteLine("[CLIENT] Connected. Waiting for server…");
            await link.RunAsync();

            Console.WriteLine("[CLIENT] Disconnected. Press Enter to exit.");
            Console.ReadLine();
        }

        // ──────────────────────────────────────────────────────────────────
        // Small helpers
        // ──────────────────────────────────────────────────────────────────
        private static string ReadKey(params string[] allowed)
        {
            var normalized = allowed.Select(a => a.Trim().ToUpperInvariant()).ToHashSet();
            while (true)
            {
                Console.Write("> ");
                var input = Console.ReadLine()?.Trim().ToUpperInvariant();
                if (!string.IsNullOrEmpty(input) && normalized.Contains(input))
                    return input;
                Console.WriteLine($"Please enter one of: {string.Join("/", allowed)}");
            }
        }

        private static string ReadString(string prompt, string def)
        {
            Console.Write($"{prompt} [{def}]: ");
            var s = Console.ReadLine();
            return string.IsNullOrWhiteSpace(s) ? def : s.Trim();
        }

        private static int ReadInt(string prompt, int def, int min = int.MinValue, int max = int.MaxValue)
        {
            while (true)
            {
                Console.Write($"{prompt} [{def}]: ");
                var s = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(s)) return def;
                if (int.TryParse(s, out var v) && v >= min && v <= max) return v;
                Console.WriteLine($"Enter a number between {min} and {max}.");
            }
        }
    }
}
