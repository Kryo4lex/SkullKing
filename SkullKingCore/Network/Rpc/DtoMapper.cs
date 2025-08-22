#nullable enable

using SkullKing.Network.Server;
using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.Factory;
using SkullKingCore.Core.Cards.Implementations;
using SkullKingCore.Core.Game;
using static SkullKingCore.Network.Rpc.Dtos;

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
                Index = index,
                CardType = card.CardType.ToString(),
                Display = card.ToString(),
                Value = null,
                TigressMode = null
            };

            if (card is NumberCard n) dto.Value = n.Number;

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

            int value = (int)(dto.Value ?? -1); // only meaningful for NUMBER
            Card card = CardFactory.Create(type, value);

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
        // PLAYER -> PlayerDto
        // --------------------------

        public static PlayerDto ToPlayerDto(Player p, bool includeHand = true)
        {
            if (p is null) throw new ArgumentNullException(nameof(p));
            return new PlayerDto
            {
                Id = p.Id,
                Name = p.Name,
                Hand = includeHand && p.Hand is not null ? ToCardDtos(p.Hand) : new List<CardViewDto>()
            };
        }

        public static List<PlayerDto> ToPlayerDtos(IEnumerable<Player> players, bool includeHands = false)
        {
            if (players is null) throw new ArgumentNullException(nameof(players));
            return players.Select(p => ToPlayerDto(p, includeHands)).ToList();
        }

        // Viewer-specific projection (only show the viewer's hand)
        public static GameStateDto ToGameStateDtoForViewer(GameState s, string viewerPlayerId)
        {
            if (s is null) throw new ArgumentNullException(nameof(s));
            if (viewerPlayerId is null) throw new ArgumentNullException(nameof(viewerPlayerId));

            var players = new List<PlayerDto>(s.Players.Count);
            foreach (var p in s.Players)
            {
                bool includeHand = p.Id == viewerPlayerId;
                players.Add(ToPlayerDto(p, includeHand));
            }

            return new GameStateDto
            {
                CurrentRound = s.CurrentRound,
                CurrentSubRound = s.CurrentSubRound,
                MaxRounds = s.MaxRounds,
                Players = players
            };
        }

        // Generic summary (no hands)
        public static GameStateDto ToGameStateDto(GameState s)
        {
            if (s is null) throw new ArgumentNullException(nameof(s));
            return new GameStateDto
            {
                CurrentRound = s.CurrentRound,
                CurrentSubRound = s.CurrentSubRound,
                MaxRounds = s.MaxRounds,
                Players = ToPlayerDtos(s.Players, includeHands: false)
            };
        }

        // Overload for convenience
        public static List<Card> ToDomainCards(List<CardViewDto> dtos)
        {
            if (dtos is null) throw new ArgumentNullException(nameof(dtos));
            return dtos.Select(ToDomainCard).ToList();
        }

    }
}
