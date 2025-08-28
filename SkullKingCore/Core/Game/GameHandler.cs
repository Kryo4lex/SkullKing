using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.Extensions;
using SkullKingCore.Core.Game.Interfaces;
using SkullKingCore.Core.Game.Scoring.Implementations;
using SkullKingCore.Core.Game.Scoring.Interfaces;
using SkullKingCore.Extensions;
using System.Threading.Tasks;

namespace SkullKingCore.Core.Game
{

    public class GameHandler
    {

        private readonly GameState _gameState;
        private readonly Dictionary<string, IGameController> _controllers;
        public IScoringSystem ScoringSystem { get; private set; }

        public GameHandler(List<Player> players, int startRound, int maxRounds, Dictionary<string, IGameController> controllers, IScoringSystem scoringSystem)
        {
            _controllers = controllers ?? throw new ArgumentNullException(nameof(controllers));
            _gameState = new GameState(players, startRound, maxRounds, Deck.CreateDeck());
            ScoringSystem = scoringSystem;

            foreach (var ctrl in _controllers.Values)
            {
                ctrl.GameState = _gameState;
            }
        }

        public async Task RunGameAsync()
        {
            await CollectNamesAsync();

            await SendToAllControllersAsync(c => c.NotifyGameStartedAsync(_gameState));

            //CurrentSubRound init = 1 required, CurrentRound can be different

            while (_gameState.CurrentRound <= _gameState.MaxRounds)
            {

                await StartRoundAsync();
                await CollectBidsAsync();

                // Play sub-rounds for this round
                while (_gameState.CurrentSubRound <= _gameState.CurrentRound)
                {

                    // Notify controllers about sub-round start
                    await SendToAllControllersAsync(c => c.NotifyAboutSubRoundStartAsync(_gameState));

                    // Play Sub Round
                    await PlaySubRoundAsync();

                    // Notify controllers about sub-round end
                    await SendToAllControllersAsync(c => c.NotifyAboutSubRoundEndAsync(_gameState));

                    _gameState.CurrentSubRound++; // increment sub-round
                }

                UpdateTotalScores();

                await BroadcastInParallelAsync(c => c.NotifyAboutMainRoundEndAsync(_gameState));

                _gameState.CurrentRound++; // increment round
            }

            await EndGameAsync();
        }

        private void UpdateTotalScores()
        {
            foreach (Player player in _gameState.Players)
            {
                player.TotalScore = ScoringSystem.ComputeTotalScore(player);
            }
        }

        /// <summary>
        /// Always ask all players for their display name concurrently; assign & broadcast.
        /// </summary>
        private async Task CollectNamesAsync()
        {
            // (Optional) purely visual signal – must NOT open an input dialog on clients.
            // await BroadcastInParallelAsync(c => c.NotifyNameCollectionStartedAsync(_state));

            // 1) Ask EVERY player at once (no blocking between players)
            var names = await CollectFromAllPlayersAsync<string>(
                (player, controller) => controller.RequestName(_gameState, Timeout.InfiniteTimeSpan)
            ).ConfigureAwait(false);

            // 2) Sanitize + ensure uniqueness (case-insensitive), preserving starting order
            var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var (player, raw) in names)
            {
                var seat = _gameState.Players.IndexOf(player) + 1;
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
            _gameState.CurrentSubRound = 1;

            // Announce the round to all controllers
            await SendToAllControllersAsync(c => c.NotifyRoundStartedAsync(_gameState));

            // Shuffle the game cards
            _gameState.ShuffledGameCards = _gameState.AllGameCards.ToList().Shuffle();

            // Deal cards to each player
            foreach (var player in _gameState.Players)
            {
                player.Hand.Clear();
                player.Hand.AddRange(_gameState.ShuffledGameCards.TakeChunk(_gameState.CurrentRound));
            }
        }

        /// <summary>
        /// Ask all players to submit their bids for this round
        /// </summary>
        private async Task CollectBidsAsync()
        {
            await BroadcastInParallelAsync(c => c.NotifyBidCollectionStartedAsync(_gameState));

            int round = _gameState.CurrentRound;

            var bidsOfPlayers = await CollectFromAllPlayersAsync<int>(
                (player, controller) => controller.RequestBidAsync(_gameState, round, Timeout.InfiniteTimeSpan)
            ).ConfigureAwait(false);

            foreach (var (player, predictedWins) in bidsOfPlayers)
            {
                RoundStat roundStat = new RoundStat(round, predictedWins);

                player.RoundStats.Add(roundStat);
            }

            for (int i = 0; i < _gameState.Players.Count; i++)
            {
                int playerIndex = (_gameState.StartingPlayerIndex + i) % _gameState.Players.Count;
                var player = _gameState.Players[playerIndex];
                var controller = _controllers[player.Id];

                await controller.AnnounceBidAsync(_gameState, Timeout.InfiniteTimeSpan);
            }

            await BroadcastInParallelAsync(c => c.WaitForBidsReceivedAsync(_gameState));
        }

        private async Task PlaySubRoundAsync()
        {
            List<Card> cardsInPlay = new();
            int playerCount = _gameState.Players.Count;

            for (int i = 0; i < playerCount; i++)
            {
                int playerIndex = (_gameState.StartingPlayerIndex + i) % playerCount;
                var player = _gameState.Players[playerIndex];
                var controller = _controllers[player.Id];

                List<Card> cardsThatPlayerIsAllowedToPlay = TrickResolver.GetAllowedCardsToPlay(cardsInPlay, player.Hand);

                List<Card> cardsThatPlayerIsNotAllowedToPlay = player.Hand!
                    .Except(cardsThatPlayerIsAllowedToPlay)
                    .ToList();

                if (player.Hand.Count != cardsThatPlayerIsAllowedToPlay.Count)
                {
                    await controller.NotifyNotAllCardsInHandCanBePlayed(_gameState, cardsThatPlayerIsAllowedToPlay, cardsThatPlayerIsNotAllowedToPlay);
                }

                Card playedCard = await controller.RequestCardPlayAsync(_gameState, cardsThatPlayerIsAllowedToPlay, Timeout.InfiniteTimeSpan);

                player.Hand.RemoveByGuid(playedCard.GuId);

                cardsInPlay.Add(playedCard);

                await SendToAllControllersAsync(c => c.NotifyCardPlayedAsync(_gameState, player, playedCard));
            }

            int? winnerIndex = TrickResolver.DetermineTrickWinnerIndex(cardsInPlay);
            Player? winner = null;
            Card? winningCard = null;
            int newStartingPlayerIndex = _gameState.StartingPlayerIndex; // default to current starting player

            if (winnerIndex.HasValue)
            {
                newStartingPlayerIndex = (_gameState.StartingPlayerIndex + winnerIndex.Value) % playerCount;
                winner = _gameState.Players[newStartingPlayerIndex];
                winningCard = cardsInPlay[winnerIndex.Value];

                HandleSubRoundWinner(winner, (int)winnerIndex, cardsInPlay);
            }
            else//special case if null, which could happen with KRAKEN or WHITE_WHALE
            {
                //this was a bug
                //winnerIndex = TrickResolver.DetermineTrickWinnerIndexNoSpecialCards(cardsInPlay);
                newStartingPlayerIndex = TrickResolver.DetermineTrickWinnerIndexNoSpecialCards(cardsInPlay);
            }

            // Set the next starting player only if we have a winner
            _gameState.StartingPlayerIndex = newStartingPlayerIndex;

            await SendToAllControllersAsync(c => c.NotifyAboutSubRoundWinnerAsync(_gameState, winner, winningCard));

        }

        private void HandleSubRoundWinner(Player winner, int winnerIndex, List<Card> cardsInPlay)
        {
            RoundStat roundStat = winner.RoundStats.Where(x => x.Round == _gameState.CurrentRound).First();

            roundStat.ActualWins++;
            roundStat.BonusPoints = roundStat.BonusPoints + TrickBonusPointResolver.ComputeTrickBonus(cardsInPlay, winnerIndex);            
        }

        private async Task EndGameAsync()
        {
            // ToDo: Get the winner depending on the score

            await SendToAllControllersAsync(c => c.NotifyGameEndedAsync(_gameState));

            await SendToAllControllersAsync(c => c.NotifyAboutGameWinnerAsync(_gameState, _gameState.Players.ToList()));

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
            int n = _gameState.Players.Count;
            for (int i = 0; i < n; i++)
                yield return _gameState.Players[(_gameState.StartingPlayerIndex + i) % n];
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
