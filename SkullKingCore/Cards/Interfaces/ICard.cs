using SkullKingCore.GameDefinitions;

namespace SkullKingCore.Cards.Interfaces
{
    public interface ICard
    {

        string ToString();

        CardType CardType { get; }

        public int? GenericValue { get; }

    }
}
