using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Game;
using SkullKingCore.Core.Game.Interfaces;
using SkullKingCore.Logging;
using SkullKingCore.Network.FileRpc.Rpc;

namespace SkullKingCore.Network.FileRpc.Server
{
    public sealed class NetworkFileHostedGameController : IGameController, IAsyncDisposable
    {
        private readonly string _folder;
        private readonly CancellationTokenSource _cts = new();
        private FileRpcConnection? _conn;

        public string Name { get; }

        public GameState? GameState { get; set; }

        /// <param name="folder">Shared folder path containing {clientId}.toServer / {clientId}.fromServer.</param>
        public NetworkFileHostedGameController(string folder, string name = "NetPlayer")
        {
            Name = name;
            _folder = folder ?? throw new ArgumentNullException(nameof(folder));
            Directory.CreateDirectory(_folder);
            _ = AcceptOnceAsync();
            Logger.Instance.WriteToConsoleAndLog($"[Server] Waiting for file-based network player in '{_folder}' ...");
        }

        private async Task AcceptOnceAsync()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    foreach (var file in Directory.EnumerateFiles(_folder, "*.toServer", SearchOption.AllDirectories))
                    {
                        var clientId = Path.GetFileName(Path.GetDirectoryName(file))!;
                        _conn = await FileRpcConnection.FromAcceptedAsync(_folder, clientId, _cts.Token).ConfigureAwait(false);
                        Logger.Instance.WriteToConsoleAndLog($"[Server] File client '{clientId}' connected.");
                        return;
                    }
                    await Task.Delay(100, _cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
        }


        private async Task EnsureConnectedAsync()
        {
            var start = DateTime.UtcNow;
            while (_conn is null)
            {
                if (_cts.IsCancellationRequested) throw new OperationCanceledException();
                if ((DateTime.UtcNow - start).TotalMinutes > 2)
                    throw new TimeoutException("Client did not connect.");
                await Task.Delay(50, _cts.Token).ConfigureAwait(false);
            }
        }

        private async Task<T?> CallAsync<T>(string method, params object?[] args)
        {
            await EnsureConnectedAsync().ConfigureAwait(false);
            return await _conn!.InvokeAsync<T>(method, args).ConfigureAwait(false);
        }

        // ---------------- IGameController proxy methods (same as your TCP version) ----------------

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

        public Task AnnounceBidAsync(GameState gameState, TimeSpan maxWait)
            => CallAsync<object?>(nameof(AnnounceBidAsync), gameState, maxWait)!;

        public Task NotifyNotAllCardsInHandCanBePlayed(GameState gameState, List<Card> allowed, List<Card> notAllowed)
            => CallAsync<object?>(nameof(NotifyNotAllCardsInHandCanBePlayed), gameState, allowed, notAllowed)!;

        public async Task<Card> RequestCardPlayAsync(GameState gameState, List<Card> hand, TimeSpan maxWait)
            => await CallAsync<Card?>(nameof(RequestCardPlayAsync), gameState, hand, maxWait)
                   .ConfigureAwait(false)
               ?? throw new InvalidOperationException("RequestCardPlayAsync returned null.");

        public Task NotifyCardPlayedAsync(GameState gameState, Player player, Card playedCard)
            => CallAsync<object?>(nameof(NotifyCardPlayedAsync), gameState, player, playedCard)!;

        public Task NotifyAboutSubRoundWinnerAsync(GameState gameState, Player? player, Card? winningCard)
            => CallAsync<object?>(nameof(NotifyAboutSubRoundWinnerAsync), gameState, player, winningCard)!;

        public Task NotifyGameStartedAsync(GameState gameState)
            => CallAsync<object?>(nameof(NotifyGameStartedAsync), gameState)!;

        public Task NotifyAboutSubRoundStartAsync(GameState gameState)
            => CallAsync<object?>(nameof(NotifyAboutSubRoundStartAsync), gameState)!;

        public Task NotifyAboutSubRoundEndAsync(GameState gameState)
            => CallAsync<object?>(nameof(NotifyAboutSubRoundEndAsync), gameState)!;

        public Task NotifyAboutMainRoundEndAsync(GameState gameState)
            => CallAsync<object?>(nameof(NotifyAboutMainRoundEndAsync), gameState)!;

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
            if (_conn is not null) await _conn.DisposeAsync();
        }
    }
}
