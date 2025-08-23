using SkullKingCore.Core.Cards.Base;
using System.Runtime.Serialization;

namespace SkullKingCore.Core.Game
{
    [DataContract]
    public class GameState
    {

        // Parameterless ctor for serializer
        private GameState() { }

        public GameState(List<Player> players, int startRound, int maxRounds, List<Card> deck)
        {
            Players = players;
            CurrentRound = startRound;
            MaxRounds = maxRounds;
            AllGameCards = deck;
        }

        [DataMember(Order = 1)] public int CurrentRound { get; set; }
        [DataMember(Order = 2)] public int CurrentSubRound { get; set; }//1
        [DataMember(Order = 3)] public int MaxRounds { get; set; }
        [DataMember(Order = 4)] public int StartingPlayerIndex { get; set; }//0

        [DataMember(Order = 5)]
        public List<Player> Players { get; set; } = new();

        //[IgnoreDataMember]

        [DataMember(Order = 6)]
        public IReadOnlyList<Card> AllGameCards { get; set; } = new List<Card>();

        [DataMember(Order = 7)]
        public List<Card> ShuffledGameCards { get; set; } = new();

    }
}
