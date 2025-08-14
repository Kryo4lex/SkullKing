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

        public MatchRunner(List<Player> players, int startRound, int maxRounds, Dictionary<string, IGameController> controllers)
        {
            _controllers = controllers ?? throw new ArgumentNullException(nameof(controllers));
            _state = new GameState(players, startRound, maxRounds, Deck.CreateDeck());
        }

        public async Task RunGameAsync()
        {
            //CurrentSubRound init = 1 required, CurrentRound can be different

            while (_state.CurrentRound <= _state.MaxRounds)
            {

                await StartRoundAsync();
                await CollectBidsAsync();

                // Play sub-rounds for this round
                while (_state.CurrentSubRound <= _state.CurrentRound)
                {
                    // Notify controllers about sub-round start
                    foreach (var controller in _controllers.Values)
                    {
                        await controller.NotifyAboutSubRoundStartAsync(_state);
                    }

                    await PlaySubRoundAsync();

                    // Notify controllers about sub-round end
                    foreach (var controller in _controllers.Values)
                    {
                        await controller.NotifyAboutSubRoundEndAsync(_state);
                    }

                    _state.CurrentSubRound++; // increment sub-round
                }

                _state.CurrentRound++; // increment round
            }

            await EndGameAsync();
        }

        private async Task StartRoundAsync()
        {
            // Reset sub-round counter at the start of each round
            _state.CurrentSubRound = 1;

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
                int playerIndex = (_state.StartingPlayerIndex + i) % playerCount;
                var player = _state.Players[playerIndex];
                var controller = _controllers[player.Id];

                int bid = await controller.RequestBidAsync(_state, _state.CurrentRound, Timeout.InfiniteTimeSpan);
                player.Bids[new Player.Round(_state.CurrentRound)] = new Player.PredictedWins(bid); // store bid per round
            }
        }

        private async Task PlaySubRoundAsync()
        {
            List<Card> cardsInPlay = new();
            int playerCount = _state.Players.Count;

            for (int i = 0; i < playerCount; i++)
            {
                int playerIndex = (_state.StartingPlayerIndex + i) % playerCount;
                var player = _state.Players[playerIndex];
                var controller = _controllers[player.Id];

                Card card = await controller.RequestCardPlayAsync(_state, player.Hand, Timeout.InfiniteTimeSpan);

                await controller.NotifyCardPlayedAsync(player, card);

                player.Hand.Remove(card);

                cardsInPlay.Add(card);
            }

            int? winnerIndex = TrickResolver.DetermineTrickWinnerIndex(cardsInPlay);
            Player? winner = null;
            Card? winningCard = null;
            int newStartingPlayerIndex = _state.StartingPlayerIndex; // default to current starting player

            if (winnerIndex.HasValue)
            {
                newStartingPlayerIndex = (_state.StartingPlayerIndex + winnerIndex.Value) % playerCount;
                winner = _state.Players[newStartingPlayerIndex];
                winningCard = cardsInPlay[winnerIndex.Value];
            }

            // Set the next starting player only if we have a winner
            _state.StartingPlayerIndex = newStartingPlayerIndex;

            foreach (var controller in _controllers.Values)
            {
                await controller.NotifyAboutSubRoundWinnerAsync(winner, winningCard, _state.CurrentRound);
            }
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
