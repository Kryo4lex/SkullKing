using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.Implementations;
using SkullKingCore.Core.Cards.SubCardTypes;

namespace SkullKingCore.Core.Cards.Factory
{
    public static class CardFactory
    {
        public static Card Create(CardType cardType, int value = -1)
        {

            switch (cardType)
            {
                case CardType.SKULL_KING:
                    return new SkullKingCard();

                case CardType.PIRATE:
                    return new PirateCard((PirateType)value);

                case CardType.MERMAID:
                    return new MermaidCard((MermaidType)value);

                case CardType.ESCAPE:
                    return new EscapeCard();

                case CardType.TIGRESS:
                    return new TigressCard();

                case CardType.LOOT:
                    return new LootCard();

                case CardType.KRAKEN:
                    return new KrakenCard();

                case CardType.WHITE_WHALE:
                    return new WhiteWhaleCard();

                case CardType.GREEN:
                case CardType.YELLOW:
                case CardType.PURPLE:
                case CardType.BLACK:

                    return new NumberCard(cardType, value);

                default:
                    throw new NotSupportedException($"Card type {cardType} not supported.");
            }
        }
    }
}
