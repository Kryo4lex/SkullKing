using SkullKingCore.Cards.Base;

namespace SkullKingCore.Core
{
    public class Player
    {
        public string Name { get; private set; }

        public List<BaseCard> CurrentCards { get; set; } = new List<BaseCard>();

        public Player(string name)
        {
           Name = name; 
        }
    }
}
