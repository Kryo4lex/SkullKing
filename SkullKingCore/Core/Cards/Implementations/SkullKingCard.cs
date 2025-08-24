using SkullKingCore.Core.Cards.Base;
using System.Runtime.Serialization;

namespace SkullKingCore.Core.Cards.Implementations
{
    [DataContract]
    public class SkullKingCard : Card
    {

        public SkullKingCard() : base(CardType.SKULL_KING)
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
