using SkullKingCore.Core.Cards.Base;
using System.Runtime.Serialization;

namespace SkullKingCore.Core.Cards.Implementations
{
    [DataContract]
    public class KrakenCard : Card
    {

        public KrakenCard() : base(CardType.KRAKEN)
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
