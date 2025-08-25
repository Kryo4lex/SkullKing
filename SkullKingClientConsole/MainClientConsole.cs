using SkullKingCore.Controller;
using SkullKingCore.Logging;
using SkullKingCore.Network;
using SkullKingCore.Network.TCP;
using SkullKingCore.Network.TCP.Rpc;
using SkullKingCore.Utility;

public static class MainClientConsole
{

    public async static Task Main()
    {
        try
        {
            Console.Title = "Skull King Console Network Client Human";

            string host;
            int port;

            /*
            host = "127.0.0.1";
            port = 1234;
            */

            /*
            host = Console.ReadLine()!;
            port = UserInput.ReadIntUntilValid($"{Environment.NewLine}Enter Port", 0, 65535);
            */

            UserInput.ParseHostPortUntilValid("Enter Host:Port, e.g. 127.0.0.1:1234 :", out host, out port);

            Logger.Instance.WriteToConsoleAndLog($"Connecting to {host}:{port} ...");

            await using var conn = await RpcConnection.ConnectAsync(host, port, CancellationToken.None);

            Logger.Instance.WriteToConsoleAndLog("Connected. Waiting for game requests...\n");

            var controller = new ConsoleHumanController();
            var dispatcher = new RpcTcpDispatcher<ConsoleHumanController>(controller);

            await conn.RunClientLoopAsync(dispatcher.DispatchAsync);
        }
        catch (Exception ex)
        {
            Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}ERROR:{Environment.NewLine}{ex}");
            Console.ReadLine();
        }
    }


}
