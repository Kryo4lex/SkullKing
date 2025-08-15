using SkullKingCore.Cards.Base;
using SkullKingCore.Cards.Implementations;
using SkullKingCore.Cards.Interfaces;
using SkullKingCore.GameDefinitions;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SkullKingCore.Core
{
    /// <summary>
    /// Resolves the winner of a trick in the game "Skull King"
    /// according to the official card interaction rules.
    /// </summary>
    public static class TrickResolver
    {

        /// <summary>
        /// Returns the index of the winning card in the played list.
        /// Applies full Skull King card resolution rules:
        /// Kraken, Whale, Pirate, Skull King, Mermaid, Escapes, Trump, and Lead following.
        /// </summary>
        /// <param name="cardsPlayed">Cards in play order.</param>
        /// <returns>Index of winner, or null if no winner (Kraken cancel).</returns>
        public static int? DetermineTrickWinnerIndex(List<Card> cardsPlayed)
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
            var leadCard = typedCards.FirstOrDefault(x => Card.IsNumberCard(x.Type))?.Card ?? cardsPlayed[0];
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
            return ResolveNumberTrick(typedCards, leadType);
        }

        /// <summary>
        /// Returns the winning card from the given list of played cards.
        /// </summary>
        /// <param name="cardsPlayed">Cards in the order they were played.</param>
        /// <returns>The winning card, or null if the trick is cancelled (e.g., by Kraken).</returns>
        public static Card? DetermineTrickWinnerCard(List<Card> cardsPlayed)
        {
            var indexOfWinningCard = DetermineTrickWinnerIndex(cardsPlayed);
            return indexOfWinningCard.HasValue ? cardsPlayed[indexOfWinningCard.Value] : null;
        }

        /// <summary>
        /// Returns the index (in original play order) of the card that
        /// <b>would have won the trick if no KRAKEN or WHITE WHALE discard effect had occurred</b>.
        ///
        /// Used to determine who leads the next trick after a trick-cancelling situation:
        ///
        /// Kraken — “The next trick is led by the player who would have won the trick.”
        /// — Remove all KRAKEN cards and resolve the remainder as if they were never played.
        ///
        /// White Whale — “If only special cards were played, then the trick is discarded (like the Kraken)
        /// and the person who played the White Whale is the next to lead.”
        /// — After removing Krakens, if the remaining trick contains only special cards and includes a White Whale,
        /// return the (first) White Whale player’s index.
        /// 
        /// If, after removing Krakens, the remaining trick contains only specials but no White Whale,
        /// resolve that trick normally (e.g., Skull King beats pirates and escapes; all Escapes → first Escape).
        /// </summary>
        /// <param name="cardsPlayed">Cards in the order they were played.</param>
        /// <returns>The index in <paramref name="cardsPlayed"/> of the player who would lead next under these rules.</returns>
        /// <exception cref="ArgumentException">If cardsPlayed is null or empty.</exception>
        /// <exception cref="InvalidOperationException">If only Krakens were played, or if resolution unexpectedly fails.</exception>
        public static int DetermineTrickWinnerIndexNoSpecialCards(List<Card> cardsPlayed)
        {
            if (cardsPlayed == null || cardsPlayed.Count == 0)
                throw new ArgumentException("cardsPlayed must contain at least one card.", nameof(cardsPlayed));

            // Remove all Krakens; we want “what would have happened without them”.
            var indexed = cardsPlayed
                .Select((card, index) => new { Card = card, OriginalIndex = index })
                .ToList();

            var noKraken = indexed.Where(x => x.Card.CardType != CardType.KRAKEN).ToList();
            if (noKraken.Count == 0)
                throw new InvalidOperationException("Only Kraken(s) were played; cannot determine a would-have-winner.");

            bool anyNumber = noKraken.Any(x => Card.IsNumberCard(x.Card.CardType));
            if (!anyNumber)
            {
                // Only specials remain after removing Krakens.
                var whale = noKraken.FirstOrDefault(x => x.Card.CardType == CardType.WHITE_WHALE);
                if (whale != null)
                {
                    // Whale + only specials ⇒ Whale player leads next.
                    return whale.OriginalIndex;
                }

                // No Whale: resolve the special-only trick normally.
                int? idxSpecials = DetermineTrickWinnerIndex(noKraken.Select(x => x.Card).ToList());
                if (idxSpecials == null)
                    throw new InvalidOperationException("Unable to resolve winner with only special cards after removing Krakens.");

                return noKraken[idxSpecials.Value].OriginalIndex;
            }

            // Numbers present: resolve normally without the Kraken(s).
            int? idx = DetermineTrickWinnerIndex(noKraken.Select(x => x.Card).ToList());
            if (idx == null)
                throw new InvalidOperationException("Unable to determine a winner after removing Krakens.");

            return noKraken[idx.Value].OriginalIndex;
        }

        /// <summary>
        /// Resolves trick when White Whale is played:
        /// Only number cards matter, regardless of color or lead suit.
        /// </summary>
        private static int? ResolveWhiteWhaleTrick(List<CardInfo> cards)
        {
            var candidates = cards.Where(x => Card.IsNumberCard(x.Type)).ToList();
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
        private static int? ResolveNumberTrick(List<CardInfo> cards, CardType leadType)
        {
            var numberCards = cards.Where(x => Card.IsNumberCard(x.Type)).ToList();
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
        /// Computes the subset of <paramref name="playerHand"/> that is legal to play
        /// given the current trick, enforcing follow-suit for number cards ONLY when the
        /// trick so far contains numbers and no special cards other than Escapes.
        /// 
        /// Official-rule alignment:
        /// - Lead color = suit of the first number card played (BLACK counts as a suit).
        /// - Non-number cards (e.g., Escape, Pirate, Skull King, Mermaid, Tigress-as-escape/pirate,
        ///   White Whale, Kraken) are always legal to play, regardless of lead color.
        /// - Follow-suit for number cards is required only when:
        ///     * at least one number card has been played in the trick, AND
        ///     * no non-Escape special has been played (meaning no Pirate, Mermaid, Skull King,
        ///       Tigress-as-Pirate, White Whale, or Kraken has appeared yet).
        ///   Examples:
        ///     - First card is Escape, later a Yellow number appears, and no Pirate/Mermaid/SK/Whale/Kraken
        ///       has been played → number cards must follow Yellow; specials still always legal.
        ///     - If any non-Escape special (Pirate, Mermaid, Skull King, White Whale, Kraken, etc.)
        ///       has been played at any point in the trick → no follow-suit requirement for number cards
        ///       for the remainder of that trick.
        /// - If no number card has been played yet, all cards are legal.
        /// 
        /// Notes:
        /// - Tigress is never a number card; as Escape or Pirate, it is always legal to play.
        /// - White Whale and Kraken both cancel suit-following immediately once played.  
        ///   * White Whale changes resolution rules and removes color relevance.  
        ///   * Kraken destroys the trick instantly, so there is never a suit to follow.
        /// - The returned list is a filtered view (new list of references) — it does not clone cards.
        /// - Edge cases: empty hand -> empty result; one-card hand -> that single card is returned as-is.
        /// </summary>
        public static List<Card> GetAllowedCardsToPlay(List<Card> currentTrick, List<Card> playerHand)
        {
            if (playerHand == null || playerHand.Count == 0)
                return new List<Card>();

            // If the player only has one card, they must play it — return as-is.
            if (playerHand.Count == 1)
                return playerHand;

            // Helper local funcs using effective types (so Tigress etc. are interpreted correctly).
            bool IsNumber(Card c) => Card.IsNumberCard(GetEffectiveType(c));
            bool IsNonEscapeSpecial(Card c)
            {
                var t = GetEffectiveType(c);
                return !Card.IsNumberCard(t) && t != CardType.ESCAPE;
            }

            // Determine if any number has been played in the trick so far.
            bool anyNumberPlayed = currentTrick != null && currentTrick.Any(IsNumber);

            // If no number yet → anything goes (including all specials).
            if (!anyNumberPlayed)
                return new List<Card>(playerHand);

            // If any non-Escape special has been played (e.g., Pirate, Mermaid, Skull King, White Whale, Kraken),
            // then follow-suit for number cards is NOT enforced for the rest of this trick.
            bool anyNonEscapeSpecialPlayed = currentTrick!.Any(IsNonEscapeSpecial);
            if (anyNonEscapeSpecialPlayed)
                return new List<Card>(playerHand);

            // At this point: numbers have been played AND the only specials seen so far are Escapes.
            // → Enforce follow-suit for number cards.
            // Lead suit is the suit of the first number card played.
            var leadNumberCard = currentTrick!.First(c => IsNumber(c));
            var leadSuit = GetEffectiveType(leadNumberCard);

            // Specials (non-number) are ALWAYS legal (e.g., Escape, Tigress-as-escape/pirate, etc.).
            var specialsAlwaysAllowed = playerHand.Where(c => !IsNumber(c));

            // Number cards in hand.
            var numberCards = playerHand.Where(IsNumber);

            // Among number cards, must follow lead suit if possible.
            var numberCardsOfLeadSuit = numberCards.Where(c => GetEffectiveType(c) == leadSuit);

            var legalNumbers = numberCardsOfLeadSuit.Any()
                ? numberCardsOfLeadSuit
                : numberCards;

            // Combine: specials (always) + legal number options.
            return specialsAlwaysAllowed.Concat(legalNumbers).ToList();
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
        private static CardType GetEffectiveType(Card card)
        {
            if (card is TigressCard tigress)
                return tigress.PlayedAsType;

            return card.CardType;
        }

        /// <summary>
        /// Lightweight record holding card, its play order index, and resolved type.
        /// </summary>
        private record CardInfo(Card Card, int Index, CardType Type);

    }

}
