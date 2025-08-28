using SkullKingCore.Network.FileRpc.Transport;
using System.Collections.Concurrent;

namespace SkullKingCore.Network.FileRpc.Rpc
{
    /// <summary>
    /// File-based analogue of TCP RpcConnection.
    /// - CLIENT: ConnectAsync(clientId, folder, ct)  -> use RunClientLoopAsync(dispatcher)
    /// - SERVER: FromAcceptedAsync(folder, clientId, ct) -> use InvokeAsync<T>(method, args)
    ///
    /// Files in <folder>:
    ///   {clientId}.toServer   : client  -> server (client writes, server reads)
    ///   {clientId}.fromServer : server -> client (server writes, client reads)
    /// </summary>
    public sealed class FileRpcConnection : IAsyncDisposable
    {
        private enum Role { Client, Server }

        private readonly string _clientId;
        private readonly Role _role;
        private readonly AppendOnlyFileChannel _incoming; // the file we READ from
        private readonly AppendOnlyFileChannel _outgoing; // the file we WRITE to
        private readonly CancellationTokenSource _cts;

        // used by SERVER to correlate InvokeAsync requests with replies
        private readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _pending =
            new(StringComparer.Ordinal);

        private FileRpcConnection(string clientId, Role role, AppendOnlyFileChannel incoming, AppendOnlyFileChannel outgoing, CancellationToken externalCt)
        {
            _clientId = clientId;
            _role = role;
            _incoming = incoming;
            _outgoing = outgoing;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);

            if (_role == Role.Server)
            {
                // server listens for rpc-reply on _incoming (client's .toServer)
                _ = Task.Run(ServerReplyListenLoop);
            }
        }

        /// <summary>CLIENT entry: connects using clientId + shared folder.</summary>
        public static Task<FileRpcConnection> ConnectAsync(string clientId, string folder, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(clientId)) throw new ArgumentNullException(nameof(clientId));
            if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentNullException(nameof(folder));

            var baseFull = Path.GetFullPath(folder)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var tail = Path.GetFileName(baseFull);

            // 🔧 avoid ...\NET1\NET1 when clientId == last folder name
            var clientDir = string.Equals(tail, clientId, StringComparison.OrdinalIgnoreCase)
                ? baseFull
                : Path.Combine(baseFull, clientId);

            Directory.CreateDirectory(clientDir);

            var toServer = Path.Combine(clientDir, $"{clientId}.toServer");   // client writes replies
            var fromServer = Path.Combine(clientDir, $"{clientId}.fromServer"); // client reads requests

            var incoming = new AppendOnlyFileChannel(fromServer); // read requests from server
            var outgoing = new AppendOnlyFileChannel(toServer);   // write replies to server

            var conn = new FileRpcConnection(clientId, Role.Client, incoming, outgoing, ct);
            return Task.FromResult(conn);
        }

        /// <summary>SERVER entry: binds to an already discovered clientId at folder.</summary>
        public static Task<FileRpcConnection> FromAcceptedAsync(string folder, string clientId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(clientId)) throw new ArgumentNullException(nameof(clientId));
            if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentNullException(nameof(folder));

            var baseFull = Path.GetFullPath(folder)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var tail = Path.GetFileName(baseFull);

            // 🔧 same “don’t double nest” rule on server side
            var clientDir = string.Equals(tail, clientId, StringComparison.OrdinalIgnoreCase)
                ? baseFull
                : Path.Combine(baseFull, clientId);

            Directory.CreateDirectory(clientDir);

            var toServer = Path.Combine(clientDir, $"{clientId}.toServer");   // server reads replies
            var fromServer = Path.Combine(clientDir, $"{clientId}.fromServer"); // server writes requests

            var incoming = new AppendOnlyFileChannel(toServer);   // read rpc-replies from client
            var outgoing = new AppendOnlyFileChannel(fromServer); // write rpc requests to client

            var conn = new FileRpcConnection(clientId, Role.Server, incoming, outgoing, ct);
            return Task.FromResult(conn);
        }

        // ---------------- SERVER API ----------------

        /// <summary>
        /// SERVER -> CLIENT: call a method on the connected client and await the result (like TCP RpcConnection.InvokeAsync).
        /// Payload is a WireFileRpc-serialized object?[] { methodName, args[] }.
        /// </summary>
        public async Task<T?> InvokeAsync<T>(string method, params object?[] args)
        {
            if (_role != Role.Server)
                throw new InvalidOperationException("InvokeAsync is only valid on server-side connections.");

            var call = new object?[] { method, args };
            var bytes = WireFileRpc.Serialize(call);

            var env = new Envelope
            {
                Source = "SERVER",
                Target = _clientId,
                Type = "rpc",
                PayloadBase64 = Envelope.ToB64(WireFileRpc.Wrap(bytes).Span)
            };

            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[env.Id] = tcs;

            await _outgoing.WriteLineAsync(Envelope.Serialize(env), _cts.Token).ConfigureAwait(false);

            using var _ = _cts.Token.Register(() => tcs.TrySetCanceled(_cts.Token));
            var replyBytes = await tcs.Task.ConfigureAwait(false);
            var unwrapped = WireFileRpc.Unwrap(replyBytes).ToArray();
            return WireFileRpc.Deserialize<T>(unwrapped);
        }

        private async Task ServerReplyListenLoop()
        {
            try
            {
                await foreach (var line in _incoming.ReadLinesAsync(_cts.Token))
                {
                    Envelope env;
                    try { env = Envelope.Deserialize(line); } catch { continue; }

                    // We expect rpc-reply targeted to SERVER
                    if (!string.Equals(env.Type, "rpc-reply", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(env.Target, "SERVER", StringComparison.OrdinalIgnoreCase)) continue;

                    if (_pending.TryRemove(env.Id, out var tcs))
                        tcs.TrySetResult(Envelope.FromB64(env.PayloadBase64));
                }
            }
            catch (OperationCanceledException) { }
        }

        // ---------------- CLIENT API ----------------

        /// <summary>
        /// CLIENT loop: read RPC requests from server, pass them into dispatcher, write correlated replies.
        /// </summary>
        public async Task RunClientLoopAsync(Func<string, object?[]?, Task<object?>> dispatcher)
        {
            if (_role != Role.Client)
                throw new InvalidOperationException("RunClientLoopAsync is only valid on client-side connections.");

            if (dispatcher is null) throw new ArgumentNullException(nameof(dispatcher));

            try
            {
                await foreach (var line in _incoming.ReadLinesAsync(_cts.Token))
                {
                    Envelope env;
                    try { env = Envelope.Deserialize(line); } catch { continue; }

                    if (!string.Equals(env.Type, "rpc", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(env.Target, _clientId, StringComparison.Ordinal)) continue;

                    var call = WireFileRpc.Deserialize<object?[]>(Envelope.FromB64(env.PayloadBase64));
                    var method = (call is { Length: > 0 } ? call[0] as string : null) ?? string.Empty;
                    var args = (call is { Length: > 1 } ? call[1] as object?[] : null) ?? Array.Empty<object?>();

                    object? resultObj;
                    try
                    {
                        resultObj = await dispatcher(method, args).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // mirror TCP behavior if it serializes structured errors; otherwise serialize exception
                        resultObj = ex;
                    }

                    var resBytes = WireFileRpc.Serialize(resultObj);
                    var reply = new Envelope
                    {
                        Id = env.Id,
                        Source = _clientId,
                        Target = "SERVER",
                        Type = "rpc-reply",
                        PayloadBase64 = Envelope.ToB64(WireFileRpc.Wrap(resBytes).Span)
                    };

                    await _outgoing.WriteLineAsync(Envelope.Serialize(reply), _cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
        }

        public async ValueTask DisposeAsync()
        {
            try { _cts.Cancel(); } catch { }
            foreach (var kv in _pending) kv.Value.TrySetCanceled();
            await _incoming.DisposeAsync();
            await _outgoing.DisposeAsync();
            _cts.Dispose();
        }
    }
}
