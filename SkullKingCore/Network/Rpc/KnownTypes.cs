using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.Implementations;
using SkullKingCore.Core.Game;

namespace SkullKing.Network.Rpc
{
    internal static class KnownTypes
    {
        public static readonly IReadOnlyCollection<Type> All = new[]
        {
            // domain roots
            typeof(GameState),
            typeof(Player),

            // player nested structs
            typeof(Player.Round),
            typeof(Player.PredictedWins),

            // base + concrete cards
            typeof(Card),
            typeof(EscapeCard),
            typeof(KrakenCard),
            typeof(LootCard),
            typeof(MermaidCard),
            typeof(NumberCard),
            typeof(PirateCard),
            typeof(SkullKingCard),
            typeof(TigressCard),
            typeof(WhiteWhaleCard),

            // common containers used in Args / Result
            typeof(List<Player>),
            typeof(List<Card>),
            typeof(Dictionary<Player.Round, Player.PredictedWins>),

            // primitives you pass around
            typeof(string),
            typeof(int),
            typeof(TimeSpan),
            typeof(Nullable<int>),
        };
    }
}
