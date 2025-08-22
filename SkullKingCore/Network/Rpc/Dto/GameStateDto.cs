namespace SkullKingCore.Network.Rpc.Dto
{
    public sealed class GameStateDto
    {
        public int CurrentRound { get; set; }
        public int CurrentSubRound { get; set; }
        public int MaxRounds { get; set; }
        // ALWAYS includes every player's full Hand and Bids (no masking):
        public List<PlayerDto> Players { get; set; } = new();

    }
}
