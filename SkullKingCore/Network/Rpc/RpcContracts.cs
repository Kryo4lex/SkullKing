using System.Runtime.Serialization;

namespace SkullKing.Network.Rpc
{
    /// <summary>
    /// Envelope for a remote procedure call request.
    /// </summary>
    [DataContract]
    public sealed class RpcEnvelope
    {
        /// <summary>
        /// Name of the method to call.
        /// </summary>
        [DataMember(Order = 1, IsRequired = true)]
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// Arguments for the call. May contain domain objects such as GameState, Player, Card, etc.
        /// </summary>
        [DataMember(Order = 2, IsRequired = true)]
        public object?[] Args { get; set; } = Array.Empty<object?>();
    }

    /// <summary>
    /// Envelope for a remote procedure call response.
    /// </summary>
    [DataContract]
    public sealed class RpcResponse
    {
        /// <summary>
        /// Result of the call (if any). May be null.
        /// </summary>
        [DataMember(Order = 1)]
        public object? Result { get; set; }

        /// <summary>
        /// Error message if the call failed. Null if success.
        /// </summary>
        [DataMember(Order = 2)]
        public string? Error { get; set; }
    }
}
