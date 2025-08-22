// SkullKing.Network/Rpc/RpcConnection.cs
// Minimal JSON RPC connection for client side: reads RpcEnvelope<JsonElement>, writes RpcResponse<TResult>.

#nullable enable

using SkullKingCore.Network.Networking;
using System.Net.Sockets;
using System.Text.Json;

namespace SkullKingCore.Network.Rpc
{

    /// <summary>
    /// Client-side RPC connection that handles a continuous request->response loop.
    /// For each received envelope, it calls the provided handler and returns its result.
    /// </summary>
    public sealed class RpcConnection : IAsyncDisposable
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private readonly CancellationTokenSource _cts = new();
        private readonly Func<string, JsonElement?, Task<object?>> _handler;
        private readonly Task _loop;

        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        private RpcConnection(TcpClient client, Func<string, JsonElement?, Task<object?>> handler)
        {
            _client = client;
            _stream = client.GetStream();
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _loop = Task.Run(LoopAsync);
        }

        public static async Task<RpcConnection> ConnectAsync(
            string host,
            int port,
            Func<string, JsonElement?, Task<object?>> handler,
            CancellationToken ct)
        {
            var c = new TcpClient();
            await c.ConnectAsync(host, port, ct).ConfigureAwait(false);
            return new RpcConnection(c, handler);
        }

        private async Task LoopAsync()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    var inBytes = await Framing.ReadFrameAsync(_stream, _cts.Token).ConfigureAwait(false);
                    if (inBytes is null) break; // disconnected

                    RpcEnvelope<JsonElement?> env;
                    try
                    {
                        env = JsonSerializer.Deserialize<RpcEnvelope<JsonElement?>>(inBytes, Json)
                              ?? throw new InvalidOperationException("Invalid envelope.");
                    }
                    catch (Exception ex)
                    {
                        var bad = new RpcResponse<object?> { Error = $"Bad request: {ex.Message}" };
                        await Framing.WriteFrameAsync(_stream, JsonSerializer.SerializeToUtf8Bytes(bad, Json), _cts.Token)
                                     .ConfigureAwait(false);
                        continue;
                    }

                    RpcResponse<object?> resp;
                    try
                    {
                        var result = await _handler(env.Method, env.Payload).ConfigureAwait(false);
                        resp = new RpcResponse<object?> { Result = result };
                    }
                    catch (Exception ex)
                    {
                        resp = new RpcResponse<object?> { Error = ex.Message };
                    }

                    var outBytes = JsonSerializer.SerializeToUtf8Bytes(resp, Json);
                    await Framing.WriteFrameAsync(_stream, outBytes, _cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { /* normal */ }
            finally
            {
                try { _stream.Dispose(); } catch { }
                try { _client.Dispose(); } catch { }
            }
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            try { await _loop.ConfigureAwait(false); } catch { /* ignore */ }
            _cts.Dispose();
        }
    }
}
