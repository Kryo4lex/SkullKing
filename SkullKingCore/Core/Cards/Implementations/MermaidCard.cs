using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.SubCardTypes;
using System.Runtime.Serialization;

namespace SkullKingCore.Core.Cards.Implementations
{
    [DataContract]
    public class MermaidCard : Card
    {
        [DataMember(Order = 1)]
        public MermaidType MermaidType { get; private set; }

        public override int? GenericValue => (int)MermaidType;

        private MermaidCard() { }

        public MermaidCard(MermaidType mermaidType) : base(CardType.MERMAID)
        {
            MermaidType = mermaidType;
        }

        public override string SubType()
        {
            return $"{MermaidType}";
        }

        public override string ToString()
        {
            return $"{CardType} : {MermaidType}";
        }
    }
}
