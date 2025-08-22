using SkullKingCore.Network.Rpc.Dto;

namespace SkullKingCore.Network.Networking.Payload
{
    public sealed class PlayerTimedOutPayload { public GameStateDto GameState { get; set; } = new(); public PlayerDto Player { get; set; } = new(); }
}
