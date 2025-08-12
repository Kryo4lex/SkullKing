using SkullKingCore.Cards.Base;
using SkullKingCore.Cards.Implementations;
using SkullKingCore.GameDefinitions;

namespace SkullKingCore.Core
{
    /// <summary>
    /// Resolves the winner of a trick in the game "Skull King"
    /// according to the official card interaction rules.
    /// </summary>
    public static class TrickResolver
    {
        /// <summary>
        /// Returns the winning card from the given list of played cards.
        /// </summary>
        /// <param name="cardsPlayed">Cards in the order they were played.</param>
        /// <returns>The winning card, or null if the trick is cancelled (e.g., by Kraken).</returns>
        public static BaseCard? DetermineTrickWinnerCard(List<BaseCard> cardsPlayed)
        {
            var indexOfWinningCard = DetermineTrickWinnerIndex(cardsPlayed);
            return indexOfWinningCard.HasValue ? cardsPlayed[indexOfWinningCard.Value] : null;
        }

        /// <summary>
        /// Returns the index of the winning card in the played list.
        /// Applies full Skull King card resolution rules:
        /// Kraken, Whale, Pirate, Skull King, Mermaid, Escapes, Trump, and Lead following.
        /// </summary>
        /// <param name="cardsPlayed">Cards in play order.</param>
        /// <returns>Index of winner, or null if no winner (Kraken cancel).</returns>
        public static int? DetermineTrickWinnerIndex(List<BaseCard> cardsPlayed)
        {
            if (cardsPlayed == null || cardsPlayed.Count == 0)
            {
                return null;
            }

            // Precompute each card’s index, type, and value for efficient lookups
            var typedCards = cardsPlayed
                .Select((card, idx) => new CardInfo(card, idx, GetEffectiveType(card)))
                .ToList();

            // Lead card = first number card, or first card if none are numbers
            var leadCard = typedCards.FirstOrDefault(x => BaseCard.IsNumberCard(x.Type))?.Card ?? cardsPlayed[0];
            var leadType = GetEffectiveType(leadCard);
            bool leadIsBlack = leadType == CardType.BLACK;

            // --- Step 1: Handle Kraken & White Whale ---
            var krakenIndexes = typedCards.Where(x => x.Type == CardType.KRAKEN).Select(x => x.Index).ToList();
            var whaleIndexes = typedCards.Where(x => x.Type == CardType.WHITE_WHALE).Select(x => x.Index).ToList();

            if (krakenIndexes.Any() && whaleIndexes.Any())
            {
                // Whichever appears later cancels/overrides
                return krakenIndexes.Max() > whaleIndexes.Max()
                    ? null // Latest Kraken cancels the trick entirely
                    : ResolveWhiteWhaleTrick(typedCards);
            }

            if (krakenIndexes.Any())
            {
                return null;// Kraken alone cancels trick
            }

            if (whaleIndexes.Any())
            {
                return ResolveWhiteWhaleTrick(typedCards);
            }

            // --- Step 2: Handle special card hierarchy ---
            bool hasPirate = typedCards.Any(x => x.Type == CardType.PIRATE);
            bool hasSkullKing = typedCards.Any(x => x.Type == CardType.SKULL_KING);
            bool hasMermaid = typedCards.Any(x => x.Type == CardType.MERMAID);

            // Mermaid beats Skull King when Pirate is also present (special combo)
            if (HasMermaidWinsCombo(hasPirate, hasSkullKing, hasMermaid))
                return FirstIndexOfType(typedCards, CardType.MERMAID);

            // Mermaid wins if no Pirate (Kraken/Whale already handled above)
            if (hasMermaid && !hasPirate)
                return FirstIndexOfType(typedCards, CardType.MERMAID);

            // Skull King beats Pirates and Numbers, but loses to Mermaid
            if (hasSkullKing && !hasMermaid)
                return FirstIndexOfType(typedCards, CardType.SKULL_KING);

            // Pirate beats Mermaid if no Skull King present
            if (hasPirate && hasMermaid && !hasSkullKing)
                return FirstIndexOfType(typedCards, CardType.PIRATE);

            // Pirate beats Numbers and Escapes if no Skull King or Mermaid present
            if (hasPirate && !hasSkullKing && !hasMermaid)
                return FirstIndexOfType(typedCards, CardType.PIRATE);

            // --- Step 3: All Escapes case ---
            if (typedCards.All(x => x.Type == CardType.ESCAPE))
                return 0; // First Escape wins if everyone escaped

            // --- Step 4: Standard number card resolution ---
            return ResolveNumberTrick(typedCards, leadType, leadIsBlack);
        }

        /// <summary>
        /// Resolves trick when White Whale is played:
        /// Only number cards matter, regardless of color or lead suit.
        /// </summary>
        private static int? ResolveWhiteWhaleTrick(List<CardInfo> cards)
        {
            var candidates = cards.Where(x => BaseCard.IsNumberCard(x.Type)).ToList();
            if (!candidates.Any()) return null;

            return candidates
                .OrderByDescending(x => x.Card.GenericValue ?? 0)
                .ThenBy(x => x.Index)
                .First().Index;
        }

        /// <summary>
        /// Resolves a normal number-based trick:
        /// - Black is trump over all colors.
        /// - Otherwise, follow lead suit if possible.
        /// - Highest number wins, earliest played wins ties.
        /// </summary>
        private static int? ResolveNumberTrick(List<CardInfo> cards, CardType leadType, bool leadIsBlack)
        {
            var numberCards = cards.Where(x => BaseCard.IsNumberCard(x.Type)).ToList();
            if (!numberCards.Any()) return null;

            // Trump rule: any Black card beats other colors
            var blackCards = numberCards.Where(x => x.Type == CardType.BLACK).ToList();
            if (blackCards.Any())
            {
                return blackCards
                    .OrderByDescending(x => x.Card.GenericValue ?? 0)
                    .ThenBy(x => x.Index)
                    .First().Index;
            }

            // Follow lead suit if possible
            var sameSuit = numberCards.Where(x => x.Type == leadType).ToList();
            var candidates = sameSuit.Any() ? sameSuit : numberCards;

            return candidates
                .OrderByDescending(x => x.Card.GenericValue ?? 0)
                .ThenBy(x => x.Index)
                .First().Index;
        }

        /// <summary>
        /// Mermaid wins combo: Pirate + Skull King + Mermaid present.
        /// </summary>
        private static bool HasMermaidWinsCombo(bool hasPirate, bool hasSkullKing, bool hasMermaid) =>
            hasPirate && hasSkullKing && hasMermaid;

        /// <summary>
        /// Returns the earliest played index of a given card type.
        /// </summary>
        private static int FirstIndexOfType(List<CardInfo> cards, CardType type) =>
            cards.Where(x => x.Type == type)
                 .OrderBy(x => x.Index)
                 .First().Index;

        /// <summary>
        /// Gets the "effective" type of a card.
        /// For example, Tigress may count as Pirate or Escape depending on play choice.
        /// </summary>
        private static CardType GetEffectiveType(BaseCard card)
        {
            if (card is TigressCard tigress)
                return tigress.PlayedAsType;

            return card.CardType;
        }

        /// <summary>
        /// Lightweight record holding card, its play order index, and resolved type.
        /// </summary>
        private record CardInfo(BaseCard Card, int Index, CardType Type);
    }

}
