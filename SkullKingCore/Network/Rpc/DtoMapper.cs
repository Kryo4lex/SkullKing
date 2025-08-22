#nullable enable

using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.Factory;
using SkullKingCore.Core.Cards.Implementations;
using SkullKingCore.Core.Game;
using SkullKingCore.Network.Rpc.Dto;
using PlayerPredictedWins = SkullKingCore.Core.Game.Player.PredictedWins;
using PlayerRound = SkullKingCore.Core.Game.Player.Round;

namespace SkullKing.Network.Rpc.Dtos
{
    public static class DtoMapper
    {
        // --------------------------
        // CARD <-> CardViewDto
        // --------------------------

        public static CardViewDto ToCardDto(Card card, int index = -1)
        {
            if (card is null) throw new ArgumentNullException(nameof(card));

            var dto = new CardViewDto
            {
                CardType = card.CardType.ToString(),
                Display = card.ToString(),
                TigressMode = null
            };

            if (card is NumberCard n) dto.GenericValue = n.Number;

            if (card is TigressCard tig)
            {
                dto.TigressMode = tig.PlayedAsType == SkullKingCore.GameDefinitions.CardType.ESCAPE ? "Escape" : "Pirate";
            }

            return dto;
        }

        public static List<CardViewDto> ToCardDtos(IReadOnlyList<Card> cards)
        {
            if (cards is null) throw new ArgumentNullException(nameof(cards));
            var list = new List<CardViewDto>(cards.Count);
            for (int i = 0; i < cards.Count; i++)
                list.Add(ToCardDto(cards[i], i));
            return list;
        }

        public static Card ToDomainCard(CardViewDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            if (!Enum.TryParse<SkullKingCore.GameDefinitions.CardType>(dto.CardType, true, out var type))
                throw new InvalidOperationException($"Unknown card type: {dto.CardType}");

            int genericValue = dto.GenericValue.HasValue ? dto.GenericValue.Value : -1;

            Card card = CardFactory.Create(type, genericValue);

            ApplyTigressModeIfAny(card, dto.TigressMode);
            return card;
        }

        public static List<Card> CardsFromDtos(IEnumerable<CardViewDto> dtos)
        {
            if (dtos is null) throw new ArgumentNullException(nameof(dtos));
            return dtos.Select(ToDomainCard).ToList();
        }

        public static Card SelectFromHand(IList<Card> hand, int index, string? tigressMode)
        {
            if (hand is null) throw new ArgumentNullException(nameof(hand));
            if (index < 0 || index >= hand.Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} out of range (0..{hand.Count - 1}).");

            var chosen = hand[index];
            ApplyTigressModeIfAny(chosen, tigressMode);
            return chosen;
        }

        private static void ApplyTigressModeIfAny(Card card, string? tigressMode)
        {
            if (card is not TigressCard tig) return;
            if (string.IsNullOrWhiteSpace(tigressMode)) return;

            tig.PlayedAsType =
                tigressMode.Equals("Escape", StringComparison.OrdinalIgnoreCase) ? SkullKingCore.GameDefinitions.CardType.ESCAPE :
                tigressMode.Equals("Pirate", StringComparison.OrdinalIgnoreCase) ? SkullKingCore.GameDefinitions.CardType.PIRATE :
                tig.PlayedAsType; // ignore invalid
        }

        // --------------------------
        // DTO -> Domain Player
        // --------------------------

        public static Player ToDomainPlayer(PlayerDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            var player = new Player(dto.Id, dto.Name);

            // Hand
            player.Hand.Clear();
            if (dto.Hand is not null && dto.Hand.Count > 0)
            {
                var cards = CardsFromDtos(dto.Hand);
                player.Hand.AddRange(cards);
            }

            // Bids
            player.Bids.Clear();
            if (dto.Bids is not null && dto.Bids.Count > 0)
            {
                var bids = BidsFromDtos(dto.Bids);
                foreach (var kvp in bids)
                    player.Bids[kvp.Key] = kvp.Value;
            }

            return player;
        }

        public static List<Player> ToDomainPlayers(IEnumerable<PlayerDto> dtos)
        {
            if (dtos is null) throw new ArgumentNullException(nameof(dtos));
            return dtos.Select(ToDomainPlayer).ToList();
        }

        // --------------------------
        // PLAYER -> PlayerDto  (ALWAYS include Hand + Bids)
        // --------------------------

        public static PlayerDto ToPlayerDto(Player p)
        {
            if (p is null) throw new ArgumentNullException(nameof(p));
            return new PlayerDto
            {
                Id = p.Id,
                Name = p.Name,
                Hand = p.Hand is not null ? ToCardDtos(p.Hand) : new List<CardViewDto>(),
                Bids = ToBidDtos(p.Bids)
            };
        }

        public static List<PlayerDto> ToPlayerDtos(IEnumerable<Player> players)
        {
            if (players is null) throw new ArgumentNullException(nameof(players));
            var list = new List<PlayerDto>();
            foreach (var p in players)
                list.Add(ToPlayerDto(p)); // full hands + bids for everyone
            return list;
        }

        // --------------------------
        // Bids <-> BidDto
        // --------------------------

        public static List<BidDto> ToBidDtos(
            IReadOnlyDictionary<PlayerRound, PlayerPredictedWins> bids)
        {
            if (bids is null) throw new ArgumentNullException(nameof(bids));
            var list = new List<BidDto>(bids.Count);
            foreach (var kvp in bids)
                list.Add(new BidDto { Round = kvp.Key.Value, PredictedWins = kvp.Value.Value });
            return list;
        }

        public static Dictionary<PlayerRound, PlayerPredictedWins> BidsFromDtos(
            IEnumerable<BidDto> dtos)
        {
            if (dtos is null) throw new ArgumentNullException(nameof(dtos));
            var dict = new Dictionary<PlayerRound, PlayerPredictedWins>();
            foreach (var b in dtos)
                dict[new PlayerRound(b.Round)] = new PlayerPredictedWins(b.PredictedWins);
            return dict;
        }

        // --------------------------
        // GameState -> GameStateDto (ALWAYS expanded)
        // --------------------------

        public static GameStateDto ToGameStateDtoForViewer(GameState s, string viewerPlayerId)
        {
            // Intentionally ignore viewerPlayerId: include everything for everyone.
            if (s is null) throw new ArgumentNullException(nameof(s));

            return new GameStateDto
            {
                CurrentRound = s.CurrentRound,
                CurrentSubRound = s.CurrentSubRound,
                MaxRounds = s.MaxRounds,
                Players = ToPlayerDtos(s.Players) // full Hand + Bids for each player
            };
        }

        public static GameStateDto ToGameStateDto(GameState s)
        {
            if (s is null) throw new ArgumentNullException(nameof(s));
            return new GameStateDto
            {
                CurrentRound = s.CurrentRound,
                CurrentSubRound = s.CurrentSubRound,
                MaxRounds = s.MaxRounds,
                Players = ToPlayerDtos(s.Players) // full Hand + Bids for each player
            };
        }

    }
}
