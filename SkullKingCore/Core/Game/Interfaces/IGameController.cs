using SkullKingCore.Cards.Base;
using SkullKingCore.GameDefinitions;

namespace SkullKingCore.Core.Game.Interfaces
{

    public interface IGameController
    {

        string Name { get; }

        Task<int> RequestBidAsync(GameState gameState, int roundNumber, TimeSpan maxWait);

        // Called during a trick to get the player's card choice
        Task<Card> RequestCardPlayAsync(GameState state, List<Card> hand, TimeSpan maxWait);

        Task<CardType> RequestTigressTypeAsync(GameState gameState, TimeSpan maxWait);

        Task ShowMessageAsync(string message);

    }

}
