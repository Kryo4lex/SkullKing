using SkullKingCore.Cards.Base;
using SkullKingCore.GameDefinitions;
using SkullKingCore.GameDefinitions.SubCardTypes;

namespace SkullKingCore.Cards.Implementations
{

    public class MermaidCard : BaseCard
    {
        public MermaidType MermaidType { get; private set; }

        public override int? GenericValue => (int)MermaidType;

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
