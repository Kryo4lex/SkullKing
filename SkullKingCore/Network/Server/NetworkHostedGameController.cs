// SkullKing.Network/Server/NetworkHostedGameController.cs
// Secure adapter that hosts a TCP server and forwards IGameController calls using JSON DTOs.
// No domain objects cross the wire. The client returns indexes/values; server maps back.

#nullable enable

using SkullKing.Network.Rpc.Dtos;
using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.Implementations;
using SkullKingCore.Core.Game;
using SkullKingCore.Core.Game.Interfaces;
using SkullKingCore.GameDefinitions;
using SkullKingCore.Network.Networking;
using SkullKingCore.Network.Networking.Payload;
using SkullKingCore.Network.Rpc.Dto;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace SkullKing.Network.Server
{
    public sealed class NetworkHostedGameController : IGameController, IAsyncDisposable
    {
        private readonly string _playerId;
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private Task? _acceptLoop;
        private NetworkStream? _clientStream;

        public int Port { get; }

        public string Name { get; } = String.Empty;

        public NetworkHostedGameController(int port, string playerId)
        {
            _playerId = playerId;
            Port = port;
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _acceptLoop = Task.Run(AcceptLoopAsync);
            Console.WriteLine($"[NetworkHostedGameController] Listening on 0.0.0.0:{port}");
        }

        private async Task AcceptLoopAsync()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(_cts.Token).ConfigureAwait(false);
                    // Keep only most recent client
                    _clientStream?.Dispose();
                    _clientStream = client.GetStream();
                    Console.WriteLine($"[NetworkHostedGameController] Client connected on port {Port}");
                }
            }
            catch (OperationCanceledException) { /* normal */ }
        }

        // ------------ JSON RPC core (string method + typed payload/result) ------------

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        private async Task<TResult?> CallAsync<TPayload, TResult>(string method, TPayload payload)
        {
            var stream = _clientStream ?? throw new InvalidOperationException("No client connected.");
            var env = new RpcEnvelope<TPayload> { Method = method, Payload = payload };
            var bytes = JsonSerializer.SerializeToUtf8Bytes(env, JsonOpts);
            await Framing.WriteFrameAsync(stream, bytes, _cts.Token).ConfigureAwait(false);

            var respBytes = await Framing.ReadFrameAsync(stream, _cts.Token).ConfigureAwait(false)
                           ?? throw new InvalidOperationException("Client disconnected.");
            var resp = JsonSerializer.Deserialize<RpcResponse<TResult>>(respBytes, JsonOpts)
                       ?? throw new InvalidOperationException("Invalid client response.");
            if (!string.IsNullOrEmpty(resp.Error))
                throw new InvalidOperationException(resp.Error);
            return resp.Result;
        }

        private Task CallAsync<TPayload>(string method, TPayload payload)
            => CallAsync<TPayload, object?>(method, payload)!;

        // ---------------- Mapping helpers (domain -> DTO and back where needed) ----------------

        private static GameStateDto ToDto(GameState s) => new()
        {
            CurrentRound = s.CurrentRound,
            CurrentSubRound = s.CurrentSubRound,
            MaxRounds = s.MaxRounds,
            Players = s.Players.Select(p => new PlayerDto { Id = p.Id, Name = p.Name }).ToList()
        };

        private static CardViewDto ToView(Card c, int index) => new()
        {
            CardType = c.CardType.ToString(),
            GenericValue = c.GenericValue,
            Display = c.ToString()
        };

        private static List<CardViewDto> ToViews(List<Card> cards)
        {
            var list = new List<CardViewDto>(cards.Count);
            for (int i = 0; i < cards.Count; i++) list.Add(ToView(cards[i], i));
            return list;
        }

        // If the chosen card is Tigress and the client specified a mode, set it.
        private static void ApplySpecialChoiceIfAny(Card chosen, string? tigressMode)
        {
            if (chosen is TigressCard tigress && !string.IsNullOrWhiteSpace(tigressMode))
            {
                var m = tigressMode.Trim().ToUpperInvariant();
                tigress.PlayedAsType = m.StartsWith("E") ? CardType.ESCAPE
                                  : m.StartsWith("P") ? CardType.PIRATE
                                  : tigress.PlayedAsType; // ignore invalid
            }
        }

        // ---------------- IGameController implementation (forwarded via JSON DTOs) ----------------

        public Task NotifyRoundStartedAsync(GameState gameState)
            => CallAsync(nameof(NotifyRoundStartedAsync), new NotifyRoundStartedPayload { GameState = ToDto(gameState) });

        public Task NotifyBidCollectionStartedAsync(GameState gameState)
            => CallAsync(nameof(NotifyBidCollectionStartedAsync), new NotifyBidCollectionStartedPayload { GameState = ToDto(gameState) });

        public Task WaitForBidsReceivedAsync(GameState gameState)
            => CallAsync(nameof(WaitForBidsReceivedAsync), new WaitForBidsReceivedPayload { GameState = ToDto(gameState) });

        public async Task<int> RequestBidAsync(GameState gameState, int roundNumber, TimeSpan maxWait)
        {
            var me = GetThisPlayer(gameState);

            var res = await CallAsync<RequestBidPayload, RequestBidResult>(
                nameof(RequestBidAsync),
                new RequestBidPayload
                {
                    GameState = DtoMapper.ToGameStateDtoForViewer(gameState, _playerId), // shows only my hand in Players list
                    RoundNumber = roundNumber,
                    MaxWaitMs = (int)Math.Min(int.MaxValue, maxWait.TotalMilliseconds),
                    Hand = DtoMapper.ToCardDtos(me.Hand) // <-- explicitly include my hand for the screen
                });

            if (res is null) throw new InvalidOperationException("Client returned no bid.");
            return res.Bid;
        }

        public Task AnnounceBidAsync(GameState gameState, Player player, int bid, TimeSpan maxWait)
        {
            var payload = new AnnounceBidPayload
            {
                // Always-expanded state: ALL players with full Hands + Bids
                GameState = DtoMapper.ToGameStateDto(gameState),

                // The specific announcing player (also with full Hand + Bids)
                Player = new PlayerDto
                {
                    Id = player.Id,
                    Name = player.Name,
                    Hand = DtoMapper.ToCardDtos(player.Hand),
                    Bids = DtoMapper.ToBidDtos(player.Bids)
                },

                Bid = bid,
                MaxWaitMs = maxWait == Timeout.InfiniteTimeSpan
                    ? -1
                    : (int)Math.Min(int.MaxValue, maxWait.TotalMilliseconds)
            };

            return CallAsync(nameof(IGameController.AnnounceBidAsync), payload);
        }

        public Task NotifyNotAllCardsInHandCanBePlayed(GameState gameState, List<Card> cardsThatPlayerIsAllowedToPlay, List<Card> cardsThatPlayerIsNotAllowedToPlay)
            => CallAsync(nameof(NotifyNotAllCardsInHandCanBePlayed), new NotAllCardsPlayablePayload
            {
                GameState = ToDto(gameState),
                Allowed = ToViews(cardsThatPlayerIsAllowedToPlay),
                NotAllowed = ToViews(cardsThatPlayerIsNotAllowedToPlay)
            });

        public async Task<Card> RequestCardPlayAsync(GameState gameState, List<Card> hand, TimeSpan maxWait)
        {
            // Send only a view of the cards; client returns an index (and optional Tigress mode).
            var res = await CallAsync<RequestCardPlayPayload, RequestCardPlayResult>(
                nameof(RequestCardPlayAsync),
                new RequestCardPlayPayload
                {
                    GameState = ToDto(gameState),
                    Hand = ToViews(hand),
                    MaxWaitMs = (int)Math.Min(int.MaxValue, maxWait.TotalMilliseconds)
                }).ConfigureAwait(false);

            if (res is null) throw new InvalidOperationException("Client returned no card selection.");
            if (res.Index < 0 || res.Index >= hand.Count)
                throw new InvalidOperationException($"Client returned invalid card index {res.Index}.");

            var chosen = hand[res.Index];
            ApplySpecialChoiceIfAny(chosen, res.TigressMode);
            return chosen;
        }

        public Task NotifyCardPlayedAsync(Player player, Card playedCard)
            => CallAsync(nameof(NotifyCardPlayedAsync), new NotifyCardPlayedPayload
            {
                Player = new PlayerDto { Id = player.Id, Name = player.Name },
                Card = ToView(playedCard, -1)
            });

        public Task NotifyAboutSubRoundWinnerAsync(Player? player, Card? winningCard, int round)
            => CallAsync(nameof(NotifyAboutSubRoundWinnerAsync), new SubRoundWinnerPayload
            {
                Player = player is null ? null : new PlayerDto { Id = player.Id, Name = player.Name },
                WinningCard = winningCard is null ? null : ToView(winningCard, -1),
                Round = round
            });

        public Task NotifyGameStartedAsync(GameState gameState)
            => CallAsync(nameof(NotifyGameStartedAsync), new NotifyGameStartedPayload { GameState = ToDto(gameState) });

        public Task NotifyAboutSubRoundStartAsync(GameState gameState)
            => CallAsync(nameof(NotifyAboutSubRoundStartAsync), new NotifySubRoundStartPayload { GameState = ToDto(gameState) });

        public Task NotifyAboutSubRoundEndAsync(GameState gameState)
            => CallAsync(nameof(NotifyAboutSubRoundEndAsync), new NotifySubRoundEndPayload { GameState = ToDto(gameState) });

        public Task NotifyGameEndedAsync(GameState gameState)
            => CallAsync(nameof(NotifyGameEndedAsync), new NotifyGameEndedPayload { GameState = ToDto(gameState) });

        public Task NotifyAboutGameWinnerAsync(GameState gameState, List<Player> winners)
            => CallAsync(nameof(NotifyAboutGameWinnerAsync), new GameWinnersPayload
            {
                GameState = ToDto(gameState),
                Winners = winners.Select(w => new PlayerDto { Id = w.Id, Name = w.Name }).ToList()
            });

        public Task ShowMessageAsync(string message)
            => CallAsync(nameof(ShowMessageAsync), new ShowMessagePayload { Message = message });

        public Task NotifyPlayerTimedOutAsync(GameState gameState, Player player)
            => CallAsync(nameof(NotifyPlayerTimedOutAsync), new PlayerTimedOutPayload
            {
                GameState = ToDto(gameState),
                Player = new PlayerDto { Id = player.Id, Name = player.Name }
            });

        // -----------------------------------------------------------------------

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            try { _listener.Stop(); } catch { }
            if (_acceptLoop is not null) { try { await _acceptLoop.ConfigureAwait(false); } catch { } }
            try { _clientStream?.Dispose(); } catch { }
            _cts.Dispose();
        }

        // helper to get this controller’s player from the state
        private Player GetThisPlayer(GameState s)
            => s.Players.First(p => p.Id == _playerId);
    }

    // -------------------- Transport envelopes (JSON) --------------------

    internal sealed class RpcEnvelope<TPayload>
    {
        public string Method { get; set; } = "";
        public TPayload? Payload { get; set; }
    }

    internal sealed class RpcResponse<TResult>
    {
        public string? Error { get; set; }
        public TResult? Result { get; set; }
    }

}
