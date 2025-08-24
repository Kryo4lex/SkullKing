using SkullKingCore.Core.Cards.Base;
using System.Runtime.Serialization;

namespace SkullKingCore.Core.Cards.Implementations
{
    [DataContract]
    public class NumberCard : Card
    {

        public const int MinValue =  1;
        public const int MaxValue = 14;

        [DataMember(Order = 1)]
        public int Number { get; set; }

        public override int? GenericValue => Number;


        // For serializer
        private NumberCard() { }

        public NumberCard(CardType cardType, int number) : base(cardType)
        {
            if(!IsNumberCard())
            {
                throw new ArgumentException("Card must be a number card!");
            }

            if(number < MinValue || number > MaxValue)
            {
                throw new ArgumentException($"Card's number must be between {MinValue} and {MaxValue}. Actual number: {number}");
            }

            Number = number;
        }

        public override string SubType()
        {
            return $"{Number}";
        }

        public override string ToString()
        {
            return $"{CardType} : {Number}";
        }
    }
}
