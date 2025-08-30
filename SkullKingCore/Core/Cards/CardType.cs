using System.Runtime.Serialization;

namespace SkullKingCore.Core.Cards
{

    [DataContract]
    public enum CardType : int
    {
        //Suit Cards
        [EnumMember] GREEN,//Parrot
        [EnumMember] LILA,//Pirate Map
        [EnumMember] YELLOW,//Treasure Chest
        [EnumMember] BLACK,//Jolly Roger(?)/Pirate flag
        //Special Cards
        [EnumMember] PIRATE,
        [EnumMember] TIGRESS,
        [EnumMember] SKULL_KING,
        [EnumMember] MERMAID,
        [EnumMember] ESCAPE,
        //Expansion Cards
        [EnumMember] LOOT,
        [EnumMember] KRAKEN,
        [EnumMember] WHITE_WHALE,
        //Used for cards where the Card Type can be decided, e.g. the Tigress
        [EnumMember] JOKER,
    }
}