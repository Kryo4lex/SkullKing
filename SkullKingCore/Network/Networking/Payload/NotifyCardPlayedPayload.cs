using SkullKingCore.Network.Rpc.Dto;

namespace SkullKingCore.Network.Networking.Payload
{
    public sealed class NotifyCardPlayedPayload
    {
        public PlayerDto Player { get; set; } = new();
        public CardViewDto Card { get; set; } = new();
    }
}
