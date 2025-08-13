using SkullKingCore.Cards.Base;

namespace SkullKingCore.Cards.Implementations
{
    public class KrakenCard : Card
    {
        public KrakenCard() : base(GameDefinitions.CardType.KRAKEN)
        {

        }

        public override string SubType()
        {
            return "";
        }

        public override string ToString()
        {
            return $"{CardType}";
        }
    }
}
