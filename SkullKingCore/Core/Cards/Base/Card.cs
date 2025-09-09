using SkullKingCore.Core.Cards.Interfaces;
using SkullKingCore.Logging;
using System.Runtime.Serialization;

namespace SkullKingCore.Core.Cards.Base
{

    [DataContract]
    public abstract class Card : ICard
    {

        public static readonly List<CardType> NumberCardTypes = new List<CardType>
        {
            CardType.PURPLE,
            CardType.GREEN,
            CardType.YELLOW,
            CardType.BLACK,
        };

        //public Guid CardGuid { get; set; } = Guid.NewGuid();

        //protected set, so that child classes can set
        //changed to regular set, so that it can be modified, e.g. for the Tigress
        [DataMember(Order = 1)]
        public CardType CardType { get; set; }

        //abstract to force child classes for their own implementation
        //virtual so that it can be overriden
        [DataMember(Order = 2)]
        public virtual int? GenericValue { get; set; } = null;

        [DataMember(Order = 3)]
        public Guid GuId { get; private set; }

        protected Card(CardType type) : this()  // ensure GuId is set
        {
            CardType = type;
        }

        // Parameterless ctor for serializer (does not change runtime behavior)
        protected Card()
        {
            GuId = Guid.NewGuid();
        }

        //This base class is abstract (childs must implement it) and subclasses must override ToString().
        public abstract override string ToString();

        public abstract string SubType();

        public static bool IsNumberCard(CardType cardType)
        {
            return NumberCardTypes.Contains(cardType);
        }

        public static bool IsNumberCard(Card card)
        {
            return NumberCardTypes.Contains(card.CardType);
        }

        public bool IsNumberCard()
        {
            return NumberCardTypes.Contains(CardType);
        }

    }
}
