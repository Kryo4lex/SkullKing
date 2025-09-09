using SkullKingCore.Core.Cards;
using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.Extensions;
using SkullKingCore.Core.Cards.Implementations;
using SkullKingCore.Core.Game;
using SkullKingCore.Core.Game.Interfaces;
using SkullKingCore.Logging;
using SkullKingCore.Utility;

namespace SkullKingCore.Controller
{
    public class ConsoleHumanController : IGameController
    {

        private const string WaitingForOtherPlayer = "Waiting for other players...";

        public string Name { get; set; } = "NET Player";
        public GameState? GameState { get; set; }

        public Task<string> RequestName(GameState gameState, TimeSpan maxWait)
        {
            Console.Clear();

            Logger.Instance.WriteToConsoleAndLog($"Enter your name:");

            string? name = Console.ReadLine();

            Name = name ?? string.Empty;

            Logger.Instance.WriteToConsoleAndLog(WaitingForOtherPlayer);

            return Task.FromResult(Name);
        }

        public Task NotifyGameStartedAsync(GameState gameState)
        {
            Console.Clear();

            Logger.Instance.WriteToConsoleAndLog($"--- Game started ---");

            Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}Players in this Game:");

            foreach (Player player in gameState.Players)
            {
                Logger.Instance.WriteToConsoleAndLog($"o {player.Name}");
            }

            Logger.Instance.WriteToConsoleAndLog($"");

            return Task.CompletedTask;
        }

        public Task NotifyRoundStartedAsync(GameState gameState)
        {
            Logger.Instance.WriteToConsoleAndLog($"--- Round {gameState.CurrentRound} ---");

            return Task.CompletedTask;
        }

        public Task NotifyBidCollectionStartedAsync(GameState gameState)
        {
            Logger.Instance.WriteToConsoleAndLog("Collecting bids...");

            return Task.CompletedTask;
        }

        public Task<int> RequestBidAsync(GameState gameState, int roundNumber, TimeSpan maxWait)
        {

            var player = gameState.Players.FirstOrDefault(p => p.Name == Name);

            if (player != null)
            {
                Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}Your cards:");
                player.Hand.PrintListFancy();
            }
            //else
            //{
            //    Logger.Instance.WriteToConsoleAndLog($"Player with name '{Name}' not found.");
            //}

            int bid = UserConsoleIO.ReadIntUntilValid($"{Environment.NewLine}Enter your number of wins prediction:", 0, roundNumber);

            Logger.Instance.WriteToConsoleAndLog($"");
            Logger.Instance.WriteToConsoleAndLog(WaitingForOtherPlayer);
            Logger.Instance.WriteToConsoleAndLog($"");

            return Task.FromResult(bid);

        }

        public Task AnnounceBidAsync(GameState gameState, TimeSpan maxWait)
        {

            foreach (Player p in gameState.Players)
            {
                Logger.Instance.WriteToConsoleAndLog($"{p.Name} bids {p.RoundStats.Where(x => x.Round == gameState.CurrentRound).First().PredictedWins}");
            }

            return Task.CompletedTask;
        }

        public Task WaitForBidsReceivedAsync(GameState gameState)
        {
            Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}Press Enter to confirm, that you have acknowledged the Bids.");

            Console.ReadLine();

            Logger.Instance.WriteToConsoleAndLog(WaitingForOtherPlayer);

            return Task.CompletedTask;
        }

        public Task NotifyNotAllCardsInHandCanBePlayed(GameState gameState, List<Card> cardsThatPlayerIsAllowedToPlay, List<Card> cardsThatPlayerIsNotAllowedToPlay)
        {
            Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}Not all Cards in your hand can be played due to lead color/suit rule. Cards you are not allowed to play:");

            cardsThatPlayerIsNotAllowedToPlay.PrintListFancy();

            return Task.CompletedTask;
        }

        public Task<Card> RequestCardPlayAsync(GameState gameState, List<Card> hand, TimeSpan maxWait)
        {

            int cardToPlayIndex;

            Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}Cards you can play:");

            hand.PrintListFancy();

            cardToPlayIndex = UserConsoleIO.ReadIntUntilValid($"{Environment.NewLine}Enter the index of the card you want to play:", 0, hand.Count - 1);

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

                switch (choice)
                {
                    case "E":
                        ((TigressCard)card).PlayedAsType = CardType.ESCAPE;
                        break;
                    case "P":
                        ((TigressCard)card).PlayedAsType = CardType.PIRATE;
                        break;
                }

            }

            //Logger.Instance.WriteToConsoleAndLog($"{Name} plays {card}");

            Logger.Instance.WriteToConsoleAndLog($"");

            return Task.FromResult(card);

        }

        public Task NotifyCardPlayedAsync(GameState gameState, Player player, Card playedCard)
        {

            //string opponentPlayerName = gameState.Players.FirstOrDefault(x => x.Id == playerID).Name;

            Logger.Instance.WriteToConsoleAndLog($"{player.Name} played {playedCard}");

            return Task.CompletedTask;
        }

        public Task NotifyAboutSubRoundStartAsync(GameState gameState)
        {
            Console.Clear();
            Logger.Instance.WriteToConsoleAndLog($"--- Sub round {gameState.CurrentSubRound}/{gameState.CurrentRound} started ---");

            return Task.CompletedTask;
        }

        public Task NotifyAboutSubRoundEndAsync(GameState gameState)
        {
            //no need to tell console CPU what is happening
            Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}--- Sub round {gameState.CurrentSubRound}/{gameState.CurrentRound} ended ---");
            Logger.Instance.WriteToConsoleAndLog($"Press Enter Key to continue");

            Console.ReadLine();

            Logger.Instance.WriteToConsoleAndLog(WaitingForOtherPlayer);

            Console.Clear();

            return Task.CompletedTask;
        }

        public Task NotifyAboutMainRoundEndAsync(GameState gameState)
        {
            PrintCurrentStats(gameState);

            Console.ReadLine();

            Logger.Instance.WriteToConsoleAndLog(WaitingForOtherPlayer);

            Console.Clear();

            return Task.CompletedTask;
        }

        public Task NotifyAboutSubRoundWinnerAsync(GameState gameState, Player? winner, Card? winningCard)
        {

            if (winner == null)
            {
                Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}Winner: None!");
            }
            else
            {
                Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}Winner: {winner.Name} won round {gameState.CurrentSubRound} with {winningCard}");
            }

            PrintCurrentStats(gameState);

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

        public Task ShowMessageAsync(string message)
        {

            Logger.Instance.WriteToConsoleAndLog($"{Name} {message}");

            return Task.CompletedTask;

        }

        public Task NotifyPlayerTimedOutAsync(GameState gameState, Player player)
        {
            Logger.Instance.WriteToConsoleAndLog($"{player.Name} timed out!");

            return Task.CompletedTask;
        }

        private string GetCurrentRoundPlayerRoundStat(Player player, GameState gameState)
        {
            RoundStat roundStat = player.RoundStats.Where(x => x.Round == gameState.CurrentRound).First();

            return $"(V: {roundStat.ActualWins} / P: {roundStat.PredictedWins} / B: {roundStat.BonusPoints} / TOT: {player.TotalScore})";
        }

        private void PrintCurrentStats(GameState gameState)
        {
            Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}Current Stats:{Environment.NewLine}");

            foreach (Player player in gameState.Players)
            {
                Logger.Instance.WriteToConsoleAndLog($"{player.Name} {GetCurrentRoundPlayerRoundStat(player, gameState)}");
            }
        }



    }
}
