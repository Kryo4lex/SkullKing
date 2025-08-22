using SkullKingCore.Network.Rpc.Dto;

namespace SkullKingCore.Network.Networking.Payload
{
    public sealed class GameWinnersPayload { public GameStateDto GameState { get; set; } = new(); public List<PlayerDto> Winners { get; set; } = new(); }
}
