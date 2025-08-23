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

        [DataContract]
        public readonly record struct Round([property: DataMember(Order = 1)] int Value);

        [DataContract]
        public readonly record struct PredictedWins([property: DataMember(Order = 1)] int Value);

        // DataContractSerializer (XML) supports non-string dict keys out of the box
        [DataMember(Order = 4)]
        public Dictionary<Round, PredictedWins> Bids { get; private set; } = new();

    }
}
