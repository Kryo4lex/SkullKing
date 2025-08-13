using SkullKingCore.Cards.Base;
using SkullKingCore.Core.Game;
using SkullKingCore.Core.Game.Interfaces;
using SkullKingCore.GameDefinitions;
using SkullKingCore.Logging;

namespace SkullKingConsole.Controller
{

    public class ConsoleCPUController : IGameController
    {

        private readonly Random _random = new();

        public string Name { get; }

        public ConsoleCPUController(string name)
        {
            Name = name;
        }

        public Task<Card> RequestCardPlayAsync(GameState state, List<Card> hand, TimeSpan maxWait)
        {

            Card? card = hand[_random.Next(hand.Count)];

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

            int bid = _random.Next(0, roundNumber + 1);

            Logger.Instance.WriteToConsoleAndLog($"{Name} bids {bid}");

            return Task.FromResult(bid);

        }

        public Task<CardType> RequestTigressTypeAsync(GameState gameState, TimeSpan maxWait)
        {

            List<CardType> availableOptions = new List<CardType>();
            availableOptions.Add(CardType.ESCAPE);
            availableOptions.Add(CardType.PIRATE);

            return Task.FromResult(availableOptions[_random.Next(availableOptions.Count)]);

        }

    }
}
