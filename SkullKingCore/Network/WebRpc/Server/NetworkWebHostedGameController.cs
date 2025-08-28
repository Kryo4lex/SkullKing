using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Game;
using SkullKingCore.Core.Game.Interfaces;
using SkullKingCore.Logging;
using SkullKingCore.Network.WebRpc.Rpc;

namespace SkullKingCore.Network.WebRpc.Server
{
    /// <summary>
    /// Per-player proxy over HTTP long-poll, backed by an internal shared host (one Kestrel per port).
    /// Same usage shape as TCP: new NetworkHttpHostedGameController(port, player.Id).
    /// </summary>
    public sealed class NetworkWebHostedGameController : IGameController, IAsyncDisposable
    {
        // -------- public knobs (set before first use) --------
        /// <summary>Show Kestrel info logs (applies only if this instance starts the host).</summary>
        public bool EnableHostInfoLogs { get; set; } = false;

        /// <summary>If null (default), wait indefinitely for first client contact; else throw after duration.</summary>
        public TimeSpan? ConnectTimeout { get; set; } = null;

        /// <summary>If true, consider client disconnected when no HTTP traffic for <see cref="ClientIdleDisconnect"/>.</summary>
        public bool TreatNoTrafficAsDisconnect { get; set; } = false;

        /// <summary>Only used with TreatNoTrafficAsDisconnect=true. Default 2 min.</summary>
        public TimeSpan ClientIdleDisconnect { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>Log every RPC call and reply.</summary>
        public bool LogAllCalls { get; set; } = false;

        // -------- state --------
        private readonly CancellationTokenSource _cts = new();
        private readonly int _port;
        private string? _clientId;
        private bool _acceptStarted;
        private Internal.WebRpcHost? _host; // set on first use

        public string Name { get; }

        public NetworkWebHostedGameController(int port, string name = "NetPlayer")
        {
            _port = port;
            Name = name;
            // Register our expected clientId immediately (host may start later)
            Internal.WebRpcHostRegistry.Register(_port, Name);
        }

        public NetworkWebHostedGameController(string baseUrl, string name = "NetPlayer")
            : this(ParsePortOrThrow(baseUrl), name) { }

        public NetworkWebHostedGameController(IPAddress address, int port, string name = "NetPlayer")
            : this(port, name) { }

        private static int ParsePortOrThrow(string url)
        {
            if (!Uri.TryCreate(url.EndsWith("/", StringComparison.Ordinal) ? url : url + "/", UriKind.Absolute, out var uri))
                throw new ArgumentException($"Invalid URL: {url}", nameof(url));
            return uri.Port;
        }

        private void EnsureHostingStarted()
        {
            if (_host is not null) return;
            // First user to touch this port decides host logging
            _host = Internal.WebRpcHostRegistry.GetOrStart(_port, enableInfoLogs: EnableHostInfoLogs);
        }

        private async Task AcceptOnceAsync()
        {
            try
            {
                EnsureHostingStarted();
                _clientId = await _host!.WaitForFirstContactAsync(Name, ConnectTimeout, _cts.Token).ConfigureAwait(false);
                Logger.Instance.WriteToConsoleAndLog($"[Server] Client '{_clientId}' connected (HTTP).");
            }
            catch (OperationCanceledException) { }
        }

        private async Task EnsureConnectedAsync()
        {
            EnsureHostingStarted();

            if (!_acceptStarted)
            {
                _acceptStarted = true;
                _ = AcceptOnceAsync();
            }

            while (_clientId is null)
            {
                if (_cts.IsCancellationRequested) throw new OperationCanceledException();
                await Task.Delay(50, _cts.Token).ConfigureAwait(false);
            }
        }

        // -------- server -> client call bridge --------
        private async Task<T?> CallAsync<T>(string method, params object?[] args)
        {
            await EnsureConnectedAsync().ConfigureAwait(false);

            var payload = new object?[] { method, args };
            var bytes = WireWebRpc.Serialize(payload);

            var replyTask = _host!.InvokeAsync(Name, bytes, method, LogAllCalls, _cts.Token);

            // Optional continuous liveness check
            while (true)
            {
                var completed = await Task.WhenAny(replyTask, Task.Delay(1000, _cts.Token)).ConfigureAwait(false);
                if (completed == replyTask)
                {
                    var replyBytes = await replyTask.ConfigureAwait(false);
                    return WireWebRpc.Deserialize<T>(replyBytes);
                }

                if (TreatNoTrafficAsDisconnect)
                {
                    var last = _host!.GetLastSeenUtc(Name);
                    if (last.HasValue && DateTime.UtcNow - last.Value > ClientIdleDisconnect)
                        throw new IOException($"Client '{Name}' disconnected (no HTTP traffic for {ClientIdleDisconnect}).");
                }
            }
        }

        // -------- IGameController proxy --------
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
            => await CallAsync<Card?>(nameof(RequestCardPlayAsync), gameState, hand, maxWait).ConfigureAwait(false)
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
            await Task.CompletedTask;
        }

        // ============================
        // Internal per-port host layer
        // ============================
        private static class Internal
        {
            internal static class WebRpcHostRegistry
            {
                private static readonly ConcurrentDictionary<int, WebRpcHost> _hosts = new();
                private static readonly ConcurrentDictionary<int, ConcurrentDictionary<string, byte>> _preRegistered =
                    new(); // ids registered before host exists

                public static void Register(int port, string clientId)
                {
                    if (_hosts.TryGetValue(port, out var host))
                        host.Register(clientId);
                    else
                        _preRegistered.GetOrAdd(port, _ => new(StringComparer.Ordinal))[clientId] = 1;
                }

                public static WebRpcHost GetOrStart(int port, bool enableInfoLogs)
                {
                    return _hosts.GetOrAdd(port, p =>
                    {
                        var host = new WebRpcHost(p, enableInfoLogs);
                        if (_preRegistered.TryGetValue(port, out var ids))
                        {
                            foreach (var id in ids.Keys) host.Register(id);
                        }
                        return host;
                    });
                }
            }

            internal sealed class WebRpcHost
            {
                // hosting
                private readonly WebApplication _app;
                private readonly Task _runTask;
                private readonly CancellationTokenSource _cts = new();

                // allow-list of clientIds
                private readonly ConcurrentDictionary<string, byte> _allowed = new(StringComparer.Ordinal);

                // first contact gate per client
                private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _firstContact =
                    new(StringComparer.Ordinal);

                // per-client queues & liveness
                private sealed class CallEntry { public string Id = ""; public byte[] Payload = Array.Empty<byte>(); }
                private sealed class ClientQueues
                {
                    public readonly ConcurrentQueue<CallEntry> PendingCalls = new();
                    public readonly ConcurrentQueue<TaskCompletionSource<CallEntry>> WaitingLongPolls = new();
                }

                private readonly ConcurrentDictionary<string, ClientQueues> _clients = new(StringComparer.Ordinal);
                private readonly ConcurrentDictionary<string, DateTime> _lastSeenUtc = new(StringComparer.Ordinal);

                // waiting replies (callId -> waiter)
                private sealed class WaitingReply
                {
                    public string ClientId = "";
                    public TaskCompletionSource<byte[]> Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
                }
                private readonly ConcurrentDictionary<string, WaitingReply> _waitingReplies = new(StringComparer.Ordinal);

                public WebRpcHost(int port, bool enableInfoLogs)
                {
                    var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = Array.Empty<string>() });

                    if (enableInfoLogs)
                        builder.Logging.SetMinimumLevel(LogLevel.Information);
                    else
                    {
                        builder.Logging.ClearProviders();
                        builder.Logging.SetMinimumLevel(LogLevel.Warning);
                    }

                    builder.WebHost.ConfigureKestrel(k => k.ListenAnyIP(port));

                    _app = builder.Build();
                    MapEndpoints(_app);

                    _runTask = _app.RunAsync(_cts.Token);
                    Logger.Instance.WriteToConsoleAndLog($"[WebRpc] Listening on http://0.0.0.0:{port}/ (and IPv6 where available)");
                }

                public void Register(string clientId)
                {
                    _allowed[clientId] = 1;
                    _firstContact.GetOrAdd(clientId, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));
                }

                public async Task<string> WaitForFirstContactAsync(string clientId, TimeSpan? timeout, CancellationToken ct)
                {
                    if (!_allowed.ContainsKey(clientId))
                        throw new InvalidOperationException($"ClientId '{clientId}' not registered.");

                    var tcs = _firstContact.GetOrAdd(clientId, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));
                    if (timeout is null)
                    {
                        using var l = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        return await tcs.Task.WaitAsync(Timeout.InfiniteTimeSpan, l.Token).ConfigureAwait(false);
                    }
                    else
                    {
                        using var l = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        return await tcs.Task.WaitAsync(timeout.Value, l.Token).ConfigureAwait(false);
                    }
                }

                public async Task<byte[]> InvokeAsync(string clientId, byte[] payload, string methodForLog, bool logCall, CancellationToken ct)
                {
                    if (!_allowed.ContainsKey(clientId))
                        throw new InvalidOperationException($"ClientId '{clientId}' not registered.");

                    var entry = new CallEntry { Id = Guid.NewGuid().ToString("N"), Payload = payload };
                    var wr = new WaitingReply { ClientId = clientId };
                    _waitingReplies[entry.Id] = wr;

                    var q = _clients.GetOrAdd(clientId, _ => new ClientQueues());

                    if (q.WaitingLongPolls.TryDequeue(out var waiter))
                        waiter.TrySetResult(entry);
                    else
                        q.PendingCalls.Enqueue(entry);

                    if (logCall)
                        Logger.Instance.WriteToConsoleAndLog($"[WebRpc] → {clientId}: {methodForLog}");

                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    using var _reg = linked.Token.Register(() =>
                    {
                        if (_waitingReplies.TryRemove(entry.Id, out var w))
                            w.Tcs.TrySetCanceled(linked.Token);
                    });

                    var bytes = await wr.Tcs.Task.ConfigureAwait(false);

                    if (logCall)
                        Logger.Instance.WriteToConsoleAndLog($"[WebRpc] ← {clientId}: {methodForLog} (reply)");

                    return bytes;
                }

                public DateTime? GetLastSeenUtc(string clientId)
                    => _lastSeenUtc.TryGetValue(clientId, out var dt) ? dt : null;

                // ---- endpoints ----
                private void MapEndpoints(WebApplication app)
                {
                    app.MapGet("/rpc/ping", async ctx =>
                    {
                        var clientId = ctx.Request.Query["clientId"].ToString();
                        if (!IsAllowed(clientId)) { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; await ctx.Response.WriteAsync("forbidden"); return; }

                        Touch(clientId);
                        EnsureQueues(clientId);
                        CompleteFirstContact(clientId);

                        ctx.Response.ContentType = "text/plain";
                        await ctx.Response.WriteAsync("ok");
                    });

                    app.MapGet("/rpc/next", async ctx =>
                    {
                        var clientId = ctx.Request.Query["clientId"].ToString();
                        if (!IsAllowed(clientId)) { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return; }

                        Touch(clientId);
                        var q = EnsureQueues(clientId);
                        CompleteFirstContact(clientId);

                        if (q.PendingCalls.TryDequeue(out var call))
                        {
                            await WriteJson(ctx.Response, new { callId = call.Id, payloadB64 = Convert.ToBase64String(call.Payload) });
                            return;
                        }

                        var waiter = new TaskCompletionSource<CallEntry>(TaskCreationOptions.RunContinuationsAsynchronously);
                        q.WaitingLongPolls.Enqueue(waiter);

                        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(55));
                        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, timeoutCts.Token);
                        using var _ = linked.Token.Register(() => waiter.TrySetCanceled(linked.Token));

                        try
                        {
                            var callWhenAvailable = await waiter.Task.ConfigureAwait(false);
                            await WriteJson(ctx.Response, new { callId = callWhenAvailable.Id, payloadB64 = Convert.ToBase64String(callWhenAvailable.Payload) });
                        }
                        catch (OperationCanceledException)
                        {
                            ctx.Response.StatusCode = StatusCodes.Status204NoContent;
                        }
                    });

                    app.MapPost("/rpc/reply/{callId}", async (HttpContext ctx, string callId) =>
                    {
                        var clientId = ctx.Request.Query["clientId"].ToString();
                        if (!IsAllowed(clientId)) { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return; }

                        Touch(clientId);

                        using var ms = new MemoryStream();
                        await ctx.Request.Body.CopyToAsync(ms);
                        var bytes = ms.ToArray();

                        if (_waitingReplies.TryRemove(callId, out var wr))
                            wr.Tcs.TrySetResult(bytes);

                        ctx.Response.StatusCode = StatusCodes.Status200OK;
                    });
                }

                private bool IsAllowed(string? id) => !string.IsNullOrWhiteSpace(id) && _allowed.ContainsKey(id);
                private void CompleteFirstContact(string id)
                {
                    if (_firstContact.TryGetValue(id, out var tcs))
                    {
                        // Only the first successful set returns true; this makes logging single-shot.
                        if (tcs.TrySetResult(id))
                            Logger.Instance.WriteToConsoleAndLog($"[WebRpc] Client '{id}' connected.");
                    }
                }
                private ClientQueues EnsureQueues(string id) => _clients.GetOrAdd(id, _ => new ClientQueues());
                private void Touch(string id) => _lastSeenUtc[id] = DateTime.UtcNow;

                private static async Task WriteJson(HttpResponse resp, object dto)
                {
                    resp.ContentType = "application/json";
                    var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    var bytes = Encoding.UTF8.GetBytes(json);
                    resp.ContentLength = bytes.Length;
                    await resp.Body.WriteAsync(bytes);
                }
            }
        }
    }
}
