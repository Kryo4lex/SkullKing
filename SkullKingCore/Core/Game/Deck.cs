using SkullKingCore.Core.Cards;
using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.Factory;

namespace SkullKingCore.Core.Game
{
    public static class Deck
    {

        public const int NumberCardLowestNumber = 1;
        public const int NumberCardHighestNumber = 14;

        public static readonly List<CardTypeCardAmountPair> GameCardComposition = new List<CardTypeCardAmountPair>()
        {
            new CardTypeCardAmountPair(CardType.GREEN,      14 ),
            new CardTypeCardAmountPair(CardType.LILA,       14 ),
            new CardTypeCardAmountPair(CardType.YELLOW,     14 ),
            new CardTypeCardAmountPair(CardType.BLACK,      14 ),
            new CardTypeCardAmountPair(CardType.PIRATE,      5 ),
            new CardTypeCardAmountPair(CardType.TIGRESS,     1 ),
            new CardTypeCardAmountPair(CardType.SKULL_KING,  1 ),
            new CardTypeCardAmountPair(CardType.MERMAID,     2 ),
            new CardTypeCardAmountPair(CardType.ESCAPE,      5 ),
          //new CardTypeCardAmountPair(CardType.LOOT,        2 ),
            new CardTypeCardAmountPair(CardType.KRAKEN,      1 ),
            new CardTypeCardAmountPair(CardType.WHITE_WHALE, 1 ),
        };

        public static List<Card> CreateDeck()
        {

            List<Card> gameCards = new List<Card>();

            foreach (CardTypeCardAmountPair cardTypeCardAmountPair in GameCardComposition)
            {
                for (int cardTypeElementCounter = 1; cardTypeElementCounter <= cardTypeCardAmountPair.CardAmount; cardTypeElementCounter++)
                {
                    Card newCard = CardFactory.Create(cardTypeCardAmountPair.CardType, cardTypeElementCounter);

                    gameCards.Add(newCard);
                }
            }

            return gameCards;
        }

    }
}
