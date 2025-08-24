namespace SkullKingCore.Core.Cards
{
    public class CardTypeCardAmountPair
    {

        public CardType CardType { get; private set; }

        public int CardAmount { get; private set; }

        public CardTypeCardAmountPair(CardType cardType, int cardAmount)
        {
            CardType = cardType;
            CardAmount = cardAmount;
        }

    }
}
