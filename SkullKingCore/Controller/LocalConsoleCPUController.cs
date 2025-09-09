using SkullKingCore.Core.Cards;
using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.Implementations;
using SkullKingCore.Core.Game;
using SkullKingCore.Core.Game.Interfaces;

namespace SkullKingCore.Controller
{

    public class LocalConsoleCPUController : IGameController
    {

        private readonly Random _random = new();

        private readonly TimeSpan ArtificalDelay = TimeSpan.FromSeconds(0);

        public string Name { get; }

        public GameState? GameState { get; set; }

        public LocalConsoleCPUController(string name)
        {
            Name = name;
        }

        public Task<string> RequestName(GameState gameState, TimeSpan maxWait)
        {
            Task.Delay(ArtificalDelay).Wait();

            return Task.FromResult(Name);
        }

        public Task NotifyGameStartedAsync(GameState gameState)
        {
            Task.Delay(ArtificalDelay).Wait();

            return Task.CompletedTask;
        }

        public Task<Card> RequestCardPlayAsync(GameState gameState, List<Card> hand, TimeSpan maxWait)
        {

            Card? card = hand[_random.Next(hand.Count)];

            //Special Case
            if (card.CardType == CardType.TIGRESS)
            {

                List<CardType> availableOptions = new List<CardType>()
                {
                    CardType.ESCAPE,
                    CardType.PIRATE,
                };

                ((TigressCard)card).PlayedAsType = availableOptions[_random.Next(availableOptions.Count)];

            }

            //Logger.Instance.WriteToConsoleAndLog($"{Name} plays {card}");

            Task.Delay(ArtificalDelay).Wait();

            return Task.FromResult(card);

        }

        public Task ShowMessageAsync(string message)
        {

            //Logger.Instance.WriteToConsoleAndLog($"{Name} {message}");

            Task.Delay(ArtificalDelay).Wait();

            return Task.CompletedTask;

        }

        public Task<int> RequestBidAsync(GameState gameState, int roundNumber, TimeSpan maxWait)
        {

            int bid = _random.Next(0, roundNumber + 1);

            Task.Delay(ArtificalDelay).Wait();

            return Task.FromResult(bid);

        }

        public Task AnnounceBidAsync(GameState gameState, TimeSpan maxWait)
        {
            //Logger.Instance.WriteToConsoleAndLog($"{player.Name} bids {bid}");

            Task.Delay(ArtificalDelay).Wait();

            return Task.CompletedTask;
        }

        public Task WaitForBidsReceivedAsync(GameState gameState)
        {

            Task.Delay(ArtificalDelay).Wait();

            return Task.CompletedTask;
        }

        public Task NotifyCardPlayedAsync(GameState gameState, Player player, Card playedCard)
        {
            /*
            string opponentPlayerName = gameState.Players.FirstOrDefault(x => x.Id == playerID).Name;

            Logger.Instance.WriteToConsoleAndLog($"{Name} played {playedCard}");
            */

            Task.Delay(ArtificalDelay).Wait();

            return Task.CompletedTask;
        }

        public Task NotifyAboutSubRoundWinnerAsync(GameState gameState,Player? winner, Card? winningCard)
        {
            //no need to tell console CPU what is happening
            /*
            if(player == null)
            {
                Logger.Instance.WriteToConsoleAndLog($"None!");
            }
            else
            {
                Logger.Instance.WriteToConsoleAndLog($"{player.Name} won round {round} with {winningCard}");
            }
            */

            Task.Delay(ArtificalDelay).Wait();

            return Task.CompletedTask;
        }

        public Task NotifyAboutSubRoundStartAsync(GameState gameState)
        {
            //no need to tell console CPU what is happening
            //Logger.Instance.WriteToConsoleAndLog($"Sub round {state.CurrentSubRound}/{state.MaxRounds} started.");

            Task.Delay(ArtificalDelay).Wait();

            return Task.CompletedTask;
        }

        public Task NotifyAboutSubRoundEndAsync(GameState gameState)
        {
            //no need to tell console CPU what is happening
            //Logger.Instance.WriteToConsoleAndLog($"Sub round {state.CurrentSubRound}/{state.MaxRounds} ended.");

            Task.Delay(ArtificalDelay).Wait();

            return Task.CompletedTask;
        }

        public Task NotifyBidCollectionStartedAsync(GameState gameState)
        {
            //no need to tell console CPU what is happening
            //Logger.Instance.WriteToConsoleAndLog("Collecting bids...");

            Task.Delay(ArtificalDelay).Wait();

            return Task.CompletedTask;
        }

        public Task NotifyRoundStartedAsync(GameState gameState)
        {
            //no need to tell console CPU what is happening
            //Logger.Instance.WriteToConsoleAndLog($"--- Round {gameState.CurrentRound} ---");

            Task.Delay(ArtificalDelay).Wait();

            return Task.CompletedTask;
        }

        public Task NotifyGameEndedAsync(GameState gameState)
        {
            //no need to tell console CPU what is happening
            //Logger.Instance.WriteToConsoleAndLog($"--- Game finished ---");

            Task.Delay(ArtificalDelay).Wait();

            return Task.CompletedTask;
        }

        public Task NotifyAboutGameWinnerAsync(GameState gameState, List<Player> winners)
        {

            Task.Delay(ArtificalDelay).Wait();

            return Task.CompletedTask;
        }

        public Task NotifyNotAllCardsInHandCanBePlayed(GameState gameState, List<Card> cardsThatPlayerIsAllowedToPlay, List<Card> cardsThatPlayerIsNotAllowedToPlay)
        {

            Task.Delay(ArtificalDelay).Wait();

            return Task.CompletedTask;
        }

        public Task NotifyPlayerTimedOutAsync(GameState gameState, Player player)
        {

            Task.Delay(ArtificalDelay).Wait();

            return Task.CompletedTask;
        }

        public Task NotifyAboutMainRoundEndAsync(GameState gameState)
        {

            Task.Delay(ArtificalDelay).Wait();

            return Task.CompletedTask;
        }
    }
}
