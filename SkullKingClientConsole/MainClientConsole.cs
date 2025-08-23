using SkullKing.Network.Rpc;
using SkullKingCore.Controller;
using SkullKingCore.Logging;
using SkullKingCore.Network;
using SkullKingCore.Utility.UserInput;

public static class MainClientConsole
{

    public async static Task Main()
    {
        try
        {
            Console.Title = "Skull King Console Network Client Human";

            string host = "127.0.0.1"; ;
            int port = 1234;

            /*
            Console.WriteLine("Enter IP/Host:");
            string host = Console.ReadLine()!;
            int port = UserInput.ReadIntUntilValid($"{Environment.NewLine}Enter Port", 0, 65535);
            */

            Console.WriteLine($"Connecting to {host}:{port} ...");

            await using var conn = await RpcConnection.ConnectAsync(host, port, CancellationToken.None);

            Console.WriteLine("Connected. Waiting for game requests...\n");

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
