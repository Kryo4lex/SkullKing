namespace SkullKingCore.Network.Rpc.Dto
{
    public sealed class CardViewDto
    {
        public string CardType { get; set; } = "";   // enum name, e.g. "NUMBER", "PIRATE", "TIGRESS"
        public string Display { get; set; } = "";    // pretty text for UI
        public int? GenericValue { get; set; }       // only used for NUMBER cards; null otherwise
        public string? TigressMode { get; set; }     // "Escape" | "Pirate" | null
    }
}
