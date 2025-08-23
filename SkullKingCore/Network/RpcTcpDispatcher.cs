using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Game;
using SkullKingCore.Core.Game.Interfaces;

namespace SkullKingCore.Network
{
    public sealed class RpcTcpDispatcher<TController>
        where TController : IGameController
    {
        private readonly TController _impl;

        public RpcTcpDispatcher(TController impl) => _impl = impl;

        public Task<object?> DispatchAsync(string method, object?[] a) =>
            Normalize(method) switch
            {
                nameof(IGameController.RequestName)
                    => Invoke(() => _impl.RequestName(Arg<GameState>(a, 0), Arg<TimeSpan>(a, 1))),

                nameof(IGameController.NotifyGameStartedAsync)
                    => Invoke(() => _impl.NotifyGameStartedAsync(Arg<GameState>(a, 0))),

                nameof(IGameController.NotifyRoundStartedAsync)
                    => Invoke(() => _impl.NotifyRoundStartedAsync(Arg<GameState>(a, 0))),

                nameof(IGameController.NotifyBidCollectionStartedAsync)
                    => Invoke(() => _impl.NotifyBidCollectionStartedAsync(Arg<GameState>(a, 0))),

                nameof(IGameController.RequestBidAsync)
                    => Invoke(() => _impl.RequestBidAsync(Arg<GameState>(a, 0), Arg<int>(a, 1), Arg<TimeSpan>(a, 2))),

                nameof(IGameController.AnnounceBidAsync)
                    => Invoke(() => _impl.AnnounceBidAsync(Arg<GameState>(a, 0), Arg<Player>(a, 1), Arg<int>(a, 2), Arg<TimeSpan>(a, 3))),

                nameof(IGameController.WaitForBidsReceivedAsync)
                    => Invoke(() => _impl.WaitForBidsReceivedAsync(Arg<GameState>(a, 0))),

                nameof(IGameController.NotifyNotAllCardsInHandCanBePlayed)
                    => Invoke(() => _impl.NotifyNotAllCardsInHandCanBePlayed(Arg<GameState>(a, 0), Arg<List<Card>>(a, 1), Arg<List<Card>>(a, 2))),

                nameof(IGameController.RequestCardPlayAsync)
                    => Invoke(() => _impl.RequestCardPlayAsync(Arg<GameState>(a, 0), Arg<List<Card>>(a, 1), Arg<TimeSpan>(a, 2))),

                nameof(IGameController.NotifyCardPlayedAsync)
                    => Invoke(() => _impl.NotifyCardPlayedAsync(Arg<Player>(a, 0), Arg<Card>(a, 1))),

                nameof(IGameController.NotifyAboutSubRoundStartAsync)
                    => Invoke(() => _impl.NotifyAboutSubRoundStartAsync(Arg<GameState>(a, 0))),

                nameof(IGameController.NotifyAboutSubRoundEndAsync)
                    => Invoke(() => _impl.NotifyAboutSubRoundEndAsync(Arg<GameState>(a, 0))),

                nameof(IGameController.NotifyAboutSubRoundWinnerAsync)
                    => Invoke(() => _impl.NotifyAboutSubRoundWinnerAsync(Arg<Player?>(a, 0), Arg<Card?>(a, 1), Arg<int>(a, 2))),

                nameof(IGameController.NotifyGameEndedAsync)
                    => Invoke(() => _impl.NotifyGameEndedAsync(Arg<GameState>(a, 0))),

                nameof(IGameController.NotifyAboutGameWinnerAsync)
                    => Invoke(() => _impl.NotifyAboutGameWinnerAsync(Arg<GameState>(a, 0), Arg<List<Player>>(a, 1))),

                nameof(IGameController.ShowMessageAsync)
                    => Invoke(() => _impl.ShowMessageAsync(Arg<string>(a, 0))),

                nameof(IGameController.NotifyPlayerTimedOutAsync)
                    => Invoke(() => _impl.NotifyPlayerTimedOutAsync(Arg<GameState>(a, 0), Arg<Player>(a, 1))),

                _ => throw new InvalidOperationException($"Unknown method: {method}")
            };

        // helpers (same as you have)
        private static T Arg<T>(object?[] a, int i) =>
            a.Length > i && a[i] is T t ? t : throw new ArgumentException($"Argument {i} must be {typeof(T).Name}.");

        private static Task<object?> Invoke(Func<Task> f) =>
            f().ContinueWith<object?>(_ => null);

        private static async Task<object?> Invoke<T>(Func<Task<T>> f) =>
            (object?)await f();

        private static string Normalize(string m)
        {
            m = m.Trim();
            var i = m.LastIndexOf('.');
            if (i >= 0 && i < m.Length - 1) m = m[(i + 1)..];
            if (m.EndsWith("()", StringComparison.Ordinal)) m = m[..^2];
            return m;
        }
    }
}
