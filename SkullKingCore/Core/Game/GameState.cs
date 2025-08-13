using SkullKingCore.Cards.Base;

namespace SkullKingCore.Core.Game
{
    public class GameState
    {
        public IReadOnlyList<Player> Players { get; }
        public int CurrentRound { get; internal set; }
        public int StartRound { get; }
        public int MaxRounds { get; }
        public List<Card> AllGameCards { get; }
        public List<Card> ShuffledGameCards { get; internal set; } = new List<Card>();

        public GameState(List<Player> players, int startRound, int maxRounds, List<Card> allGameCards)
        {
            Players = players ?? throw new ArgumentNullException(nameof(players));
            StartRound = startRound;
            CurrentRound = startRound;
            MaxRounds = maxRounds;
            AllGameCards = allGameCards ?? throw new ArgumentNullException(nameof(allGameCards));
        }
    }
}
