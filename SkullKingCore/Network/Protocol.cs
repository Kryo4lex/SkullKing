using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkullKingCore.Network;

/// <summary>Human-readable envelope for all messages.</summary>
public sealed class NetEnvelope
{
    [JsonPropertyName("version")] public int Version { get; init; } = 1;
    [JsonPropertyName("id")] public string Id { get; init; } = Guid.NewGuid().ToString("N");
    [JsonPropertyName("type")] public string Type { get; init; } = "";   // e.g., "RequestBid"
    [JsonPropertyName("kind")] public string Kind { get; init; } = "";   // "req" | "res" | "evt" | "err"
    [JsonPropertyName("playerName")] public string? PlayerName { get; init; }  // origin/target (readable)
    [JsonPropertyName("data")] public JsonElement Data { get; init; }    // payload
}

public static class NetJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}
