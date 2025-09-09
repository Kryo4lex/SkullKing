using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Game;
using SkullKingCore.Core.Game.Interfaces;
using System.Text.Json;

namespace SkullKingCore.Network.FileRpc
{
    /// <summary>
    /// FileRpc analogue of TCP RpcTcpDispatcher. Takes a concrete IGameController implementation
    /// and dispatches incoming RPC method calls to it.
    /// 
    /// Usage on the client:
    ///   var controller = new ConsoleHumanController();
    ///   var dispatcher = new RpcFileDispatcher<ConsoleHumanController>(controller);
    ///   await conn.RunClientLoopAsync(dispatcher.DispatchAsync);
    /// </summary>
    public sealed class RpcFileDispatcher<TController>
        where TController : IGameController
    {
        private readonly TController _impl;

        public RpcFileDispatcher(TController impl) => _impl = impl;

        public event EventHandler? GameStateUpdated;

        private void OnGameStateUpdated() => GameStateUpdated?.Invoke(this, EventArgs.Empty);

        public Task<object?> DispatchAsync(string method, object?[] a)
        {

            foreach (object? obj in a)
            {
                if (obj is GameState gameState)
                {
                    _impl.GameState = gameState;
                    break;
                }
            }

            // Notify listeners that a dispatch with (a) GameState happened.
            OnGameStateUpdated();

            return Normalize(method) switch
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
                    => Invoke(() => _impl.AnnounceBidAsync(Arg<GameState>(a, 0), Arg<TimeSpan>(a, 1))),

                nameof(IGameController.WaitForBidsReceivedAsync)
                    => Invoke(() => _impl.WaitForBidsReceivedAsync(Arg<GameState>(a, 0))),

                nameof(IGameController.NotifyNotAllCardsInHandCanBePlayed)
                    => Invoke(() => _impl.NotifyNotAllCardsInHandCanBePlayed(Arg<GameState>(a, 0), Arg<List<Card>>(a, 1), Arg<List<Card>>(a, 2))),

                nameof(IGameController.RequestCardPlayAsync)
                    => Invoke(() => _impl.RequestCardPlayAsync(Arg<GameState>(a, 0), Arg<List<Card>>(a, 1), Arg<TimeSpan>(a, 2))),

                nameof(IGameController.NotifyCardPlayedAsync)
                    => Invoke(() => _impl.NotifyCardPlayedAsync(Arg<GameState>(a, 0), Arg<Player>(a, 1), Arg<Card>(a, 2))),

                nameof(IGameController.NotifyAboutSubRoundStartAsync)
                    => Invoke(() => _impl.NotifyAboutSubRoundStartAsync(Arg<GameState>(a, 0))),

                nameof(IGameController.NotifyAboutSubRoundEndAsync)
                    => Invoke(() => _impl.NotifyAboutSubRoundEndAsync(Arg<GameState>(a, 0))),

                nameof(IGameController.NotifyAboutMainRoundEndAsync)
                    => Invoke(() => _impl.NotifyAboutMainRoundEndAsync(Arg<GameState>(a, 0))),

                nameof(IGameController.NotifyAboutSubRoundWinnerAsync)
                    => Invoke(() => _impl.NotifyAboutSubRoundWinnerAsync(Arg<GameState>(a, 0), Arg<Player?>(a, 1), Arg<Card?>(a, 2))),

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
        }

        private static readonly JsonSerializerOptions s_jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            // IncludeFields = true, // enable if you rely on public fields in DTOs
        };

        private static T Arg<T>(object?[] a, int i)
        {
            if (a is null) throw new ArgumentNullException(nameof(a));
            if (i < 0 || i >= a.Length) throw new ArgumentOutOfRangeException(nameof(i));

            var v = a[i];

            // If T is reference/nullable, allow null
            if (v is null)
            {
                if (default(T) is null) return default!;
                throw new ArgumentException($"Argument {i} must be {typeof(T).Name}, but was null.");
            }

            // Fast path
            if (v is T t) return t;

            // Most common: args were deserialized as JsonElement
            if (v is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.Null)
                {
                    if (default(T) is null) return default!;
                    throw new ArgumentException(
                        $"Argument {i} was JSON null but {typeof(T).Name} is not nullable.");
                }

                try
                {
                    var value = je.Deserialize<T>(s_jsonOptions);
                    if (value is not null) return value;
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(
                        $"Argument {i} could not be deserialized to {typeof(T).Name} (JsonElement {je.ValueKind}).",
                        ex);
                }
            }

            // Sometimes object graphs arrive as IDictionary
            if (v is System.Collections.IDictionary dict)
            {
                var json = JsonSerializer.Serialize(dict, s_jsonOptions);
                try
                {
                    var value = JsonSerializer.Deserialize<T>(json, s_jsonOptions);
                    if (value is not null) return value;
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(
                        $"Argument {i} could not be deserialized to {typeof(T).Name} from IDictionary.",
                        ex);
                }
            }

            throw new ArgumentException(
                $"Argument {i} must be {typeof(T).Name}, but was {v.GetType().FullName}.");
        }

        private static Task<object?> Invoke(Func<Task> f) =>
            f().ContinueWith<object?>(_ => null);

        private static async Task<object?> Invoke<T>(Func<Task<T>> f) =>
            await f();

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
