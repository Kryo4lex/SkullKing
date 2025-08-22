#nullable enable

using SkullKing.Network.Rpc.Dtos;
using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Game.Interfaces;
using SkullKingCore.GameDefinitions;
using SkullKingCore.Logging;
using SkullKingCore.Network.Networking.Payload;
using SkullKingCore.Network.Rpc;
using SkullKingCore.Utility.UserInput;
using System.Text.Json;

internal static class Program
{

    private sealed class ShowMessagePayload { public string Message { get; set; } = ""; }

    // ===== JSON options =====
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string Name = "NET1";

    // ===== Main =====
    private static async Task Main()
    {
        Console.Title = "Skull King Network Client (JSON DTO)";

        //Console.Write("Enter server host [127.0.0.1]: ");
        var host = "127.0.0.1";//ReadOrDefault("127.0.0.1");

        //Console.Write("Enter server port [1234]: ");
        //var portText = Console.ReadLine();
        int port = 1234;//if (!int.TryParse(portText, out var port)) port = 1234;

        Console.WriteLine($"Connecting to {host}:{port} ...");
        await using var conn = await RpcConnection.ConnectAsync(host, port, DispatchAsync, CancellationToken.None);
        Console.WriteLine("Connected. Waiting for game requests...\n");

        await Task.Delay(Timeout.Infinite);
    }

    // ===== Dispatcher for all RPC methods =====
    // ===== Dispatcher for all RPC methods (refactored) =====
    private static Task<object?> DispatchAsync(string method, JsonElement? payload) =>
        method switch
        {
            nameof(IGameController.NotifyRoundStartedAsync) => Handle_NotifyRoundStartedAsync(payload),
            nameof(IGameController.NotifyBidCollectionStartedAsync) => Handle_NotifyBidCollectionStartedAsync(payload),
            nameof(IGameController.WaitForBidsReceivedAsync) => Handle_WaitForBidsReceivedAsync(payload),
            nameof(IGameController.RequestBidAsync) => Handle_RequestBidAsync(payload),
            nameof(IGameController.AnnounceBidAsync) => Handle_AnnounceBidAsync(payload),
            nameof(IGameController.NotifyNotAllCardsInHandCanBePlayed) => Handle_NotifyNotAllCardsInHandCanBePlayed(payload),
            nameof(IGameController.RequestCardPlayAsync) => Handle_RequestCardPlayAsync(payload),
            nameof(IGameController.NotifyCardPlayedAsync) => Handle_NotifyCardPlayedAsync(payload),
            nameof(IGameController.NotifyAboutSubRoundWinnerAsync) => Handle_NotifyAboutSubRoundWinnerAsync(payload),
            nameof(IGameController.NotifyGameStartedAsync) => Handle_NotifyGameStartedAsync(payload),
            nameof(IGameController.NotifyAboutSubRoundStartAsync) => Handle_NotifyAboutSubRoundStartAsync(payload),
            nameof(IGameController.NotifyAboutSubRoundEndAsync) => Handle_NotifyAboutSubRoundEndAsync(payload),
            nameof(IGameController.NotifyGameEndedAsync) => Handle_NotifyGameEndedAsync(payload),
            nameof(IGameController.NotifyAboutGameWinnerAsync) => Handle_NotifyAboutGameWinnerAsync(payload),
            nameof(IGameController.ShowMessageAsync) => Handle_ShowMessageAsync(payload),
            nameof(IGameController.NotifyPlayerTimedOutAsync) => Handle_NotifyPlayerTimedOutAsync(payload),
            _ => throw new InvalidOperationException($"Unknown method: {method}")
        };

    // ===== Handlers (one per RPC) =====

    private static Task<object?> Handle_NotifyRoundStartedAsync(JsonElement? payload)
    {
        //var p = payload!.Value.Deserialize<NotifyRoundStartedPayload>(Json)!;

        Console.Clear();

        Logger.Instance.WriteToConsoleAndLog($"--- Game started ---");

        return Task.FromResult<object?>(null);
    }

    private static Task<object?> Handle_NotifyBidCollectionStartedAsync(JsonElement? payload)
    {
        //var p = payload!.Value.Deserialize<NotifyBidCollectionStartedPayload>(Json)!;

        Logger.Instance.WriteToConsoleAndLog("Collecting bids...");

        return Task.FromResult<object?>(null);
    }

    private static Task<object?> Handle_WaitForBidsReceivedAsync(JsonElement? payload)
    {
        //var p = payload!.Value.Deserialize<WaitForBidsReceivedPayload>(Json)!;

        Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}Press Enter to confirm, that you have acknowledged the Bids.");

        Console.ReadLine();

        return Task.FromResult<object?>(null);
    }

    private static Task<object?> Handle_RequestBidAsync(JsonElement? payload)
    {
        var p = payload!.Value.Deserialize<RequestBidPayload>(Json)!;

        List<Card> playerHand = DtoMapper.CardsFromDtos(p.Hand);

        Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}Your cards:");
        Card.PrintListFancy(playerHand);

        int bid = 0;

        while (!UserInput.TryReadInt($"{Environment.NewLine}Enter your number of wins prediction:", out bid, 0, p.GameState.CurrentRound))
        {

        }

        Logger.Instance.WriteToConsoleAndLog($"");

        return Task.FromResult<object?>(new RequestBidResult { Bid = bid });
    }

    private static Task<object?> Handle_AnnounceBidAsync(JsonElement? payload)
    {
        var p = payload!.Value.Deserialize<AnnounceBidPayload>(Json)!;
        var round = p.GameState.CurrentRound;

        Logger.Instance.WriteToConsoleAndLog("== Current Bids ==");

        foreach (var pl in p.GameState.Players)
        {
            // Find this player's bid for the current round, if any
            var bidDto = pl.Bids.FirstOrDefault(b => b.Round == round);

            if (bidDto is not null)
            {
                // Highlight the player who just announced (optional)
                var marker = (pl.Id == p.Player.Id) ? "*" : " ";
                Logger.Instance.WriteToConsoleAndLog($"{marker} {pl.Name} bids {bidDto.PredictedWins}");
            }
            else
            {
                Logger.Instance.WriteToConsoleAndLog($"  {pl.Name} has not bid yet");
            }
        }

        return Task.FromResult<object?>(null);
    }

    private static Task<object?> Handle_NotifyNotAllCardsInHandCanBePlayed(JsonElement? payload)
    {
        var p = payload!.Value.Deserialize<NotAllCardsPlayablePayload>(Json)!;

        Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}Not all Cards in your hand can be played due to lead color/suit rule. Cards you are not allowed to play:");

        Card.PrintListFancy(DtoMapper.CardsFromDtos(p.NotAllowed));

        return Task.FromResult<object?>(null);
    }

    private static Task<object?> Handle_RequestCardPlayAsync(JsonElement? payload)
    {
        var p = payload!.Value.Deserialize<RequestCardPlayPayload>(Json)!;

        int cardToPlayIndex = 0;

        Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}Cards you can play:");

        List<Card> hand = DtoMapper.CardsFromDtos(p.Hand);

        Card.PrintListFancy(hand);

        while (!UserInput.TryReadInt($"{Environment.NewLine}Enter the index of the card you want to play:", out cardToPlayIndex, 0, hand.Count - 1))
        {

        }

        Card card = hand[cardToPlayIndex];

        CardType playedAs = CardType.ESCAPE;

        //Special Case
        if (card.CardType == CardType.TIGRESS)
        {

            string? choice;
            do
            {
                Logger.Instance.WriteToConsoleAndLog("Enter E (Escape) or P (Pirate) and confirm: ");
                choice = Console.ReadLine()?.Trim().ToUpperInvariant();
            }
            while (choice != "E" && choice != "P");

            switch (choice)
            {
                case "E":
                    playedAs = CardType.ESCAPE;
                    break;
                case "P":
                    playedAs = CardType.PIRATE;
                    break;
            }

        }

        Logger.Instance.WriteToConsoleAndLog($"{Name} plays {card}");

        return Task.FromResult<object?>(new RequestCardPlayResult { Index = cardToPlayIndex, TigressMode = playedAs.ToString() });
    }

    private static Task<object?> Handle_NotifyCardPlayedAsync(JsonElement? payload)
    {
        var p = payload!.Value.Deserialize<NotifyCardPlayedPayload>(Json)!;

        Logger.Instance.WriteToConsoleAndLog($"{p.Player.Name} played {p.Card.Display}");

        return Task.FromResult<object?>(null);
    }

    private static Task<object?> Handle_NotifyAboutSubRoundWinnerAsync(JsonElement? payload)
    {
        var p = payload!.Value.Deserialize<SubRoundWinnerPayload>(Json)!;

        if (p.Player == null)
        {
            Logger.Instance.WriteToConsoleAndLog($"None!");
        }
        else
        {
            Logger.Instance.WriteToConsoleAndLog($"{p.Player.Name} won round {p.Round} with {DtoMapper.ToDomainCard(p.WinningCard)}");
        }

        return Task.FromResult<object?>(null);
    }

    private static Task<object?> Handle_NotifyGameStartedAsync(JsonElement? payload)
    {
        var p = payload!.Value.Deserialize<NotifyGameStartedPayload>(Json)!;

        Console.Clear();

        Logger.Instance.WriteToConsoleAndLog($"--- Game started ---");

        return Task.FromResult<object?>(null);
    }

    private static Task<object?> Handle_NotifyAboutSubRoundStartAsync(JsonElement? payload)
    {
        var p = payload!.Value.Deserialize<NotifySubRoundStartPayload>(Json)!;

        Console.Clear();
        Logger.Instance.WriteToConsoleAndLog($"--- Sub round {p.GameState.CurrentSubRound}/{p.GameState.MaxRounds} started ---");

        return Task.FromResult<object?>(null);
    }

    private static Task<object?> Handle_NotifyAboutSubRoundEndAsync(JsonElement? payload)
    {
        var p = payload!.Value.Deserialize<NotifySubRoundEndPayload>(Json)!;

        Logger.Instance.WriteToConsoleAndLog($"--- Sub round {p.GameState.CurrentSubRound}/{p.GameState.MaxRounds} ended ---");
        Logger.Instance.WriteToConsoleAndLog($"Press Any Key to continue");

        return Task.FromResult<object?>(null);
    }

    private static Task<object?> Handle_NotifyGameEndedAsync(JsonElement? payload)
    {
        var p = payload!.Value.Deserialize<NotifyGameEndedPayload>(Json)!;

        Logger.Instance.WriteToConsoleAndLog($"--- Game finished ---");

        return Task.FromResult<object?>(null);
    }

    private static Task<object?> Handle_NotifyAboutGameWinnerAsync(JsonElement? payload)
    {
        var p = payload!.Value.Deserialize<GameWinnersPayload>(Json)!;

        Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}Winner(s):");

        foreach (var w in p.Winners)
        {
            Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}{w.Name}");
        }

        return Task.FromResult<object?>(null);
    }

    private static Task<object?> Handle_ShowMessageAsync(JsonElement? payload)
    {
        var p = payload!.Value.Deserialize<ShowMessagePayload>(Json)!;

        Logger.Instance.WriteToConsoleAndLog($"[MESSAGE] {p.Message}");

        return Task.FromResult<object?>(null);
    }

    private static Task<object?> Handle_NotifyPlayerTimedOutAsync(JsonElement? payload)
    {
        var p = payload!.Value.Deserialize<PlayerTimedOutPayload>(Json)!;
        Console.WriteLine($"[TIMEOUT] {p.Player.Name}");
        return Task.FromResult<object?>(null);
    }

    // ===== Console helpers =====

    private static string ReadOrDefault(string fallback)
    {
        var s = Console.ReadLine();
        return string.IsNullOrWhiteSpace(s) ? fallback : s.Trim();
    }

}
