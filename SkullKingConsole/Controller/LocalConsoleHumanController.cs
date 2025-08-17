using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.Implementations;
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

        public Task NotifyGameStartedAsync(GameState state)
        {
            Console.Clear();

            Logger.Instance.WriteToConsoleAndLog($"--- Game started ---");

            return Task.CompletedTask;
        }

        public Task<Card> RequestCardPlayAsync(GameState state, List<Card> hand, TimeSpan maxWait)
        {

            int cardToPlayIndex = 0;

            Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}Cards you can play:");

            Card.PrintListFancy(hand);

            while(!UserInput.TryReadInt($"{Environment.NewLine}Enter the index of the card you want to play:", out cardToPlayIndex, 0, hand.Count - 1))
            {

            }

            Card card = hand[cardToPlayIndex];

            //Special Case
            if (card.CardType == CardType.TIGRESS)
            {

                string? choice;
                do
                {
                    Logger.Instance.WriteToConsoleAndLog("Enter E (Escape) or P (Pirate) and confirm: ");
                    choice = Console.ReadLine()?.Trim().ToUpperInvariant();
                }
                while (choice != "E" && choice != "P");

                switch(choice)
                {
                    case "E":
                        ((TigressCard)card).PlayedAsType = CardType.ESCAPE;
                        break;
                    case "P":
                        ((TigressCard)card).PlayedAsType = CardType.PIRATE;
                        break;
                }

            }

            Logger.Instance.WriteToConsoleAndLog($"{Name} plays {card}");

            return Task.FromResult(card);

        }

        public Task NotifyNotAllCardsInHandCanBePlayed(GameState gameState, List<Card> cardsThatPlayerIsAllowedToPlay, List<Card> cardsThatPlayerIsNotAllowedToPlay)
        {
            Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}Not all Cards in your hand can be played due to lead color/suit rule. Cards you are not allowed to play:");

            Card.PrintListFancy(cardsThatPlayerIsNotAllowedToPlay);

            return Task.CompletedTask;
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
                Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}Your cards:");
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

            Logger.Instance.WriteToConsoleAndLog($"");

            return Task.FromResult(bid);

        }

        public Task AnnounceBidAsync(GameState gameState, Player player, int bid, TimeSpan maxWait)
        {
            Logger.Instance.WriteToConsoleAndLog($"{player.Name} bids {bid}");

            return Task.CompletedTask;
        }

        public Task WaitForBidsReceivedAsync(GameState gameState)
        {
            Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}Press Enter to confirm, that you have acknowledged the Bids.");

            Console.ReadLine();

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
