using SkullKingCore.Core.Cards;
using SkullKingCore.Core.Cards.Base;

namespace SkullKingCore.Core.Game
{
    /// <summary>
    /// Tracks Loot alliances during a round and gives +20 to BOTH allied players
    /// at the end of the round IF they both hit their bids exactly.
    ///
    /// Rules we follow:
    /// - If a player plays a LOOT card into a trick, they ally with the player who won that trick.
    /// - If the LOOT player themselves wins the trick (the "all escapes" Captain’s Log case),
    ///   NO alliance is formed for that LOOT card.
    /// - If the trick is cancelled (Kraken or White Whale with only specials), NO alliances are formed.
    /// - Multiple LOOT cards in a single trick create multiple alliances with the same winner.
    /// - We store absolute player indexes (0..N-1).
    /// </summary>
    public sealed class LootAllianceTracker
    {
        private sealed class LootAlliance
        {
            public int LootPlayerAbsIndex;
            public int WinnerPlayerAbsIndex;
            public LootAlliance(int lootAbs, int winnerAbs)
            {
                LootPlayerAbsIndex = lootAbs;
                WinnerPlayerAbsIndex = winnerAbs;
            }
        }

        private readonly List<LootAlliance> _alliances = new List<LootAlliance>();

        /// <summary>
        /// Forget everything from previous rounds. Call at the START of each round.
        /// </summary>
        public void Reset()
        {
            _alliances.Clear();
        }

        /// <summary>
        /// Call ONCE after each trick resolves.
        /// </summary>
        /// <param name="trick">Cards in the order they were played (lead first).</param>
        /// <param name="winnerRelIndex">Index of the winner relative to the trick order, or null if cancelled.</param>
        /// <param name="startingPlayerAbsIndex">
        /// Absolute seat index (0..N-1) of the player who LED this trick (the first to play).
        /// Used to translate trick-relative positions into absolute player indices.
        /// </param>
        /// <param name="playerCount">Total number of players in the game.</param>
        public void ObserveTrick(
            IList<Card> trick,
            int? winnerRelIndex,
            int startingPlayerAbsIndex,
            int playerCount)
        {
            if (trick == null) return;
            if (trick.Count == 0) return;
            if (!winnerRelIndex.HasValue) return; // cancelled trick → no alliances

            // Convert winner’s relative position in the trick to their absolute seat index.
            int winnerAbsIndex = ToAbsoluteIndex(startingPlayerAbsIndex, winnerRelIndex.Value, playerCount);

            // Check each played card; if it's LOOT, form alliance with the trick winner,
            // except when the LOOT player IS the winner (captain's log: no alliance).
            for (int rel = 0; rel < trick.Count; rel++)
            {
                Card card = trick[rel];
                if (card.CardType == CardType.LOOT)
                {
                    int lootAbsIndex = ToAbsoluteIndex(startingPlayerAbsIndex, rel, playerCount);

                    if (lootAbsIndex != winnerAbsIndex)
                    {
                        _alliances.Add(new LootAlliance(lootAbsIndex, winnerAbsIndex));
                    }
                }
            }
        }

        /// <summary>
        /// Award the +20 LOOT bonuses into RoundStat.BonusPoints.
        /// Call this ONCE at the END of the round, after all tricks are done.
        /// </summary>
        /// <param name="state">Game state containing players and their RoundStats.</param>
        /// <param name="round">The round number we just finished.</param>
        public void ApplyEndOfRoundBonuses(GameState state, int round)
        {
            if (state == null) return;

            foreach (LootAlliance a in _alliances)
            {

                RoundStat? lootPlayerStat = FindRoundStat(state.Players[a.LootPlayerAbsIndex].RoundStats, round);
                RoundStat? winnerPlayerStat = FindRoundStat(state.Players[a.WinnerPlayerAbsIndex].RoundStats, round);

                if (lootPlayerStat == null) continue;
                if (winnerPlayerStat == null) continue;

                bool lootPlayerHitBid = lootPlayerStat.PredictedWins == lootPlayerStat.ActualWins;
                bool winnerPlayerHitBid = winnerPlayerStat.PredictedWins == winnerPlayerStat.ActualWins;

                if (lootPlayerHitBid && winnerPlayerHitBid)
                {
                    lootPlayerStat.BonusPoints += 20;
                    winnerPlayerStat.BonusPoints += 20;
                }

            }
        }

        private static int ToAbsoluteIndex(int startAbs, int relative, int playerCount)
        {
            // Convert relative position within a trick to the player's absolute seat index.
            // Example: startAbs=2, relative=1, playerCount=5 → (2+1)%5 = 3.
            int v = startAbs + relative;
            v = v % playerCount;
            if (v < 0) v += playerCount;
            return v;
        }

        private static RoundStat? FindRoundStat(List<RoundStat> list, int round)
        {
            if (list == null) return null;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Round == round) return list[i];
            }
            return null;
        }

    }
}
