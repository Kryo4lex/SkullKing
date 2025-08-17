namespace SkullKingCore.Network.Client;

/// <summary>Registers handlers on INetworkLink and delegates to ConsoleClientAgent.</summary>
public sealed class ControllerNetworkAdapter
{
    private readonly INetworkLink _link;
    private readonly ConsoleClientAgent _agent;

    public ControllerNetworkAdapter(INetworkLink link, ConsoleClientAgent agent)
    {
        _link = link;
        _agent = agent;
    }

    public void Register()
    {
        // Requests
        _link.OnRequest<BidRequest, BidResponse>("RequestBid",
            async (playerName, req, ct) =>
            {
                int bid = await _agent.ChooseBidAsync(req.Round, req.MinBid, req.MaxBid, ct);
                return new BidResponse(bid);
            });

        _link.OnRequest<CardPlayRequest, CardPlayResponse>("RequestCardPlay",
            async (playerName, req, ct) =>
            {
                var allowed = req.Allowed.Select(a => a.Label).ToList();
                var (idx, mode) = await _agent.ChooseCardAsync(allowed, req.RequireTigressMode, ct);
                return new CardPlayResponse { ChoiceIndex = idx, TigressMode = mode };
            });

        // Events (typed, must match server payloads)
        _link.OnEvent<GameStartedEvt>("GameStarted",
            async (pn, e, ct) => await _agent.Show($"Game started (round {e.Round}) with players: {string.Join(", ", e.Players.Select(x => x.Name))}"));

        _link.OnEvent<RoundStartedEvt>("RoundStarted",
            async (pn, e, ct) => await _agent.Show($"Round {e.Round} started."));

        _link.OnEvent<MessageEvt>("BidCollectionStarted",
            async (pn, e, ct) => await _agent.Show("Bid collection started."));

        _link.OnEvent<BidAnnouncedEvt>("BidAnnounced",
            async (pn, e, ct) => await _agent.Show($"{e.By} bids {e.Bid}"));

        _link.OnEvent<NotAllPlayableEvt>("NotAllPlayable",
            async (pn, e, ct) => await _agent.Show("Some cards are blocked due to lead suit."));

        _link.OnEvent<CardPlayedEvt>("CardPlayed",
            async (pn, e, ct) => await _agent.Show($"{e.By} played {e.Card}"));

        _link.OnEvent<SubRoundPhaseEvt>("SubRoundStarted",
            async (pn, e, ct) => await _agent.Show($"Sub-round {e.SubRound} started."));

        _link.OnEvent<SubRoundPhaseEvt>("SubRoundEnded",
            async (pn, e, ct) => await _agent.Show($"Sub-round {e.SubRound} ended."));

        _link.OnEvent<SubRoundWinnerEvt>("SubRoundWinner",
            async (pn, e, ct) => await _agent.Show($"Sub-round winner: {(e.Winner ?? "None")}{(e.WinningCard != null ? $" with {e.WinningCard}" : "")}"));

        _link.OnEvent<GameEndedEvt>("GameEnded",
            async (pn, e, ct) => await _agent.Show("Game ended."));

        _link.OnEvent<PlayerTimedOutEvt>("PlayerTimedOut",
            async (pn, e, ct) => await _agent.Show($"Timeout: {e.PlayerName} during {e.Phase}."));

        _link.OnEvent<MessageEvt>("Message",
            async (pn, e, ct) => await _agent.Show(e.Message));
    }
}
