using SkullKingCore.Network.Rpc.Dto;

namespace SkullKingCore.Network.Networking.Payload
{
    public sealed class WaitForBidsReceivedPayload { public GameStateDto GameState { get; set; } = new(); }
}
