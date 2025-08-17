using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.Implementations;
using SkullKingCore.Core.Game;
using SkullKingCore.Core.Game.Interfaces;
using SkullKingCore.Network;

namespace SkullKingCore.Network.Server;

/// <summary>Server-side IGameController relay; emits/awaits human-readable JSON via INetworkLink.</summary>
public sealed class NetworkController : IGameController
{
    private readonly INetworkLink _link;
    private readonly string _playerId;
    private readonly string _playerName;

    public NetworkController(string playerId, string playerName, INetworkLink link)
    {
        _playerId = playerId ?? throw new ArgumentNullException(nameof(playerId));
        _playerName = playerName ?? throw new ArgumentNullException(nameof(playerName));
        _link = link ?? throw new ArgumentNullException(nameof(link));
        Name = playerName;
    }

    public string Name { get; }

    // Events (typed)
    public Task NotifyGameStartedAsync(GameState state) =>
        _link.SendEventAsync("GameStarted", _playerName, new GameStartedEvt
        {
            Round = state.CurrentRound,
            Players = state.Players.Select(p => new PlayerNameDto { Id = p.Id, Name = p.Name }).ToList()
        });

    public Task NotifyRoundStartedAsync(GameState state) =>
        _link.SendEventAsync("RoundStarted", _playerName, new RoundStartedEvt { Round = state.CurrentRound });

    public Task NotifyBidCollectionStartedAsync(GameState state) =>
        _link.SendEventAsync("BidCollectionStarted", _playerName, new MessageEvt { Message = "Bid collection started." });

    public Task AnnounceBidAsync(GameState state, Player player, int bid, TimeSpan _) =>
        _link.SendEventAsync("BidAnnounced", _playerName, new BidAnnouncedEvt { By = player.Name, Bid = bid });

    public Task NotifyNotAllCardsInHandCanBePlayed(GameState state, List<Card> allowed, List<Card> notAllowed) =>
        _link.SendEventAsync("NotAllPlayable", _playerName, new NotAllPlayableEvt
        {
            Allowed = allowed.Select(c => new CardLabel { Label = CardLabeler.ToLabel(c) }).ToList(),
            NotAllowed = notAllowed.Select(c => new CardLabel { Label = CardLabeler.ToLabel(c) }).ToList()
        });

    public Task NotifyCardPlayedAsync(Player player, Card playedCard) =>
        _link.SendEventAsync("CardPlayed", _playerName, new CardPlayedEvt { By = player.Name, Card = CardLabeler.ToLabel(playedCard) });

    public Task NotifyAboutSubRoundStartAsync(GameState state) =>
        _link.SendEventAsync("SubRoundStarted", _playerName, new SubRoundPhaseEvt { SubRound = state.CurrentSubRound });

    public Task NotifyAboutSubRoundEndAsync(GameState state) =>
        _link.SendEventAsync("SubRoundEnded", _playerName, new SubRoundPhaseEvt { SubRound = state.CurrentSubRound });

    public Task NotifyAboutSubRoundWinnerAsync(Player? player, Card? winningCard, int round) =>
        _link.SendEventAsync("SubRoundWinner", _playerName, new SubRoundWinnerEvt
        {
            Winner = player?.Name,
            WinningCard = winningCard != null ? CardLabeler.ToLabel(winningCard) : null,
            Round = round
        });

    public Task NotifyGameEndedAsync(GameState state) =>
        _link.SendEventAsync("GameEnded", _playerName, new GameEndedEvt());

    public Task NotifyAboutGameWinnerAsync(GameState gameState, List<Player> winners) =>
        _link.SendEventAsync("GameWinners", _playerName, new MessageEvt { Message = "Winners: " + string.Join(", ", winners.Select(w => w.Name)) });

    public Task ShowMessageAsync(string message) =>
        _link.SendEventAsync("Message", _playerName, new MessageEvt { Message = message });

    public Task WaitForBidsReceivedAsync(GameState state) => Task.CompletedTask;

    public Task NotifyPlayerTimedOutAsync(GameState gameState, Player player, string phase) =>
        _link.SendEventAsync("PlayerTimedOut", _playerName, new PlayerTimedOutEvt { PlayerName = player.Name, Phase = phase });

    // Requests
    public async Task<int> RequestBidAsync(GameState gameState, int roundNumber, TimeSpan maxWait)
    {
        var req = new BidRequest { Round = roundNumber, MinBid = 0, MaxBid = roundNumber };
        var res = await _link.SendRequestAsync<BidRequest, BidResponse>("RequestBid", _playerName, req, maxWait);
        return res.Bid;
    }

    public async Task<Card> RequestCardPlayAsync(GameState state, List<Card> hand, TimeSpan maxWait)
    {
        var allowedLabels = hand.Select(CardLabeler.ToLabel).Select(s => new CardLabel { Label = s }).ToList();
        bool requireTigress = hand.Any(c => c is TigressCard);

        var req = new CardPlayRequest { PlayerName = _playerName, Allowed = allowedLabels, RequireTigressMode = requireTigress };
        var res = await _link.SendRequestAsync<CardPlayRequest, CardPlayResponse>("RequestCardPlay", _playerName, req, maxWait);

        if (res.ChoiceIndex < 0 || res.ChoiceIndex >= hand.Count)
            throw new InvalidOperationException("Client returned out-of-range card index.");

        var chosen = hand[res.ChoiceIndex];

        if (chosen is TigressCard tigress)
        {
            if (string.IsNullOrWhiteSpace(res.TigressMode))
                throw new InvalidOperationException("Tigress selected but no mode provided by client.");

            tigress.PlayedAsType = res.TigressMode.Equals("PIRATE", StringComparison.OrdinalIgnoreCase)
                ? SkullKingCore.GameDefinitions.CardType.PIRATE
                : SkullKingCore.GameDefinitions.CardType.ESCAPE;
        }

        return chosen;
    }
}
