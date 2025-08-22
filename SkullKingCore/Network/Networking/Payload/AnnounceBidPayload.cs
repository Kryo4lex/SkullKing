using SkullKingCore.Network.Rpc.Dto;

namespace SkullKingCore.Network.Networking.Payload
{
    public sealed class AnnounceBidPayload
    {
        public GameStateDto GameState { get; set; } = new();
        public PlayerDto Player { get; set; } = new();
        public int Bid { get; set; }
        public int MaxWaitMs { get; set; }
    }
}
