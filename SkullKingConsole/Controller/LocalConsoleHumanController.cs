using SkullKingCore.Cards.Base;
using SkullKingCore.Cards.Implementations;
using SkullKingCore.Core.Game;
using SkullKingCore.Core.Game.Interfaces;
using SkullKingCore.GameDefinitions;
using SkullKingCore.Logging;
using SkullKingCore.Utility.UserInput;

namespace SkullKingConsole.Controller
{

    public class LocalConsoleHumanController : IGameController
    {

        private readonly Random _random = new();

        public string Name { get; }

        public LocalConsoleHumanController(string name)
        {
            Name = name;
        }

        public Task<Card> RequestCardPlayAsync(GameState state, List<Card> hand, TimeSpan maxWait)
        {

            int cardToPlayIndex = 0;

            Card.PrintListFancy(hand);

            while(!UserInput.TryReadInt($"{Environment.NewLine}Enter the index of the card you want to play:", out cardToPlayIndex, 0, hand.Count - 1))
            {

            }

            Card card = hand[cardToPlayIndex];

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

            hand.Remove(card);

            return Task.FromResult(card);

        }

        public Task ShowMessageAsync(string message)
        {

            Logger.Instance.WriteToConsoleAndLog($"{Name} {message}");

            return Task.CompletedTask;

        }

        public Task<int> RequestBidAsync(GameState gameState, int roundNumber, TimeSpan maxWait)
        {

            var player = gameState.Players.FirstOrDefault(p => p.Name == Name);

            if (player != null)
            {
                Logger.Instance.WriteToConsoleAndLog($"Your cards:");
                Card.PrintListFancy(player.Hand);
            }
            else
            {
                Logger.Instance.WriteToConsoleAndLog($"Player with name '{Name}' not found.");
            }

            int bid = 0;

            while (!UserInput.TryReadInt($"{Environment.NewLine}Enter your number of wins prediction:", out bid, 0, roundNumber))
            {

            }

            Logger.Instance.WriteToConsoleAndLog($"{Name} bids {bid}");

            return Task.FromResult(bid);

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

            if(player == null)
            {
                Logger.Instance.WriteToConsoleAndLog($"None!");
            }
            else
            {
                Logger.Instance.WriteToConsoleAndLog($"{player.Name} won round {round} with {winningCard}");
            }

            return Task.CompletedTask;
        }

        public Task NotifyAboutSubRoundStartAsync(GameState state)
        {
            Console.Clear();
            Logger.Instance.WriteToConsoleAndLog($"--- Sub round {state.CurrentSubRound}/{state.MaxRounds} started ---");

            return Task.CompletedTask;
        }

        public Task NotifyAboutSubRoundEndAsync(GameState state)
        {
            //no need to tell console CPU what is happening
            Logger.Instance.WriteToConsoleAndLog($"--- Sub round {state.CurrentSubRound}/{state.MaxRounds} ended ---");
            Logger.Instance.WriteToConsoleAndLog($"Press Any Key to continue");
            Console.ReadLine();

            return Task.CompletedTask;
        }

        public Task NotifyBidCollectionStartedAsync(GameState gameState)
        {
            Logger.Instance.WriteToConsoleAndLog("Collecting bids...");

            return Task.CompletedTask;
        }

        public Task NotifyRoundStartedAsync(GameState gameState)
        {
            Logger.Instance.WriteToConsoleAndLog($"--- Round {gameState.CurrentRound} ---");

            return Task.CompletedTask;
        }

        public Task NotifyGameEndedAsync(GameState gameState)
        {
            Logger.Instance.WriteToConsoleAndLog($"--- Game finished ---");

            return Task.CompletedTask;
        }

        public Task NotifyAboutGameWinnerAsync(GameState gameState, List<Player> winners)
        {

            return Task.CompletedTask;
        }

    }
}
