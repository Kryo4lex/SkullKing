using SkullKingCore.Cards.Base;
using SkullKingCore.Cards.Implementations;
using SkullKingCore.Core.Game.Interfaces;
using SkullKingCore.Extensions;
using SkullKingCore.GameDefinitions;
using SkullKingCore.Logging;

namespace SkullKingCore.Core.Game
{

    public class GameState
    {
        public List<Player> Players { get; }
        public int CurrentRound { get; private set; } = 1;
        public int StartRound { get; }
        public int MaxRounds { get; }

        public List<Card> AllGameCards = new List<Card>();

        public List<Card> ShuffledGameCards = new List<Card>();

        private int _startingPlayerIndex = 0;

        private readonly Random _random = new(1234);
        private readonly Dictionary<string, IGameController> _controllers;

        public GameState(List<Player> players, int startRound, int maxRounds, Dictionary<string, IGameController> controllers)
        {
            Players = players ?? throw new ArgumentNullException(nameof(players));
            StartRound = startRound;
            CurrentRound = StartRound;
            MaxRounds = maxRounds;
            _controllers = controllers ?? throw new ArgumentNullException(nameof(controllers));
        }

        public async Task RunGameAsync()
        {
            while (!IsGameOver())
            {
                await StartRoundAsync();
                await CollectBidsAsync();
                await PlayRoundAsync();
                CurrentRound++;
            }

            await EndGameAsync();
        }

        private bool IsGameOver() => CurrentRound > MaxRounds;

        private Task StartRoundAsync()
        {
            Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}--- Round {CurrentRound} ---");

            AllGameCards = Deck.CreateDeck();

            ShuffledGameCards = AllGameCards.ToList().Shuffle();

            foreach (var player in Players)
            {

                player.Hand.Clear();

                player.Hand.AddRange(ShuffledGameCards.TakeChunk(CurrentRound));
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Ask all players to submit their bids for this round
        /// </summary>
        private async Task CollectBidsAsync()
        {

            Logger.Instance.WriteToConsoleAndLog("Collecting bids...");

            int playerCount = Players.Count;

            for (int i = 0; i < playerCount; i++)
            {

                int playerIndex = (_startingPlayerIndex + i) % playerCount;
                var player = Players[playerIndex];
                var controller = _controllers[player.Id];

                int bid = await controller.RequestBidAsync(this, CurrentRound, Timeout.InfiniteTimeSpan);
                player.Bids[CurrentRound] = bid; // store bid per round

                //await controller.ShowMessageAsync($"Your bid for round {CurrentRound}: {bid}");

            }
        }

        private async Task PlayRoundAsync()
        {
            List<Card> cardsInPlay = new();

            int playerCount = Players.Count;

            for (int i = 0; i < playerCount; i++)
            {
                int playerIndex = (_startingPlayerIndex + i) % playerCount;
                var player = Players[playerIndex];
                var controller = _controllers[player.Id];
                Card card = await controller.RequestCardPlayAsync(this, player.Hand, Timeout.InfiniteTimeSpan);

                //Special Case
                if(card is TigressCard)
                {
                    card.CardType = await controller.RequestTigressTypeAsync(this, Timeout.InfiniteTimeSpan);
                    Logger.Instance.WriteToConsoleAndLog($"Tigress played as {card.CardType}");
                }

                cardsInPlay.Add(card);
            }

            int? winnerIndex = TrickResolver.DetermineTrickWinnerIndex(cardsInPlay);

            //ToDo: Score Handling

            string winnerName;

            //ToDo: winner would be the person who otherwise would have won for Kraken and White Whale
            if (winnerIndex == null)
            {
                //winnerIndex = 0;
                winnerIndex = TrickResolver.DetermineTrickWinnerIndexNoSpecialCards(cardsInPlay);
                winnerName = "None!";
            }
            else
            {
                winnerName = Players[(int)winnerIndex].Name;
            }

            foreach (var controller in _controllers.Values)
            {
                await controller.ShowMessageAsync($"Round {CurrentRound} winner: {winnerName}");
            }

            _startingPlayerIndex = (_startingPlayerIndex + 1) % playerCount;
        }

        private async Task EndGameAsync()
        {

            Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}--- Game finished ---");

            /*
            foreach (var player in Players)
            {
                await _controllers[player.Id].ShowMessageAsync(
                    $"{player.Name} final score: {player.Score}, Bids: {string.Join(", ", player.Bids)}");
            }
            
            int highScore = -1;
            Player? gameWinner = null;
            
            foreach (var p in Players)
            {
                if (p.Score > highScore)
                {
                    highScore = p.Score;
                    gameWinner = p;
                }
            }

            foreach (var controller in _controllers.Values)
            {
                await controller.ShowMessageAsync($"Game winner: {gameWinner?.Name}!");
            }
            */
        }

    }
}
