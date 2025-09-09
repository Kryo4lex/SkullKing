using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Logging;

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

        public static void PrintListFancy(this List<Card> cards, string headerIndex = "Index", string headerCardType = "Card Type", string headerSubType = "Sub Type")
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
