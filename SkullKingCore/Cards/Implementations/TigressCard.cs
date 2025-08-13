using SkullKingCore.Cards.Base;
using SkullKingCore.GameDefinitions;

namespace SkullKingCore.Cards.Implementations
{
    public class TigressCard : Card
    {

        public CardType PlayedAsType { get; set; }

        public TigressCard() : base(CardType.TIGRESS)
        {
            PlayedAsType = CardType.PIRATE;
        }

        public override string SubType()
        {
            return $"{PlayedAsType}";
        }

        public override string ToString()
        {
            return $"{CardType} : {PlayedAsType}";
        }
    }
}
