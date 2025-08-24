using SkullKingCore.Core.Cards.Base;
using System.Runtime.Serialization;

namespace SkullKingCore.Core.Game
{
    [DataContract]
    public class Player
    {

        // Parameterless ctor for serializer
        private Player() { }

        public Player(string id, string name)
        {
            Id = id; Name = name;
        }

        [DataMember(Order = 1)] public string Id { get; private set; } = "";
        [DataMember(Order = 2)] public string Name { get; set; } = "";

        [DataMember(Order = 3)]
        public List<Card> Hand { get; set; } = new();

        [DataMember(Order = 4)]
        public List<RoundStat> RoundStats { get; set; } = new();

        [DataMember(Order = 5)]
        public int TotalScore { get; set; } = 0;

    }

}
