using SkullKingCore.Cards.Base;

namespace SkullKingCore.Core
{
    public class Player
    {
        public string Name { get; private set; }

        public List<Card> CurrentCards { get; set; } = new List<Card>();

        public Player(string name)
        {
           Name = name; 
        }
    }
}
