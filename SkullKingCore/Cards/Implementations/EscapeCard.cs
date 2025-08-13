using SkullKingCore.Cards.Base;

namespace SkullKingCore.Cards.Implementations
{
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
