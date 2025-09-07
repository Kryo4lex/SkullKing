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

        private string? _imageName;

        public string ImageName => _imageName ??= GenerateImageName();

        private string GenerateImageName()
        {
            var imageName = CardType.ToString();

            var subTypeStr = SubType()?.ToString();
            if (!string.IsNullOrEmpty(subTypeStr))
            {
                imageName += "_" + subTypeStr;
            }

            return imageName;
        }

        public static void PrintListFancy(List<Card> cards, string headerIndex = "Index", string headerCardType = "Card Type", string headerSubType = "Sub Type")
        {
            // Determine the max width for each column based on headers and content
            int maxLengthCardType = Math.Max(headerCardType.Length, cards.Max(obj => obj.CardType.ToString().Length));
            int maxLengthSubType = Math.Max(headerSubType.Length, cards.Max(obj => obj.SubType().Length));
            int maxLengthIndex = Math.Max(headerIndex.Length, cards.Count.ToString().Length);

            string separator = "+" +
                new string('-', maxLengthIndex + 2) + "+" +
                new string('-', maxLengthCardType + 2) + "+" +
                new string('-', maxLengthSubType + 2) + "+";


            // Print the table header
            string headerLine = $"| {headerIndex.PadLeft(maxLengthIndex)} | {headerCardType.PadRight(maxLengthCardType)} | {headerSubType.PadRight(maxLengthSubType)} |";

            Logger.Instance.WriteToConsoleAndLog(separator);
            Logger.Instance.WriteToConsoleAndLog(headerLine);
            Logger.Instance.WriteToConsoleAndLog(separator);

            int cardIndex = 0;

            // Print each row
            foreach (Card card in cards)
            {
                string line =
                    $"| {cardIndex.ToString().PadLeft(maxLengthIndex)} | " +
                    $"{card.CardType.ToString().PadRight(maxLengthCardType)} | " +
                    $"{card.SubType().PadRight(maxLengthSubType)} |";

                cardIndex++;

                Logger.Instance.WriteToConsoleAndLog(line);
            }

            Logger.Instance.WriteToConsoleAndLog(separator);
        }

    }
}
