using SkullKingCore.Cards.Base;

namespace SkullKingCore.Cards.Implementations
{
    public class LootCard : BaseCard
    {
        public LootCard() : base(GameDefinitions.CardType.LOOT)
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
