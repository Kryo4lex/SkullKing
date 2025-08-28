using SkullKingCore.Controller;
using SkullKingCore.Logging;
using SkullKingCore.Network;
using SkullKingCore.Network.FileRpc;
using SkullKingCore.Network.FileRpc.Rpc;
using SkullKingCore.Network.TCP;
using SkullKingCore.Network.TCP.Rpc;
using SkullKingCore.Network.WebRpc.Rpc;
using SkullKingCore.Utility;
using System.Globalization;

namespace SkullKingClientConsole
{
    public static class MainClientConsole
    {
        public static async Task Main(string[]? args = null)
        {
            try
            {
                args ??= Array.Empty<string>();

                // 1) Transport selection: from args[0] if present, else prompt
                TransportKind transport;
                if (args.Length > 0 && TryParseTransport(args[0], out var t))
                {
                    transport = t;
                }
                else
                {
                    transport = UserConsoleIO.PromptTransportKind();
                }

                Console.Title = $"Skull King Console Network Client Human ({Misc.GetEnumLabel(transport)})";

                var controller = new ConsoleHumanController();

                switch (transport)
                {
                    case TransportKind.Tcp:
                        {
                            // Expected arguments:
                            //   args[0] = transport
                            //   args[1] = "host:port"  (optional; if not provided, prompt)
                            string host;
                            int port;

                            if (args.Length >= 2 && TryParseHostPort(args[1], out host, out port))
                            {
                                // ok
                            }
                            else
                            {
                                // interactive fallback
                                UserConsoleIO.ParseHostPortUntilValid("Enter Host:Port, e.g. 127.0.0.1:1234 :", out host, out port);
                            }

                            Logger.Instance.WriteToConsoleAndLog($"Connecting to {host}:{port} ...");

                            await using var conn = await TcpRpcConnection.ConnectAsync(host, port, CancellationToken.None);

                            Logger.Instance.WriteToConsoleAndLog("Connected. Waiting for game requests...\n");

                            var dispatcher = new RpcTcpDispatcher<ConsoleHumanController>(controller);
                            await conn.RunClientLoopAsync(dispatcher.DispatchAsync);
                            break;
                        }

                    case TransportKind.FileRpc:
                        {
                            // Expected arguments:
                            //   args[0] = transport
                            //   args[1] = clientId (e.g. NET1)
                            //   args[2..] = folder (may contain spaces; we join remainder)
                            string clientId;
                            string folder;

                            if (args.Length >= 2)
                                clientId = args[1].Trim();
                            else
                            {
                                Logger.Instance.WriteToConsoleAndLog("Enter ClientId (e.g., NET1): ");
                                clientId = (Console.ReadLine() ?? string.Empty).Trim();
                            }
                            if (string.IsNullOrWhiteSpace(clientId))
                                clientId = Environment.UserName;

                            if (args.Length >= 3)
                                folder = string.Join(" ", args.Skip(2)).Trim(); // supports quoted or unquoted multi-token paths
                            else
                            {
                                Logger.Instance.WriteToConsoleAndLog("Enter shared folder path (e.g., C:\\Temp\\SkullKingShare\\NET1): ");
                                folder = (Console.ReadLine() ?? string.Empty).Trim();
                            }

                            if (string.IsNullOrWhiteSpace(folder))
                            {
                                Logger.Instance.WriteToConsoleAndLog("No folder provided. Aborting.");
                                return;
                            }

                            Logger.Instance.WriteToConsoleAndLog($"Connecting via files in '{folder}' as '{clientId}' ...");

                            await using var conn = await FileRpcConnection.ConnectAsync(clientId, folder, CancellationToken.None);

                            Logger.Instance.WriteToConsoleAndLog("Connected. Waiting for game requests...\n");

                            var dispatcher = new RpcFileDispatcher<ConsoleHumanController>(controller);

                            // Bridge nullability: FileRpc RunClientLoopAsync may pass a null args array
                            await conn.RunClientLoopAsync((method, a) =>
                                dispatcher.DispatchAsync(method, a ?? Array.Empty<object?>()));

                            break;
                        }

                    case TransportKind.WebRpc:
                        {
                            // Expected arguments:
                            //   args[0] = transport
                            //   args[1] = baseUrl  (e.g., http://localhost:5055/)
                            //   args[2] = clientId (e.g. NET1)
                            string baseUrl;
                            string clientId;

                            if (args.Length >= 2)
                                baseUrl = args[1].Trim();
                            else
                            {
                                Logger.Instance.WriteToConsoleAndLog("Enter Base URL (e.g., http://localhost:5055/): ");
                                baseUrl = (Console.ReadLine() ?? string.Empty).Trim();
                            }

                            if (args.Length >= 3)
                                clientId = args[2].Trim();
                            else
                            {
                                Logger.Instance.WriteToConsoleAndLog("Enter ClientId (e.g., NET1): ");
                                clientId = (Console.ReadLine() ?? string.Empty).Trim();
                            }

                            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(clientId))
                            {
                                Logger.Instance.WriteToConsoleAndLog("Base URL and ClientId required.");
                                return;
                            }

                            Logger.Instance.WriteToConsoleAndLog($"Connecting via WebRpc {baseUrl} as '{clientId}' ...");

                            await using var conn = await WebRpcConnection.ConnectAsync(baseUrl, clientId, CancellationToken.None);

                            // Reuse file dispatcher (same IGameController method map)
                            var dispatcher = new RpcFileDispatcher<ConsoleHumanController>(controller);

                            Logger.Instance.WriteToConsoleAndLog("Connected. Waiting for game requests...\n");

                            await conn.RunClientLoopAsync((m, a) =>
                                dispatcher.DispatchAsync(m, a ?? Array.Empty<object?>()));

                            break;
                        }

                    default:
                        Logger.Instance.WriteToConsoleAndLog($"Unsupported transport: {transport}");
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}ERROR:{Environment.NewLine}{ex}");
                Console.ReadLine();
            }
        }

        // ---------------- helpers ----------------

        private static bool TryParseTransport(string value, out TransportKind transport)
        {
            transport = default;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            // Accept numeric ("1", "2", "3"), enum name ("Tcp","FileRpc","WebRpc"), or common aliases
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) &&
                Enum.IsDefined(typeof(TransportKind), n))
            {
                transport = (TransportKind)n;
                return true;
            }

            var s = value.Trim().ToLowerInvariant();

            if (s is "tcp")
            {
                transport = TransportKind.Tcp; return true;
            }
            if (s is "file" or "filerpc" or "file-rpc")
            {
                transport = TransportKind.FileRpc; return true;
            }
            if (s is "web" or "webrpc" or "http" or "httprpc" or "web-rpc")
            {
                transport = TransportKind.WebRpc; return true;
            }

            // Try enum parse by name (case-insensitive)
            if (Enum.TryParse<TransportKind>(value, true, out var parsed))
            {
                transport = parsed;
                return true;
            }

            return false;
        }

        private static bool TryParseHostPort(string input, out string host, out int port)
        {
            host = string.Empty;
            port = 0;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            // Split on last ':' to support IPv6 literals if bracketed elsewhere; here we keep it simple.
            var idx = input.LastIndexOf(':');
            if (idx <= 0 || idx == input.Length - 1)
                return false;

            host = input[..idx].Trim();
            var portStr = input[(idx + 1)..].Trim();

            if (string.IsNullOrWhiteSpace(host))
                return false;

            if (!int.TryParse(portStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out port))
                return false;

            return port >= 0 && port <= 65535;
        }
    }
}
