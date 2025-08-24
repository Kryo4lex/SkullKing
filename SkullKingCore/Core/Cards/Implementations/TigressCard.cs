using SkullKingCore.Core.Cards.Base;
using System.Runtime.Serialization;

namespace SkullKingCore.Core.Cards.Implementations
{
    [DataContract]
    public class TigressCard : Card
    {

        /// <summary>When played, Tigress can be ESCAPE or PIRATE.</summary>
        [DataMember(Order = 1)]
        public CardType PlayedAsType { get; set; }

        public TigressCard() : base(CardType.TIGRESS)
        {
            PlayedAsType = CardType.JOKER;
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
