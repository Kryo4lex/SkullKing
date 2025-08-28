using SkullKingCore.Controller;
using SkullKingCore.Logging;
using SkullKingCore.Network;
using SkullKingCore.Network.FileRpc;
using SkullKingCore.Network.FileRpc.Rpc;
using SkullKingCore.Network.TCP;
using SkullKingCore.Network.TCP.Rpc;
using SkullKingCore.Network.WebRpc.Rpc;
using SkullKingCore.Utility;

namespace SkullKingClientConsole
{

    public static class MainClientConsole
    {

        public static async Task Main()
        {
            try
            {

                TransportKind transport = UserConsoleIO.PromptTransportKind();

                Console.Title = $"Skull King Console Network Client Human ({Misc.GetEnumLabel(transport)})";

                var controller = new ConsoleHumanController();

                switch (transport)
                {
                    case TransportKind.Tcp:
                    {
                        string host;
                        int port;

                        UserConsoleIO.ParseHostPortUntilValid("Enter Host:Port, e.g. 127.0.0.1:1234 :", out host, out port);

                        Logger.Instance.WriteToConsoleAndLog($"Connecting to {host}:{port} ...");

                        await using var conn = await TcpRpcConnection.ConnectAsync(host, port, CancellationToken.None);

                        Logger.Instance.WriteToConsoleAndLog("Connected. Waiting for game requests...\n");

                        var dispatcher = new RpcTcpDispatcher<ConsoleHumanController>(controller);
                        await conn.RunClientLoopAsync(dispatcher.DispatchAsync);
                        break;
                    }

                    case TransportKind.FileRpc:
                    {
                        Logger.Instance.WriteToConsoleAndLog("Enter ClientId (e.g., NET1): ");

                        var clientId = (Console.ReadLine() ?? string.Empty).Trim();

                        if (string.IsNullOrWhiteSpace(clientId))
                        {
                            clientId = Environment.UserName;
                        }

                        Logger.Instance.WriteToConsoleAndLog("Enter shared folder path (e.g., C:\\Temp\\SkullKingShare\\NET1): ");

                        var folder = (Console.ReadLine() ?? string.Empty).Trim();

                        if (string.IsNullOrWhiteSpace(folder))
                        {
                            Logger.Instance.WriteToConsoleAndLog("No folder provided. Aborting.");
                            return;
                        }

                        Logger.Instance.WriteToConsoleAndLog($"Connecting via files in '{folder}' as '{clientId}' ...");

                        await using var conn = await FileRpcConnection.ConnectAsync(clientId, folder, CancellationToken.None);

                        Logger.Instance.WriteToConsoleAndLog("Connected. Waiting for game requests...\n");

                        var dispatcher = new RpcFileDispatcher<ConsoleHumanController>(controller);

                        // bridge nullability: FileRpc RunClientLoopAsync may pass a null args array
                        await conn.RunClientLoopAsync((method, args) =>
                            dispatcher.DispatchAsync(method, args ?? Array.Empty<object?>()));

                        break;
                    }
                    case TransportKind.WebRpc:
                    {
                        Logger.Instance.WriteToConsoleAndLog("Enter Base URL (e.g., http://localhost:5055/): ");

                        var baseUrl = (Console.ReadLine() ?? string.Empty).Trim();

                        Logger.Instance.WriteToConsoleAndLog("Enter ClientId (e.g., NET1): ");

                        var clientId = (Console.ReadLine() ?? string.Empty).Trim();

                        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(clientId))
                        {
                            Logger.Instance.WriteToConsoleAndLog("Base URL and ClientId required.");

                            return;
                        }

                        Logger.Instance.WriteToConsoleAndLog($"Connecting via WebRpc {baseUrl} as '{clientId}' ...");

                        await using var conn = await WebRpcConnection.ConnectAsync(baseUrl, clientId, CancellationToken.None);

                        // Reuse file dispatcher (same method map)
                        var dispatcher = new RpcFileDispatcher<ConsoleHumanController>(controller);

                        Logger.Instance.WriteToConsoleAndLog("Connected. Waiting for game requests...\n");
                            
                        await conn.RunClientLoopAsync((m, a) => dispatcher.DispatchAsync(m, a ?? Array.Empty<object?>()));

                        break;
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