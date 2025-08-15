using SkullKingCore.Core.Cards.Base;

namespace SkullKingCore.Core.Cards.Implementations
{
    public class LootCard : Card
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
