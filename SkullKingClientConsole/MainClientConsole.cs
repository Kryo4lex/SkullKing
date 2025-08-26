using SkullKingCore.Controller;
using SkullKingCore.Logging;
using SkullKingCore.Network;
using SkullKingCore.Network.FileRpc;
// transport-specific namespaces
using SkullKingCore.Network.TCP;
using SkullKingCore.Utility;

using FileRpcConnection = SkullKingCore.Network.FileRpc.Rpc.RpcConnection;
// disambiguate RpcConnection types
using TcpRpcConnection = SkullKingCore.Network.TCP.Rpc.RpcConnection;

namespace SkullKingClientConsole
{

    public static class MainClientConsole
    {

        public static async Task Main()
        {
            try
            {
                Console.WriteLine("Select network transport:");
                Console.WriteLine("  1) TCP (sockets)");
                Console.WriteLine("  2) FileRpc (shared folder)");
                var choice = UserInput.ReadIntUntilValid("Enter 1 or 2", 1, 2);
                var transport = (TransportKind)choice;

                Console.Title = transport switch
                {
                    TransportKind.Tcp => "Skull King Console Network Client Human (TCP)",
                    TransportKind.FileRpc => "Skull King Console Network Client Human (FileRpc)",
                    _ => "Skull King Console Network Client Human"
                };

                var controller = new ConsoleHumanController();

                switch (transport)
                {
                    case TransportKind.Tcp:
                        {
                            // Host:Port input (same UX as your original TCP client)
                            string host;
                            int port;
                            UserInput.ParseHostPortUntilValid("Enter Host:Port, e.g. 127.0.0.1:1234 :", out host, out port);

                            Logger.Instance.WriteToConsoleAndLog($"Connecting to {host}:{port} ...");

                            await using var conn = await TcpRpcConnection.ConnectAsync(host, port, CancellationToken.None);

                            Logger.Instance.WriteToConsoleAndLog("Connected. Waiting for game requests...\n");

                            var dispatcher = new RpcTcpDispatcher<ConsoleHumanController>(controller);
                            await conn.RunClientLoopAsync(dispatcher.DispatchAsync);
                            break;
                        }

                    case TransportKind.FileRpc:
                        {
                            Console.Write("Enter ClientId (e.g., NET1): ");
                            var clientId = (Console.ReadLine() ?? string.Empty).Trim();
                            if (string.IsNullOrWhiteSpace(clientId))
                                clientId = Environment.UserName;

                            Console.Write("Enter shared folder path (e.g., C:\\Temp\\SkullKingShare\\NET1): ");
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