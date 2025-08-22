using SkullKingCore.Core.Cards.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkullKingCore.Network.Rpc
{
    public class Dtos
    {

        // ===== DTOs (must mirror server-side DTOs) =====

        public sealed class GameStateDto
        {
            public int CurrentRound { get; set; }
            public int CurrentSubRound { get; set; }
            public int MaxRounds { get; set; }
            public List<PlayerDto> Players { get; set; } = new();
        }

        public sealed class PlayerDto
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public List<CardViewDto> Hand { get; set; } = new();
        }

        public sealed class CardViewDto
        {
            public int Index { get; set; }               // position in hand (for UI selection)
            public string CardType { get; set; } = "";   // enum name, e.g. "NUMBER", "PIRATE", "TIGRESS"
            public string Display { get; set; } = "";    // pretty text for UI
            public int? Value { get; set; }              // only used for NUMBER cards; null otherwise
            public string? TigressMode { get; set; }     // "Escape" | "Pirate" | null
        }


    }
}
