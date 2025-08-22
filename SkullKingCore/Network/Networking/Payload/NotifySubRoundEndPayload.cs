using SkullKingCore.Network.Rpc.Dto;

namespace SkullKingCore.Network.Networking.Payload
{
    public sealed class NotifySubRoundEndPayload { public GameStateDto GameState { get; set; } = new(); }
}
