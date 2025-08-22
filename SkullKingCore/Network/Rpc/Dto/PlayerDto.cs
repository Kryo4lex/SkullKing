namespace SkullKingCore.Network.Rpc.Dto
{
    public sealed class PlayerDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public List<CardViewDto> Hand { get; set; } = new();
        public List<BidDto> Bids { get; set; } = new();
    }
}
