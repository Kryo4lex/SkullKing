using SkullKingCore.Core.Game;

namespace SkullKingClientUI.ViewModels
{
    public class RoundStatVM
    {
        public int Round { get; }
        public int Predicted { get; }
        public int Actual { get; }
        public int Bonus { get; }

        public RoundStatVM(RoundStat rs)
        {
            Round = rs.Round;
            Predicted = rs.PredictedWins;
            Actual = rs.ActualWins;
            Bonus = rs.BonusPoints;
        }
    }
}
