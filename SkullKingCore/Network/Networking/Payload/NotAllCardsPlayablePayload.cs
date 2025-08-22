using SkullKingCore.Network.Rpc.Dto;

namespace SkullKingCore.Network.Networking.Payload
{
    public sealed class NotAllCardsPlayablePayload
    {
        public GameStateDto GameState { get; set; } = new();
        public List<CardViewDto> Allowed { get; set; } = new();
        public List<CardViewDto> NotAllowed { get; set; } = new();
    }
}
