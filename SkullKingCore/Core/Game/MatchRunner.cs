using SkullKingCore.Cards.Base;
using SkullKingCore.Core.Game.Interfaces;
using SkullKingCore.Extensions;
using SkullKingCore.GameDefinitions;

namespace SkullKingCore.Core.Game
{

    public class MatchRunner
    {
        private readonly GameState _state;
        private readonly Dictionary<string, IGameController> _controllers;
        private int _startingPlayerIndex = 0;
        private readonly Random _random = new(1234);

        public MatchRunner(List<Player> players, int startRound, int maxRounds, Dictionary<string, IGameController> controllers)
        {
            _controllers = controllers ?? throw new ArgumentNullException(nameof(controllers));
            _state = new GameState(players, startRound, maxRounds, Deck.CreateDeck());
        }

        public async Task RunGameAsync()
        {
            while (!IsGameOver())
            {
                await StartRoundAsync();
                await CollectBidsAsync();
                await PlayRoundAsync();
                _state.CurrentRound++;
            }

            await EndGameAsync();
        }

        private bool IsGameOver() => _state.CurrentRound > _state.MaxRounds;

        private async Task StartRoundAsync()
        {
            // Announce the round to all controllers
            foreach (var controller in _controllers.Values)
            {
                await controller.NotifyRoundStartedAsync(_state);
            }

            // Shuffle the game cards
            _state.ShuffledGameCards = _state.AllGameCards.ToList().Shuffle();

            // Deal cards to each player
            foreach (var player in _state.Players)
            {
                player.Hand.Clear();
                player.Hand.AddRange(_state.ShuffledGameCards.TakeChunk(_state.CurrentRound));
            }
        }

        /// <summary>
        /// Ask all players to submit their bids for this round
        /// </summary>
        private async Task CollectBidsAsync()
        {
            foreach (var controller in _controllers.Values)
            {
                await controller.NotifyBidCollectionStartedAsync(_state);
            }

            int playerCount = _state.Players.Count;

            for (int i = 0; i < playerCount; i++)
            {
                int playerIndex = (_startingPlayerIndex + i) % playerCount;
                var player = _state.Players[playerIndex];
                var controller = _controllers[player.Id];

                int bid = await controller.RequestBidAsync(_state, _state.CurrentRound, Timeout.InfiniteTimeSpan);
                player.Bids[new Player.Round(_state.CurrentRound)] = new Player.PredictedWins(bid); // store bid per round
            }
        }

        private async Task PlayRoundAsync()
        {
            List<Card> cardsInPlay = new();
            int playerCount = _state.Players.Count;

            for (int i = 0; i < playerCount; i++)
            {
                int playerIndex = (_startingPlayerIndex + i) % playerCount;
                var player = _state.Players[playerIndex];
                var controller = _controllers[player.Id];

                Card card = await controller.RequestCardPlayAsync(_state, player.Hand, Timeout.InfiniteTimeSpan);

                await controller.NotifyCardPlayedAsync(player, card);

                cardsInPlay.Add(card);
            }

            int? winnerIndex = TrickResolver.DetermineTrickWinnerIndex(cardsInPlay);
            Player? winner = winnerIndex.HasValue ? _state.Players[winnerIndex.Value] : null;
            Card? winningCard = winnerIndex.HasValue ? cardsInPlay[winnerIndex.Value] : null;

            // ToDo: winner would be the person who otherwise would have won for Kraken
            if (winnerIndex == null)
            {
                winnerIndex = TrickResolver.DetermineTrickWinnerIndexNoSpecialCards(cardsInPlay);
            }
            else
            {
                // ToDo: Score Handling
            }

            foreach (var controller in _controllers.Values)
            {
                await controller.NotifyAboutRoundWinnerAsync(winner, winningCard, _state.CurrentRound);
            }

            _startingPlayerIndex = (_startingPlayerIndex + 1) % playerCount;
        }

        private async Task EndGameAsync()
        {
            // ToDo: Get the winner depending on the score

            foreach (var controller in _controllers.Values)
            {
                await controller.NotifyGameEndedAsync(_state);
            }

            foreach (var controller in _controllers.Values)
            {
                await controller.NotifyAboutGameWinnerAsync(_state, _state.Players.ToList());
            }
        }
    }

}
