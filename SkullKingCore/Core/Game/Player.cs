using SkullKingCore.Cards.Base;

namespace SkullKingCore.Core.Game
{
    public class Player
    {
        public string Id { get; }
        public string Name { get; }
        public List<Card> Hand { get; } = new();
        public int Score { get; set; }

        // Track bids per round
        public Dictionary<int, int> Bids { get; } = new();

        public Player(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
