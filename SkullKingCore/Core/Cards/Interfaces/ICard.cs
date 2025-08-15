using SkullKingCore.GameDefinitions;

namespace SkullKingCore.Core.Cards.Interfaces
{
    public interface ICard
    {

        string ToString();

        CardType CardType { get; }

        public int? GenericValue { get; }

    }
}
