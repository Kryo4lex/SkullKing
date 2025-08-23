using SkullKingCore.Core.Cards.Base;

namespace SkullKingCore.Core.Cards.Extensions
{
    public static class CardListExtensions
    {
        public static bool RemoveByGuid(this List<Card> cards, Guid id)
        {
            var card = cards.FirstOrDefault(c => c.GuId == id);
            if (card != null)
            {
                cards.Remove(card);
                return true;
            }
            return false;
        }
    }
}
