using SkullKingCore.Core.Cards.Base;
using System.Runtime.Serialization;

namespace SkullKingCore.Core.Cards.Implementations
{
    [DataContract]
    public class EscapeCard : Card
    {

        public EscapeCard() : base(GameDefinitions.CardType.ESCAPE)
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
