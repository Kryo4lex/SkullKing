using SkullKingCore.Cards.Base;

namespace SkullKingCore.Cards.Implementations
{
    public class EscapeCard : BaseCard
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
