namespace SkullKingCore.Core.Game.Scoring.Interfaces
{
    public interface IScoringSystem
    {
        /// <summary>
        /// Computes the total score for a player across all their recorded rounds.
        /// </summary>
        int ComputeTotalScore(Player player);

        /// <summary>
        /// Computes the score for a single round.
        /// </summary>
        int ComputeRoundScore(RoundStat roundStat);
    }
}
