using SkullKingCore.Cards.Base;
using SkullKingCore.GameDefinitions;

namespace SkullKingCore.Cards.Implementations
{

    public class SkullKingCard : BaseCard
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
