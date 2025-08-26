using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkullKingCore.Network.FileRpc.Transport
{
    /// <summary>
    /// One-line JSON envelope. Payload is raw bytes encoded as Base64.
    /// </summary>
    internal sealed class Envelope
    {
        [JsonPropertyName("id")] public string Id { get; set; } = Guid.NewGuid().ToString("N");
        [JsonPropertyName("source")] public string Source { get; set; } = "";
        [JsonPropertyName("target")] public string Target { get; set; } = ""; // "SERVER" or {clientId}
        [JsonPropertyName("type")] public string Type { get; set; } = "";     // "rpc" | "rpc-reply" | "event"
        [JsonPropertyName("payloadB64")] public string PayloadBase64 { get; set; } = "";
        [JsonPropertyName("ts")] public long UnixTs { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public static string Serialize(Envelope env) => JsonSerializer.Serialize(env);
        public static Envelope Deserialize(string line) => JsonSerializer.Deserialize<Envelope>(line)!;

        public static string ToB64(ReadOnlySpan<byte> bytes) => Convert.ToBase64String(bytes);
        public static byte[] FromB64(string b64) => Convert.FromBase64String(b64);
    }
}
