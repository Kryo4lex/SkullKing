using SkullKing.Network.Rpc;
using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Game;
using SkullKingCore.Core.Game.Interfaces;
using System.Net;
using System.Net.Sockets;

namespace SkullKing.Network.Server
{
    public sealed class NetworkHostedGameController : IGameController, IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private RpcConnection? _conn;

        public string Name { get; }

        public NetworkHostedGameController(int port, string name = "NetPlayer")
        {
            Name = name;
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _ = AcceptOnceAsync();
            Console.WriteLine($"[Server] Waiting for network player on port {port} ...");
        }

        private async Task AcceptOnceAsync()
        {
            try
            {
                var tcp = await _listener.AcceptTcpClientAsync(_cts.Token).ConfigureAwait(false);
                _conn = await RpcConnection.FromAcceptedAsync(tcp, _cts.Token).ConfigureAwait(false);
                Console.WriteLine($"[Server] Network player connected.");
            }
            catch (OperationCanceledException) { }
        }

        private async Task EnsureConnectedAsync()
        {
            var start = DateTime.UtcNow;
            while (_conn is null)
            {
                if (_cts.IsCancellationRequested) throw new OperationCanceledException();
                if ((DateTime.UtcNow - start).TotalMinutes > 2) throw new TimeoutException("Client did not connect.");
                await Task.Delay(50, _cts.Token).ConfigureAwait(false);
            }
        }

        private async Task<T?> CallAsync<T>(string method, params object?[] args)
        {
            await EnsureConnectedAsync().ConfigureAwait(false);
            return await _conn!.InvokeAsync<T>(method, args).ConfigureAwait(false);
        }

        // IGameController proxy methods

        public async Task<string> RequestName(GameState gameState, TimeSpan maxWait)
            => await CallAsync<string?>(nameof(RequestName), gameState, maxWait).ConfigureAwait(false)
               ?? throw new InvalidOperationException("RequestName returned null.");

        public Task NotifyRoundStartedAsync(GameState gameState)
            => CallAsync<object?>(nameof(NotifyRoundStartedAsync), gameState)!;

        public Task NotifyBidCollectionStartedAsync(GameState gameState)
            => CallAsync<object?>(nameof(NotifyBidCollectionStartedAsync), gameState)!;

        public Task WaitForBidsReceivedAsync(GameState gameState)
            => CallAsync<object?>(nameof(WaitForBidsReceivedAsync), gameState)!;

        public async Task<int> RequestBidAsync(GameState gameState, int roundNumer, TimeSpan maxWait)
            => await CallAsync<int>(nameof(RequestBidAsync), gameState, roundNumer, maxWait)!.ConfigureAwait(false);

        public Task AnnounceBidAsync(GameState gameState, Player player, int bid, TimeSpan maxWait)
            => CallAsync<object?>(nameof(AnnounceBidAsync), gameState, player, bid, maxWait)!;

        public Task NotifyNotAllCardsInHandCanBePlayed(GameState gameState, List<Card> allowed, List<Card> notAllowed)
            => CallAsync<object?>(nameof(NotifyNotAllCardsInHandCanBePlayed), gameState, allowed, notAllowed)!;

        public async Task<Card> RequestCardPlayAsync(GameState gameState, List<Card> hand, TimeSpan maxWait)
            => await CallAsync<Card?>(nameof(RequestCardPlayAsync), gameState, hand, maxWait)
                   .ConfigureAwait(false)
               ?? throw new InvalidOperationException("RequestCardPlayAsync returned null.");

        public Task NotifyCardPlayedAsync(Player player, Card playedCard)
            => CallAsync<object?>(nameof(NotifyCardPlayedAsync), player, playedCard)!;

        public Task NotifyAboutSubRoundWinnerAsync(Player? player, Card? winningCard, int round)
            => CallAsync<object?>(nameof(NotifyAboutSubRoundWinnerAsync), player, winningCard, round)!;

        public Task NotifyGameStartedAsync(GameState gameState)
            => CallAsync<object?>(nameof(NotifyGameStartedAsync), gameState)!;

        public Task NotifyAboutSubRoundStartAsync(GameState gameState)
            => CallAsync<object?>(nameof(NotifyAboutSubRoundStartAsync), gameState)!;

        public Task NotifyAboutSubRoundEndAsync(GameState gameState)
            => CallAsync<object?>(nameof(NotifyAboutSubRoundEndAsync), gameState)!;

        public Task NotifyGameEndedAsync(GameState gameState)
            => CallAsync<object?>(nameof(NotifyGameEndedAsync), gameState)!;

        public Task NotifyAboutGameWinnerAsync(GameState gameState, List<Player> winners)
            => CallAsync<object?>(nameof(NotifyAboutGameWinnerAsync), gameState, winners)!;

        public Task ShowMessageAsync(string message)
            => CallAsync<object?>(nameof(ShowMessageAsync), message)!;

        public Task NotifyPlayerTimedOutAsync(GameState gameState, Player player)
            => CallAsync<object?>(nameof(NotifyPlayerTimedOutAsync), gameState, player)!;

        public async ValueTask DisposeAsync()
        {
            try { _cts.Cancel(); } catch { }
            try { _listener.Stop(); } catch { }
            if (_conn is not null) await _conn.DisposeAsync();
        }
    }
}
