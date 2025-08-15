using SkullKingCore.Core.Cards.Base;
using SkullKingCore.GameDefinitions;
using SkullKingCore.GameDefinitions.SubCardTypes;

namespace SkullKingCore.Core.Cards.Implementations
{
    public class PirateCard : Card
    {

        public PirateType PirateType { get; private set; }

        public override int? GenericValue => (int)PirateType;

        public PirateCard(PirateType pirateType) : base(CardType.PIRATE)
        {
            PirateType = pirateType;
        }

        public override string SubType()
        {
            return $"{PirateType}";
        }

        public override string ToString()
        {
            return $"{CardType} : {PirateType}";
        }
    }
}
