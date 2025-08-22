using SkullKingCore.Network.Rpc.Dto;

namespace SkullKingCore.Network.Networking.Payload
{
    public sealed class SubRoundWinnerPayload
    {
        public PlayerDto? Player { get; set; }
        public CardViewDto? WinningCard { get; set; }
        public int Round { get; set; }
    }
}
