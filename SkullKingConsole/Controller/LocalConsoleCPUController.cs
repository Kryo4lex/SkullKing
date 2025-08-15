using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.Implementations;
using SkullKingCore.Core.Game;
using SkullKingCore.Core.Game.Interfaces;
using SkullKingCore.GameDefinitions;
using SkullKingCore.Logging;

namespace SkullKingConsole.Controller
{

    public class LocalConsoleCPUController : IGameController
    {

        private readonly Random _random = new();

        public string Name { get; }

        public LocalConsoleCPUController(string name)
        {
            Name = name;
        }

        public Task NotifyGameStartedAsync(GameState state)
        {

            return Task.CompletedTask;
        }

        public Task<Card> RequestCardPlayAsync(GameState state, List<Card> hand, TimeSpan maxWait)
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

            Logger.Instance.WriteToConsoleAndLog($"{Name} plays {card}");

            return Task.FromResult(card);

        }

        public Task ShowMessageAsync(string message)
        {

            Logger.Instance.WriteToConsoleAndLog($"{Name} {message}");

            return Task.CompletedTask;

        }

        public Task<int> RequestBidAsync(GameState gameState, int roundNumber, TimeSpan maxWait)
        {

            int bid = _random.Next(0, roundNumber + 1);

            return Task.FromResult(bid);

        }

        public Task AnnounceBidAsync(GameState gameState, Player player, int bid, TimeSpan maxWait)
        {
            Logger.Instance.WriteToConsoleAndLog($"{player.Name} bids {bid}");

            return Task.CompletedTask;
        }

        public Task WaitForBidsReceivedAsync(GameState gameState)
        {

            return Task.CompletedTask;
        }

        public Task NotifyCardPlayedAsync(Player player, Card playedCard)
        {
            /*
            string opponentPlayerName = gameState.Players.FirstOrDefault(x => x.Id == playerID).Name;

            Logger.Instance.WriteToConsoleAndLog($"{Name} played {playedCard}");
            */
            return Task.CompletedTask;
        }

        public Task NotifyAboutSubRoundWinnerAsync(Player? player, Card? winningCard, int round)
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
            return Task.CompletedTask;
        }

        public Task NotifyAboutSubRoundStartAsync(GameState state)
        {
            //no need to tell console CPU what is happening
            //Logger.Instance.WriteToConsoleAndLog($"Sub round {state.CurrentSubRound}/{state.MaxRounds} started.");

            return Task.CompletedTask;
        }

        public Task NotifyAboutSubRoundEndAsync(GameState state)
        {
            //no need to tell console CPU what is happening
            //Logger.Instance.WriteToConsoleAndLog($"Sub round {state.CurrentSubRound}/{state.MaxRounds} ended.");

            return Task.CompletedTask;
        }

        public Task NotifyBidCollectionStartedAsync(GameState gameState)
        {
            //no need to tell console CPU what is happening
            //Logger.Instance.WriteToConsoleAndLog("Collecting bids...");

            return Task.CompletedTask;
        }

        public Task NotifyRoundStartedAsync(GameState gameState)
        {
            //no need to tell console CPU what is happening
            //Logger.Instance.WriteToConsoleAndLog($"--- Round {gameState.CurrentRound} ---");

            return Task.CompletedTask;
        }

        public Task NotifyGameEndedAsync(GameState gameState)
        {
            //no need to tell console CPU what is happening
            //Logger.Instance.WriteToConsoleAndLog($"--- Game finished ---");

            return Task.CompletedTask;
        }

        public Task NotifyAboutGameWinnerAsync(GameState gameState, List<Player> winners)
        {

            return Task.CompletedTask;
        }

        public Task NotifyNotAllCardsInHandCanBePlayed(GameState gameState, List<Card> cardsThatPlayerIsAllowedToPlay, List<Card> cardsThatPlayerIsNotAllowedToPlay)
        {
            return Task.CompletedTask;
        }
    }
}
