namespace SkullKingCore.Network;

// Requests & Responses
public sealed class BidRequest { public int Round { get; init; } public int MinBid { get; init; } public int MaxBid { get; init; } }
public sealed class BidResponse { public int Bid { get; init; } public BidResponse() { } public BidResponse(int bid) { Bid = bid; } }

public sealed class CardLabel { public string Label { get; init; } = ""; }

public sealed class CardPlayRequest
{
    public string PlayerName { get; init; } = "";
    public List<CardLabel> Allowed { get; init; } = new();
    public bool RequireTigressMode { get; init; }
}
public sealed class CardPlayResponse
{
    public int ChoiceIndex { get; init; }
    public string? TigressMode { get; init; } // "ESCAPE" | "PIRATE" if TIGRESS chosen
}

// Events (reference types)
public sealed class GameStartedEvt { public int Round { get; init; } public List<PlayerNameDto> Players { get; init; } = new(); }
public sealed class PlayerNameDto { public string Id { get; init; } = ""; public string Name { get; init; } = ""; }

public sealed class RoundStartedEvt { public int Round { get; init; } }
public sealed class BidAnnouncedEvt { public string By { get; init; } = ""; public int Bid { get; init; } }
public sealed class NotAllPlayableEvt
{
    public List<CardLabel> Allowed { get; init; } = new();
    public List<CardLabel> NotAllowed { get; init; } = new();
}
public sealed class CardPlayedEvt { public string By { get; init; } = ""; public string Card { get; init; } = ""; }
public sealed class SubRoundPhaseEvt { public int SubRound { get; init; } }
public sealed class SubRoundWinnerEvt
{
    public string? Winner { get; init; }
    public string? WinningCard { get; init; }
    public int Round { get; init; }
}
public sealed class GameEndedEvt { }
public sealed class MessageEvt { public string Message { get; init; } = ""; }

public sealed class PlayerTimedOutEvt { public string PlayerName { get; init; } = ""; public string Phase { get; init; } = ""; }
