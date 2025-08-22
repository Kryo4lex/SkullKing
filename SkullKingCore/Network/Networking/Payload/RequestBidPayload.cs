using SkullKingCore.Network.Rpc.Dto;

namespace SkullKingCore.Network.Networking.Payload
{
    public sealed class RequestBidPayload
    {
        public GameStateDto GameState { get; set; } = new();
        public int RoundNumber { get; set; }
        public List<CardViewDto> Hand { get; set; } = new();
        public int MaxWaitMs { get; set; }
    }
}
