using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.Extensions;
using SkullKingCore.Core.Game.Interfaces;
using SkullKingCore.Extensions;
using SkullKingCore.GameDefinitions;

namespace SkullKingCore.Core.Game
{

    public class GameHandler
    {
        private readonly GameState _state;
        private readonly Dictionary<string, IGameController> _controllers;

        public GameHandler(List<Player> players, int startRound, int maxRounds, Dictionary<string, IGameController> controllers)
        {
            _controllers = controllers ?? throw new ArgumentNullException(nameof(controllers));
            _state = new GameState(players, startRound, maxRounds, Deck.CreateDeck());
        }

        public async Task RunGameAsync()
        {

            for (int i = 0; i < _state.Players.Count; i++)
            {
                var player = _state.Players[i];
                var controller = _controllers[player.Id];

                string playerName = await controller.RequestName(_state, Timeout.InfiniteTimeSpan);
                player.Name = playerName;
            }

            await SendToAllControllersAsync(c => c.NotifyGameStartedAsync(_state));

            //CurrentSubRound init = 1 required, CurrentRound can be different

            while (_state.CurrentRound <= _state.MaxRounds)
            {

                await StartRoundAsync();
                await CollectBidsAsync();

                // Play sub-rounds for this round
                while (_state.CurrentSubRound <= _state.CurrentRound)
                {

                    // Notify controllers about sub-round start
                    await SendToAllControllersAsync(c => c.NotifyAboutSubRoundStartAsync(_state));

                    // Play Sub Round
                    await PlaySubRoundAsync();

                    // Notify controllers about sub-round end
                    await SendToAllControllersAsync(c => c.NotifyAboutSubRoundEndAsync(_state));

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
            await SendToAllControllersAsync(c => c.NotifyRoundStartedAsync(_state));

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

            await SendToAllControllersAsync(c => c.NotifyBidCollectionStartedAsync(_state));

            int playerCount = _state.Players.Count;

            for (int i = 0; i < playerCount; i++)
            {
                int playerIndex = (_state.StartingPlayerIndex + i) % playerCount;
                var player = _state.Players[playerIndex];
                var controller = _controllers[player.Id];

                int bid = await controller.RequestBidAsync(_state, _state.CurrentRound, Timeout.InfiniteTimeSpan);
                player.Bids[new Player.Round(_state.CurrentRound)] = new Player.PredictedWins(bid); // store bid per round
            }

            for (int i = 0; i < playerCount; i++)
            {
                int playerIndex = (_state.StartingPlayerIndex + i) % playerCount;
                var player = _state.Players[playerIndex];
                var controller = _controllers[player.Id];

                await controller.AnnounceBidAsync(_state, player, player.Bids[new Player.Round(_state.CurrentRound)].Value, Timeout.InfiniteTimeSpan);
            }

            await SendToAllControllersAsync(c => c.WaitForBidsReceivedAsync(_state));
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

                List<Card> cardsThatPlayerIsAllowedToPlay = TrickResolver.GetAllowedCardsToPlay(cardsInPlay, player.Hand);

                List<Card> cardsThatPlayerIsNotAllowedToPlay = player.Hand!
                    .Except(cardsThatPlayerIsAllowedToPlay)
                    .ToList();

                if (player.Hand.Count != cardsThatPlayerIsAllowedToPlay.Count)
                {
                    await controller.NotifyNotAllCardsInHandCanBePlayed(_state, cardsThatPlayerIsAllowedToPlay, cardsThatPlayerIsNotAllowedToPlay);
                }

                Card playedCard = await controller.RequestCardPlayAsync(_state, cardsThatPlayerIsAllowedToPlay, Timeout.InfiniteTimeSpan);

                player.Hand.RemoveByGuid(playedCard.GuId);

                cardsInPlay.Add(playedCard);

                await SendToAllControllersAsync(c => c.NotifyCardPlayedAsync(player, playedCard));
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
            else//special case if null, which could happen with KRAKEN or WHITE_WHALE
            {
                winnerIndex = TrickResolver.DetermineTrickWinnerIndexNoSpecialCards(cardsInPlay);
            }

            // Set the next starting player only if we have a winner
            _state.StartingPlayerIndex = newStartingPlayerIndex;

            await SendToAllControllersAsync(c => c.NotifyAboutSubRoundWinnerAsync(winner, winningCard, _state.CurrentRound));

        }

        private async Task EndGameAsync()
        {
            // ToDo: Get the winner depending on the score

            await SendToAllControllersAsync(c => c.NotifyGameEndedAsync(_state));

            await SendToAllControllersAsync(c => c.NotifyAboutGameWinnerAsync(_state, _state.Players.ToList()));

        }

        public async Task SendToAllControllersAsync(Func<IGameController, Task> action)
        {
            foreach (var controller in _controllers.Values)
            {
                await action(controller);
            }
        }

    }

}
