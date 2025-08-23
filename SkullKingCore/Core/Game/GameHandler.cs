using SkullKing.Network.Server;
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
            await CollectNamesAsync();

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

        /// <summary>
        /// Always ask all players for their display name concurrently; assign & broadcast.
        /// </summary>
        private async Task CollectNamesAsync()
        {
            // (Optional) purely visual signal – must NOT open an input dialog on clients.
            // await BroadcastInParallelAsync(c => c.NotifyNameCollectionStartedAsync(_state));

            // 1) Ask EVERY player at once (no blocking between players)
            var answers = await CollectFromAllPlayersAsync<string>(
                (player, controller) => controller.RequestName(_state, Timeout.InfiniteTimeSpan)
            ).ConfigureAwait(false);

            // 2) Sanitize + ensure uniqueness (case-insensitive), preserving starting order
            var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var (player, raw) in answers)
            {
                var seat = _state.Players.IndexOf(player) + 1;
                var name = (raw ?? string.Empty).Trim();
                if (name.Length == 0) name = $"Player {seat}";

                if (seen.TryGetValue(name, out var count))
                {
                    count++;
                    seen[name] = count;
                    name = $"{name} ({count})";
                    // mark the new variant as used too
                    seen[name] = 1;
                }
                else
                {
                    seen[name] = 1;
                }

                player.Name = name;
            }

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
            await BroadcastInParallelAsync(c => c.NotifyBidCollectionStartedAsync(_state));

            var round = _state.CurrentRound;
            var roundKey = new Player.Round(round);

            var bids = await CollectFromAllPlayersAsync<int>(
                (player, controller) => controller.RequestBidAsync(_state, round, Timeout.InfiniteTimeSpan)
            ).ConfigureAwait(false);

            foreach (var (player, bid) in bids)
                player.Bids[roundKey] = new Player.PredictedWins(bid);

            for (int i = 0; i < _state.Players.Count; i++)
            {
                int playerIndex = (_state.StartingPlayerIndex + i) % _state.Players.Count;
                var player = _state.Players[playerIndex];
                var controller = _controllers[player.Id];

                await controller.AnnounceBidAsync(_state, player, player.Bids[new Player.Round(_state.CurrentRound)].Value, Timeout.InfiniteTimeSpan);
            }

            // And your “wait for bids received” broadcast:
            await BroadcastInParallelAsync(c => c.WaitForBidsReceivedAsync(_state));
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

        public Task SendToAllControllersAsync(Func<IGameController, Task> action)
        {
            var tasks = new List<Task>(_controllers.Count);
            foreach (var controller in _controllers.Values)
                tasks.Add(SafeInvokeAsync(action, controller));   // fire all calls immediately
            return Task.WhenAll(tasks);                            // await all in parallel
        }

        private static async Task SafeInvokeAsync(
            Func<IGameController, Task> action,
            IGameController controller)
        {
            try
            {
                await action(controller).ConfigureAwait(false);
            }
            catch
            {
                
            }
        }

        // Inside GameHandler
        private IEnumerable<Player> PlayersInStartOrder()
        {
            int n = _state.Players.Count;
            for (int i = 0; i < n; i++)
                yield return _state.Players[(_state.StartingPlayerIndex + i) % n];
        }

        private async Task<(Player player, TResult result)[]> CollectFromAllPlayersAsync<TResult>(
            Func<Player, IGameController, Task<TResult>> request)
        {
            // If you used this earlier, keep it; otherwise iterate directly
            var players = PlayersInStartOrder().ToArray();

            var tasks = new Task<TResult>[players.Length];
            for (int i = 0; i < players.Length; i++)
            {
                var p = players[i];
                var c = _controllers[p.Id]; // IGameController
                tasks[i] = request(p, c);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            var results = new (Player, TResult)[players.Length];
            for (int i = 0; i < players.Length; i++)
                results[i] = (players[i], tasks[i].Result);

            return results;
        }

        /// <summary>
        /// Broadcast a no-result RPC to all controllers concurrently.
        /// </summary>
        private Task BroadcastInParallelAsync(Func<IGameController, Task> action)
        {
            var tasks = new List<Task>(_controllers.Count);
            foreach (var ctrl in _controllers.Values)
                tasks.Add(action(ctrl));
            return Task.WhenAll(tasks);
        }


    }

}
