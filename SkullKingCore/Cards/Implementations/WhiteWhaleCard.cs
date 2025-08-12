using SkullKingCore.Cards.Base;

namespace SkullKingCore.Cards.Implementations
{
    public class WhiteWhaleCard : BaseCard
    {
        public WhiteWhaleCard() : base(GameDefinitions.CardType.WHITE_WHALE)
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
