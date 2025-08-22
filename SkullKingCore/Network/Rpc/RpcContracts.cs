// SkullKing.Network/Rpc/RpcContracts.cs
#nullable enable

using System.Text.Json;

namespace SkullKingCore.Network.Rpc
{
    /// <summary>
    /// Request envelope sent from server -> client.
    /// Method is the RPC method name (e.g., nameof(IGameController.RequestBidAsync)).
    /// Payload is a method-specific DTO serialized with System.Text.Json.
    /// </summary>
    public sealed class RpcEnvelope<TPayload>
    {
        public string Method { get; set; } = "";
        public TPayload? Payload { get; set; }
    }

    /// <summary>
    /// Response envelope sent from client -> server.
    /// If Error is not null/empty, server should treat the call as failed.
    /// </summary>
    public sealed class RpcResponse<TResult>
    {
        public string? Error { get; set; }
        public TResult? Result { get; set; }
    }

    /// <summary>
    /// Central JSON options so both sides use the same casing & behavior.
    /// (Optional helper for convenience; you can also inline these options).
    /// </summary>
    public static class RpcJson
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }
}
