using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace SkullKingCore.Network;

/// <summary>Newline-delimited JSON over TCP with correlation, handlers, and wire logging.</summary>
public sealed class TcpJsonLink : INetworkLink
{
    private readonly TcpClient _tcp;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;

    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> _pending = new();
    private readonly ConcurrentDictionary<string, Func<string?, JsonElement, CancellationToken, Task<JsonElement>>> _reqHandlers = new();
    private readonly ConcurrentDictionary<string, Func<string?, JsonElement, CancellationToken, Task>> _evtHandlers = new();

    public TcpJsonLink(TcpClient tcp)
    {
        _tcp = tcp;
        var stream = tcp.GetStream();
        _reader = new StreamReader(stream, new UTF8Encoding(false), leaveOpen: true);
        _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
    }

    public static async Task<TcpJsonLink> AcceptAsync(TcpListener listener, CancellationToken ct = default)
    {
        var client = await listener.AcceptTcpClientAsync(ct);
        return new TcpJsonLink(client);
    }

    public static async Task<TcpJsonLink> ConnectAsync(string host, int port, CancellationToken ct = default)
    {
        var client = new TcpClient();
        await client.ConnectAsync(host, port, ct);
        return new TcpJsonLink(client);
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            string? line;
            try
            {
                line = await _reader.ReadLineAsync(ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LINK] Read error: {ex}");
                break;
            }

            if (line is null) break;

            NetEnvelope env;
            try
            {
                env = JsonSerializer.Deserialize<NetEnvelope>(line, NetJson.Options)!;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LINK] Bad JSON: {ex}\n{line}");
                continue;
            }

            try
            {
                switch (env.Kind)
                {
                    case "res":
                        if (_pending.TryRemove(env.Id, out var tcs))
                            tcs.TrySetResult(env.Data);
                        else
                            Console.WriteLine($"[LINK] Unmatched response id: {env.Id}");
                        break;

                    case "req":
                        if (_reqHandlers.TryGetValue(env.Type, out var handler))
                        {
                            var rsp = await handler(env.PlayerName, env.Data, ct);
                            var reply = new NetEnvelope
                            {
                                Version = env.Version,
                                Id = env.Id,
                                Type = $"Response.{env.Type}",
                                Kind = "res",
                                PlayerName = env.PlayerName,
                                Data = rsp
                            };
                            string json = JsonSerializer.Serialize(reply, NetJson.Options);
                            await _writer.WriteLineAsync(json.AsMemory(), ct);
                        }
                        else
                        {
                            Console.WriteLine($"[LINK] No handler for request: {env.Type}");
                        }
                        break;

                    case "evt":
                        if (_evtHandlers.TryGetValue(env.Type, out var evt))
                            await evt(env.PlayerName, env.Data, ct);
                        else
                            Console.WriteLine($"[LINK] No handler for event: {env.Type}");
                        break;

                    case "err":
                        if (_pending.TryRemove(env.Id, out var errTcs))
                            errTcs.TrySetException(new InvalidOperationException(env.Data.ToString()));
                        break;

                    default:
                        Console.WriteLine($"[LINK] Unknown kind: {env.Kind}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LINK] Dispatch error for '{env.Type}/{env.Kind}': {ex}");
            }
        }
    }

    public async Task SendEventAsync(string type, string? playerName, object data, CancellationToken ct = default)
    {
        var env = new
        {
            version = 1,
            id = Guid.NewGuid().ToString("N"),
            type,
            kind = "evt",
            playerName,
            data
        };
        string json = JsonSerializer.Serialize(env, NetJson.Options);
        await _writer.WriteLineAsync(json.AsMemory(), ct);
    }

    public async Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        string type, string? playerName, TRequest data, TimeSpan timeout, CancellationToken ct = default)
        where TResponse : class
    {
        string id = Guid.NewGuid().ToString("N");
        var env = new
        {
            version = 1,
            id,
            type,
            kind = "req",
            playerName,
            data
        };
        var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = tcs;

        string json = JsonSerializer.Serialize(env, NetJson.Options);
        await _writer.WriteLineAsync(json.AsMemory(), ct);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (timeout != Timeout.InfiniteTimeSpan)
            cts.CancelAfter(timeout);
        await using var _ = cts.Token.Register(() => tcs.TrySetCanceled());

        var payload = await tcs.Task;
        var res = payload.Deserialize<TResponse>(NetJson.Options)!;
        return res;
    }

    public void OnRequest<TRequest, TResponse>(string type, Func<string?, TRequest, CancellationToken, Task<TResponse>> handler)
        where TRequest : class
        where TResponse : class
    {
        _reqHandlers[type] = async (pn, data, ct) =>
        {
            var req = data.Deserialize<TRequest>(NetJson.Options)!;
            var rsp = await handler(pn, req, ct);
            return JsonSerializer.SerializeToElement(rsp, NetJson.Options);
        };
    }

    public void OnEvent<TEvent>(string type, Func<string?, TEvent, CancellationToken, Task> handler)
        where TEvent : class
    {
        _evtHandlers[type] = async (pn, data, ct) =>
        {
            var evt = data.Deserialize<TEvent>(NetJson.Options)!;
            await handler(pn, evt, ct);
        };
    }

    public async ValueTask DisposeAsync()
    {
        try { _writer.Dispose(); } catch { }
        try { _reader.Dispose(); } catch { }
        try { _tcp.Close(); } catch { }
        await Task.CompletedTask;
    }
}
