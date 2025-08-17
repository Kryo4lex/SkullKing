using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.Implementations;

namespace SkullKingCore.Network;

/// <summary>Produces human-readable labels for cards (for wire & logs).</summary>
public static class CardLabeler
{
    public static string ToLabel(Card c) => c switch
    {
        NumberCard n => $"{n.CardType}-{n.Number}",
        PirateCard p => $"PIRATE({p.SubType()})",
        MermaidCard m => $"MERMAID({m.SubType()})",
        TigressCard => "TIGRESS",
        EscapeCard => "ESCAPE",
        SkullKingCard => "SKULL_KING",
        WhiteWhaleCard => "WHITE_WHALE",
        KrakenCard => "KRAKEN",
        LootCard => "LOOT",
        _ => $"{c.CardType}{(string.IsNullOrWhiteSpace(c.SubType()) ? "" : $"({c.SubType()})")}"
    };
}
