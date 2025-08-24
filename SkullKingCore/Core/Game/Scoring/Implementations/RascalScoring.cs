using SkullKingCore.Core.Game.Scoring.Interfaces;

namespace SkullKingCore.Core.Game.Scoring.Implementations
{
    /// <summary>
    /// Rascal’s Scoring:
    /// - Potential base points each round = 10 × cards dealt that round (i.e., 10 × Round).
    /// - Direct Hit (exact bid): full potential.
    /// - Glancing Blow (off by 1): half potential.
    /// - Complete Miss (off by 2+): zero.
    ///
    /// Bonuses scale the same way:
    /// - Direct Hit: full BonusPoints
    /// - Glancing Blow: half BonusPoints (integer division)
    /// - Complete Miss: zero BonusPoints
    /// </summary>
    public class RascalScoring : IScoringSystem
    {
        private const int PotentialPerCard = 10;

        public int ComputeTotalScore(Player player)
            => player?.RoundStats?.Sum(ComputeRoundScore) ?? 0;

        public int ComputeRoundScore(RoundStat rs)
        {
            if (rs == null) return 0;

            int roundCards = Math.Max(0, rs.Round);
            int potential = PotentialPerCard * roundCards;
            int diff = Math.Abs(rs.ActualWins - rs.PredictedWins);

            // Base points based on accuracy
            int basePoints = diff switch
            {
                0 => potential,          // Direct Hit
                1 => potential / 2,      // Glancing Blow (integer half)
                _ => 0                   // Complete Miss
            };

            // Bonuses scale exactly the same way
            int scaledBonus = diff switch
            {
                0 => rs.BonusPoints,
                1 => rs.BonusPoints / 2, // integer half; truncates toward zero for negatives
                _ => 0
            };

            return basePoints + scaledBonus;
        }
    }
}
