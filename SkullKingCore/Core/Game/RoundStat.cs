using System.Runtime.Serialization;

namespace SkullKingCore.Core.Game
{

    [DataContract]
    public class RoundStat
    {

        // Parameterless ctor for serializer
        public RoundStat() { }

        public RoundStat(int round, int predictedWins)
        {
            PredictedWins = predictedWins;
            Round = round;
        }

        [DataMember(Order = 1)] public int Round { get; set; }

        [DataMember(Order = 2)] public int PredictedWins { get; set; }

        [DataMember(Order = 3)] public int ActualWins { get; set; } = 0;

        [DataMember(Order = 4)] public int BonusPoints { get; set; } = 0;

    }
}
