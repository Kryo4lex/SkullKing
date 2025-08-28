using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SkullKingCore.Network.WebRpc.Rpc
{
    /// <summary>
    /// Client-side WebRpc connection (long-poll).
    /// - GET  /rpc/next?clientId=...   (blocks ~55-60s; returns CallId + payload or 204)
    /// - POST /rpc/reply/{callId}?clientId=...  (binary body with serialized result)
    /// Additionally pings /rpc/ping every ~30s to keep liveness behind strict proxies.
    /// </summary>
    public sealed class WebRpcConnection : IAsyncDisposable
    {
        private readonly HttpClient _http;
        private readonly bool _disposeHttp;
        private readonly Uri _base;
        private readonly string _clientId;
        private readonly CancellationTokenSource _cts = new();
        private Task? _pingLoop;

        private WebRpcConnection(HttpClient http, bool disposeHttp, string baseUrl, string clientId, TimeSpan? longPollTimeout)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _disposeHttp = disposeHttp;

            _http.Timeout = longPollTimeout ?? TimeSpan.FromSeconds(65);
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("SkullKing-WebRpc/1.0");
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _http.DefaultRequestHeaders.ConnectionClose = false;

            _base = new Uri(baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/", UriKind.Absolute);
            _clientId = clientId;

            _pingLoop = Task.Run(PingLoopAsync);
        }

        public static Task<WebRpcConnection> ConnectAsync(string baseUrl, string clientId, CancellationToken _)
        {
            var handler = new HttpClientHandler
            {
                UseProxy = true,
                Proxy = WebRequest.DefaultWebProxy,
                DefaultProxyCredentials = CredentialCache.DefaultCredentials,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            var http = new HttpClient(handler, disposeHandler: true);
            return Task.FromResult(new WebRpcConnection(http, true, baseUrl, clientId, null));
        }

        public static Task<WebRpcConnection> ConnectAsync(
            string baseUrl,
            string clientId,
            CancellationToken _,
            HttpMessageHandler? handler,
            TimeSpan? longPollTimeout = null)
        {
            HttpClient http;
            bool disposeHttp;
            if (handler is null)
            {
                var def = new HttpClientHandler
                {
                    UseProxy = true,
                    Proxy = WebRequest.DefaultWebProxy,
                    DefaultProxyCredentials = CredentialCache.DefaultCredentials,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };
                http = new HttpClient(def, disposeHandler: true);
                disposeHttp = true;
            }
            else
            {
                http = new HttpClient(handler, disposeHandler: false);
                disposeHttp = true;
            }

            return Task.FromResult(new WebRpcConnection(http, disposeHttp, baseUrl, clientId, longPollTimeout));
        }

        private sealed class NextCallDto
        {
            public string? CallId { get; set; }
            public string? PayloadB64 { get; set; }
        }

        public async Task RunClientLoopAsync(Func<string, object?[]?, Task<object?>> dispatcher)
        {
            if (dispatcher is null) throw new ArgumentNullException(nameof(dispatcher));
            var ct = _cts.Token;

            while (!ct.IsCancellationRequested)
            {
                HttpResponseMessage resp;
                try
                {
                    var url = new Uri(_base, $"rpc/next?clientId={Uri.EscapeDataString(_clientId)}");
                    resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
                catch (HttpRequestException)
                {
                    await Task.Delay(500, ct).ConfigureAwait(false);
                    continue;
                }

                if (resp.StatusCode == HttpStatusCode.NoContent)
                    continue;

                resp.EnsureSuccessStatusCode();

                var dto = await resp.Content.ReadFromJsonAsync<NextCallDto>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct).ConfigureAwait(false);

                if (dto?.CallId is null || dto.PayloadB64 is null)
                    continue;

                var bytes = Convert.FromBase64String(dto.PayloadB64);
                var call = WireWebRpc.Deserialize<object?[]>(bytes);

                var method = (call is { Length: > 0 } ? call[0] as string : null) ?? string.Empty;
                var args = (call is { Length: > 1 } ? call[1] as object?[] : null) ?? Array.Empty<object?>();

                object? resultObj;
                try
                {
                    resultObj = await dispatcher(method, args).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    resultObj = ex; // or wrap it if you have an error envelope
                }

                var replyBytes = WireWebRpc.Serialize(resultObj);
                using var content = new ByteArrayContent(replyBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                try
                {
                    var replyUrl = new Uri(_base, $"rpc/reply/{dto.CallId}?clientId={Uri.EscapeDataString(_clientId)}");
                    using var reply = await _http.PostAsync(replyUrl, content, ct).ConfigureAwait(false);
                    reply.EnsureSuccessStatusCode();
                }
                catch (OperationCanceledException) { break; }
                catch (HttpRequestException)
                {
                    await Task.Delay(250, ct).ConfigureAwait(false);
                }
            }
        }

        private async Task PingLoopAsync()
        {
            var ct = _cts.Token;
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var url = new Uri(_base, $"rpc/ping?clientId={Uri.EscapeDataString(_clientId)}");
                    using var _ = await _http.GetAsync(url, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
                catch { /* ignore transient */ }

                try { await Task.Delay(TimeSpan.FromSeconds(30), ct).ConfigureAwait(false); }
                catch { break; }
            }
        }

        public async ValueTask DisposeAsync()
        {
            try { _cts.Cancel(); } catch { }
            try { if (_pingLoop is not null) await _pingLoop; } catch { }
            if (_disposeHttp) { try { _http.Dispose(); } catch { } }
            await Task.CompletedTask;
        }
    }
}
