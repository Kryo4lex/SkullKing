using SkullKing.Network.Rpc;
using SkullKingCore.Controller;
using SkullKingCore.Network;
using SkullKingCore.Utility.UserInput;
using System.Security.Cryptography;

public static class MainClientConsole
{

    async static Task Main()
    {
        Console.Title = "Skull King Console Network Client Human";

        /*
        string host = "127.0.0.1"; ;
        int port = 1234;
        */
        
        Console.WriteLine("Enter IP/Host:");
        string host = Console.ReadLine()!;
        int port = UserInput.ReadIntUntilValid($"{Environment.NewLine}Enter Port", 0, 65535);
        

        Console.WriteLine($"Connecting to {host}:{port} ...");

        await using var conn = await RpcConnection.ConnectAsync(host, port, CancellationToken.None);

        Console.WriteLine("Connected. Waiting for game requests...\n");

        var controller = new ConsoleHumanController();
        var dispatcher = new RpcTcpDispatcher<ConsoleHumanController>(controller);

        await conn.RunClientLoopAsync(dispatcher.DispatchAsync);
    }

}
