using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.Implementations;
using SkullKingCore.GameDefinitions;

namespace SkullKingCore.Core
{

    /// <summary>
    /// Calculates bonus points granted to the winner of a trick, according to Skull King rules:
    /// - +10 for each Yellow/Purple/Green 14 present in the trick.
    /// - +20 for each Black (Jolly Roger) 14 present in the trick.
    /// - +20 per Mermaid if a Pirate wins.
    /// - +30 per Pirate if the Skull King wins.
    /// - +40 per Skull King if a Mermaid wins.
    /// Notes:
    /// - Bonuses stack; e.g., capturing a Yellow 14 and a Skull King with a Mermaid yields 10 + 40.
    /// - “Order of play is not a determining factor”; only the winner and the set of cards matter.
    /// </summary>
    public static class TrickBonusPointResolver
    {

        /// <summary>
        /// Compute the trick bonus for the winner of this trick.
        /// </summary>
        /// <param name="cardsPlayed">Cards in the order they were played for this trick.</param>
        /// <param name="winnerIndex">Index into <paramref name="cardsPlayed"/> of the winning card.</param>
        /// <returns>Total bonus points awarded to the winner for this trick.</returns>
        public static int ComputeTrickBonus(IReadOnlyList<Card> cardsPlayed, int winnerIndex)
        {
            if (cardsPlayed is null || cardsPlayed.Count == 0)
                return 0;

            if (winnerIndex < 0 || winnerIndex >= cardsPlayed.Count)
                throw new ArgumentOutOfRangeException(nameof(winnerIndex));

            var winner = cardsPlayed[winnerIndex];

            var winnerType = GetEffectiveType(winner);

            int bonus = 0;

            // 1) Number 14 bonuses (awarded to the trick winner)
            foreach (var c in cardsPlayed)
            {
                var t = GetEffectiveType(c);
                if (Card.IsNumberCard(t) && (c.GenericValue ?? 0) == 14)
                {
                    // Standard suits: Yellow/Lila(Purple)/Green → +10 each
                    if (t == CardType.YELLOW || t == CardType.LILA || t == CardType.GREEN)
                        bonus += 10;
                    // Black (Jolly Roger) → +20 each
                    else if (t == CardType.BLACK)
                        bonus += 20;
                    // Other suits (if any expansions exist) grant no 14 bonus by these rules
                }
            }

            // Count characters present in the trick (not just the winner)
            int piratesInTrick = cardsPlayed.Count(c => GetEffectiveType(c) == CardType.PIRATE);
            int mermaidsInTrick = cardsPlayed.Count(c => GetEffectiveType(c) == CardType.MERMAID);
            int skullKingsInTrick = cardsPlayed.Count(c => GetEffectiveType(c) == CardType.SKULL_KING);

            // 2) Character capture bonuses depend on WHO won
            switch (winnerType)
            {
                case CardType.PIRATE:
                    // 20 points for EACH Mermaid taken by a Pirate
                    if (mermaidsInTrick > 0)
                        bonus += 20 * mermaidsInTrick;
                    break;

                case CardType.SKULL_KING:
                    // 30 points for EACH Pirate taken by the Skull King
                    if (piratesInTrick > 0)
                        bonus += 30 * piratesInTrick;
                    break;

                case CardType.MERMAID:
                    // 40 points for taking the Skull King with a Mermaid
                    if (skullKingsInTrick > 0)
                        bonus += 40 * skullKingsInTrick;
                    break;

                default:
                    // No character-capture bonus for other winners
                    break;
            }

            return bonus;
        }

        /// <summary>
        /// Map cards like Tigress to their played-as type; otherwise return the card's own type.
        /// </summary>
        private static CardType GetEffectiveType(Card card)
        {
            if (card is TigressCard tigress)
                return tigress.PlayedAsType;
            return card.CardType;
        }

    }
}
