using SkullKing.Network.Networking;
using System.Net.Sockets;

namespace SkullKing.Network.Rpc
{
    public sealed class RpcConnection : IAsyncDisposable
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private readonly CancellationTokenSource _cts = new();

        private RpcConnection(TcpClient client)
        {
            _client = client;
            _stream = client.GetStream();
        }

        public static async Task<RpcConnection> ConnectAsync(string host, int port, CancellationToken ct)
        {
            var tcp = new TcpClient();
            await tcp.ConnectAsync(host, port, ct).ConfigureAwait(false);
            return new RpcConnection(tcp);
        }

        public static Task<RpcConnection> FromAcceptedAsync(TcpClient accepted, CancellationToken ct)
            => Task.FromResult(new RpcConnection(accepted));

        public async Task<T?> InvokeAsync<T>(string method, params object?[] args)
        {
            var env = new RpcEnvelope { Method = method, Args = args ?? Array.Empty<object?>() };
            await Framing.WriteFrameAsync(_stream, Wire.Serialize(env), _cts.Token).ConfigureAwait(false);

            var respBytes = await Framing.ReadFrameAsync(_stream, _cts.Token).ConfigureAwait(false)
                           ?? throw new InvalidOperationException("Client disconnected.");

            var resp = Wire.Deserialize<RpcResponse>(respBytes);
            if (resp.Error is not null) throw new InvalidOperationException(resp.Error);
            return (T?)resp.Result;
        }

        public async Task RunClientLoopAsync(Func<string, object?[], Task<object?>> handler)
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    var reqBytes = await Framing.ReadFrameAsync(_stream, _cts.Token).ConfigureAwait(false);
                    if (reqBytes is null) break;

                    RpcEnvelope env;
                    try { env = Wire.Deserialize<RpcEnvelope>(reqBytes); }
                    catch (Exception ex)
                    {
                        var bad = new RpcResponse { Error = "Bad request: " + ex.Message };
                        await Framing.WriteFrameAsync(_stream, Wire.Serialize(bad), _cts.Token).ConfigureAwait(false);
                        continue;
                    }

                    RpcResponse resp;
                    try
                    {
                        var result = await handler(env.Method, env.Args).ConfigureAwait(false);
                        resp = new RpcResponse { Result = result };
                    }
                    catch (Exception ex)
                    {
                        resp = new RpcResponse { Error = ex.ToString() };
                    }

                    await Framing.WriteFrameAsync(_stream, Wire.Serialize(resp), _cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            finally { await DisposeAsync(); }
        }

        public async ValueTask DisposeAsync()
        {
            try { _cts.Cancel(); } catch { }
            try { _stream.Dispose(); } catch { }
            try { _client.Dispose(); } catch { }
            await Task.CompletedTask;
        }
    }
}
