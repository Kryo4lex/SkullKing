namespace SkullKingCore.Core.Game.Scoring
{
    public static class SkullKingScoring
    {
        /// <summary>
        /// Computes total score for a player across all recorded RoundStats
        /// using standard Skull King scoring. BonusPoints count only on exact bids.
        /// </summary>
        public static int ComputeTotalScore(Player player)
            => player?.RoundStats?.Sum(ComputeRoundScore) ?? 0;

        /// <summary>
        /// Computes the score for a single round.
        /// Rules:
        /// - Non-zero bid:
        ///     * exact => +20 per trick taken
        ///     * wrong => -10 per trick off (|actual - bid|)
        /// - Zero bid:
        ///     * exact (ActualWins == 0) => +10 * Round
        ///     * wrong (ActualWins > 0)  => -10 * Round
        /// - BonusPoints apply only if bid is exact.
        /// </summary>
        public static int ComputeRoundScore(RoundStat rs)
        {
            if (rs == null) return 0;

            int basePoints;
            bool exact = rs.ActualWins == rs.PredictedWins;

            if (rs.PredictedWins == 0)
            {
                basePoints = (rs.ActualWins == 0)
                    ? 10 * rs.Round
                    : -10 * rs.Round;
            }
            else
            {
                if (exact)
                    basePoints = 20 * rs.ActualWins;
                else
                    basePoints = -10 * Math.Abs(rs.ActualWins - rs.PredictedWins);
            }

            // Bonus applies only on exact bids
            int bonus = exact ? rs.BonusPoints : 0;

            return basePoints + bonus;
        }
    }
}
