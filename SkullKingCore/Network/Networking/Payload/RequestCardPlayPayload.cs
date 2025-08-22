using SkullKingCore.Network.Rpc.Dto;

namespace SkullKingCore.Network.Networking.Payload
{
    public sealed class RequestCardPlayPayload
    {
        public GameStateDto GameState { get; set; } = new();
        public List<CardViewDto> Hand { get; set; } = new();
        public int MaxWaitMs { get; set; }
    }
}
