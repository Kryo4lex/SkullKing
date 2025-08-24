using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.SubCardTypes;
using System.Runtime.Serialization;

namespace SkullKingCore.Core.Cards.Implementations
{
    [DataContract]
    public class PirateCard : Card
    {
        [DataMember(Order = 1)]
        public PirateType PirateType { get; private set; }

        public override int? GenericValue => (int)PirateType;

        private PirateCard() { }

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
