using SkullKingCore.Core.Cards.Base;

namespace SkullKingCore.Core.Game
{
    public class Player
    {
        public string Id { get; }
        public string Name { get; }
        public List<Card> Hand { get; set; } = new();
        public int Score { get; set; }

        // Track bids per round
        public record Round(int Value);
        public record PredictedWins(int Value);

        public Dictionary<Round, PredictedWins> Bids { get; } = new();

        public Player(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
