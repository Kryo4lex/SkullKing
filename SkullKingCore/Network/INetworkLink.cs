namespace SkullKingCore.Network;

/// <summary>Transport abstraction (JSON over TCP, WebSockets, etc.).</summary>
public interface INetworkLink : IAsyncDisposable
{
    Task SendEventAsync(string type, string? playerName, object data, CancellationToken ct = default);

    Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        string type, string? playerName, TRequest data, TimeSpan timeout, CancellationToken ct = default)
        where TResponse : class;

    void OnRequest<TRequest, TResponse>(string type, Func<string?, TRequest, CancellationToken, Task<TResponse>> handler)
        where TRequest : class
        where TResponse : class;

    void OnEvent<TEvent>(string type, Func<string?, TEvent, CancellationToken, Task> handler)
        where TEvent : class;

    Task RunAsync(CancellationToken ct = default);
}
