using SkullKingCore.Core.Cards.Base;

namespace SkullKingCore.Core.Game.Interfaces
{
    public interface IGameController
    {
        string Name { get; }


        Task<string> RequestName(GameState gameState, TimeSpan maxWait);
        Task NotifyRoundStartedAsync(GameState gameState);
        Task NotifyBidCollectionStartedAsync(GameState gameState);
        Task WaitForBidsReceivedAsync(GameState gameState);

        Task<int> RequestBidAsync(GameState gameState, int roundNumer, TimeSpan maxWait);
        Task AnnounceBidAsync(GameState gameState, Player player, int bid, TimeSpan maxWait);

        Task NotifyNotAllCardsInHandCanBePlayed(GameState gameState, List<Card> cardsThatPlayerIsAllowedToPlay, List<Card> cardsThatPlayerIsNotAllowedToPlay);
        Task<Card> RequestCardPlayAsync(GameState gameState, List<Card> hand, TimeSpan maxWait);

        Task NotifyCardPlayedAsync(Player player, Card playedCard);
        Task NotifyAboutSubRoundWinnerAsync(Player? player, Card? winningCard, int round);

        Task NotifyGameStartedAsync(GameState gameState);
        Task NotifyAboutSubRoundStartAsync(GameState gameState);
        Task NotifyAboutSubRoundEndAsync(GameState gameState);

        Task NotifyGameEndedAsync(GameState gameState);
        Task NotifyAboutGameWinnerAsync(GameState gameState, List<Player> winners);

        Task ShowMessageAsync(string message);

        Task NotifyPlayerTimedOutAsync(GameState gameState, Player player);
    }
}
