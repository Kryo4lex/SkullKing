using SkullKingCore.Cards.Base;

namespace SkullKingCore.Core.Game.Interfaces
{

    public interface IGameController
    {

        string Name { get; }

        Task NotifyRoundStartedAsync(GameState gameState);

        Task NotifyBidCollectionStartedAsync(GameState gameState);


        Task<int> RequestBidAsync(GameState gameState, int roundNumber, TimeSpan maxWait);

        Task NotifyNotAllCardsInHandCanBePlayed(GameState gameState, List<Card> cardsThatPlayerIsAllowedToPlay, List<Card> cardsThatPlayerIsNotAllowedToPlay);

        Task<Card> RequestCardPlayAsync(GameState state, List<Card> hand, TimeSpan maxWait);


        Task NotifyCardPlayedAsync(Player player, Card playedCard);

        Task NotifyAboutSubRoundWinnerAsync(Player? player, Card? winningCard, int round);

        Task NotifyAboutSubRoundStartAsync(GameState state);

        Task NotifyAboutSubRoundEndAsync(GameState state);

        Task NotifyGameEndedAsync(GameState gameState);

        Task NotifyAboutGameWinnerAsync(GameState gameState, List<Player> winners);

        Task ShowMessageAsync(string message);

    }

}
