using SkullKingCore.Core.Cards.Base;
using SkullKingCore.GameDefinitions;

namespace SkullKingCore.Core.Cards.Implementations
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
