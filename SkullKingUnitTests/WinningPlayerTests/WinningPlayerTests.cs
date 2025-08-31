using SkullKingCore.Core.Cards;
using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.Implementations;
using SkullKingCore.Core.Cards.SubCardTypes;
using SkullKingCore.Core.Game;

namespace SkullKingUnitTests.WinningPlayerTests
{
    public class TrickWinnerFacts
    {
        private static void AssertWinnerIndex(List<Card> trick, int? expectedWinnerIndex)
        {
            int? actual = TrickResolver.GetWinningPlayerIndex(trick);
            Assert.Equal(expectedWinnerIndex, actual);
        }

        //0x distinguishes Tricks of English rule book, which seems to be different from the German one...

        [Fact]
        public void Case_0A()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.GREEN,  14),
                new NumberCard(CardType.YELLOW,  2),
                new NumberCard(CardType.LILA,    6),
                new NumberCard(CardType.BLACK,   4),//Winner, black is stronger than other suit/color cards
            };
            AssertWinnerIndex(trick, 3);
        }

        [Fact]
        public void Case_0B()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.YELLOW,  12),//Winner, leading color and highest value of colors
                new NumberCard(CardType.YELLOW,   5),
                new NumberCard(CardType.LILA,    14),
            };
            AssertWinnerIndex(trick, 0);
        }

        [Fact]
        public void Case_0C()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.YELLOW,  12),
                new NumberCard(CardType.YELLOW,   5),
                new NumberCard(CardType.BLACK,    2),//Winner, black is stronger than other suit/color cards
            };
            AssertWinnerIndex(trick, 2);
        }

        //"Farbkarten", page 8, bottom
        [Fact]
        public void Case_1()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.GREEN,  7),
                new NumberCard(CardType.GREEN, 12),//Winner
                new NumberCard(CardType.GREEN,  8),
            };
            AssertWinnerIndex(trick, 1);
        }

        //"Farbkarten", page 9, top
        [Fact]
        public void Case_2()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.YELLOW,  8),//Winner
                new NumberCard(CardType.YELLOW,  5),
                new NumberCard(CardType.LILA,   10),
            };
            AssertWinnerIndex(trick, 0);
        }

        //"Farbkarten", page 9, bottom
        [Fact]
        public void Case_3()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.YELLOW, 12),
                new NumberCard(CardType.BLACK,   5),
                new NumberCard(CardType.BLACK,   7),//Winner
            };
            AssertWinnerIndex(trick, 2);
        }

        //"Sonderkarten", page 10, "Flucht", Logbuch des Kapitäns
        //All cards are Escape cards (Flucht)
        [Fact]
        public void Case_4()
        {
            var trick = new List<Card>
            {
                new EscapeCard(),//Winner, because first Escape card played
                new EscapeCard(),
                new EscapeCard(),
            };
            AssertWinnerIndex(trick, 0);
        }

        //"Sonderkarten", page 10, "Charaktere", "Piraten"
        //Pirates beat Escape, Color Card, incl. Trump, and Mermaids
        [Fact]
        public void Case_5()
        {
            var trick = new List<Card>
            {
                new EscapeCard(),
                new NumberCard(CardType.LILA,   14),
                new NumberCard(CardType.GREEN,  14),
                new NumberCard(CardType.YELLOW, 14),
                new NumberCard(CardType.BLACK,  14),
                new MermaidCard(MermaidType.ALYRA),
                new PirateCard(PirateType.BENDT_THE_BANDIT),//Winner
            };
            AssertWinnerIndex(trick, 6);
        }

        //"Sonderkarten", page 10, "Charaktere", "Piraten"
        //All cards are Pirates
        [Fact]
        public void Case_6()
        {
            var trick = new List<Card>
            {
                new PirateCard(PirateType.BENDT_THE_BANDIT),//Winner, because first Pirate card played
                new PirateCard(PirateType.HARRY_THE_GIANT),
                new PirateCard(PirateType.JUANITA_JADE),
                new PirateCard(PirateType.RASCAL_OF_ROATAN),
                new PirateCard(PirateType.ROSIE_D_LANEY),
            };
            AssertWinnerIndex(trick, 0);
        }

        //"Sonderkarten", page 11, "Charaktere", "Skull King"
        //The scourge of the seas is the trump of Pirates and beats all numbered cards and Pirates(including the Tigress, when played as a Pirate).
        [Fact]
        public void Case_7()
        {
            var trick = new List<Card>
            {
                new EscapeCard(),
                new NumberCard(CardType.LILA,   14),
                new NumberCard(CardType.GREEN,  14),
                new NumberCard(CardType.YELLOW, 14),
                new NumberCard(CardType.BLACK,  14),
                new PirateCard(PirateType.BENDT_THE_BANDIT),
                new SkullKingCard(),//Winner
            };
            AssertWinnerIndex(trick, 6);
        }

        //"Sonderkarten", page 11, "Charaktere", "Skull King"
        //The only ones who can defeat him are the Mermaids, luring him into the sea with their precious treasure.
        [Fact]
        public void Case_8()
        {
            var trick = new List<Card>
            {
                new EscapeCard(),
                new NumberCard(CardType.LILA,   14),
                new NumberCard(CardType.GREEN,  14),
                new NumberCard(CardType.YELLOW, 14),
                new NumberCard(CardType.BLACK,  14),
                new SkullKingCard(),
                new MermaidCard(MermaidType.ALYRA)//Winner
            };
            AssertWinnerIndex(trick, 6);
        }

        //"Sonderkarten", page 11, "Charaktere", "Mermaid"
        //Mermaids beat all numbered suits
        [Fact]
        public void Case_9()
        {
            var trick = new List<Card>
            {
                new EscapeCard(),
                new NumberCard(CardType.LILA,   14),
                new NumberCard(CardType.GREEN,  14),
                new NumberCard(CardType.YELLOW, 14),
                new NumberCard(CardType.BLACK,  14),
                new MermaidCard(MermaidType.SIRENA),//Winner
            };
            AssertWinnerIndex(trick, 5);
        }

        //"Sonderkarten", page 11, "Charaktere", "Mermaid"
        //...but lose to all of the Pirates
        [Fact]
        public void Case_10()
        {
            var trick = new List<Card>
            {
                new EscapeCard(),
                new NumberCard(CardType.LILA,   14),
                new NumberCard(CardType.GREEN,  14),
                new NumberCard(CardType.YELLOW, 14),
                new NumberCard(CardType.BLACK,  14),
                new MermaidCard(MermaidType.ALYRA),
                new PirateCard(PirateType.BENDT_THE_BANDIT),//Winner
            };
            AssertWinnerIndex(trick, 6);
        }

        //"Sonderkarten", page 11, "Charaktere", "Mermaid"
        //If both Mermaids end up in the same trick, the first one played wins the trick.
        [Fact]
        public void Case_11()
        {
            var trick = new List<Card>
            {
                new EscapeCard(),
                new NumberCard(CardType.LILA,   14),
                new NumberCard(CardType.GREEN,  14),
                new NumberCard(CardType.YELLOW, 14),
                new NumberCard(CardType.BLACK,  14),
                new SkullKingCard(),
                new MermaidCard(MermaidType.SIRENA),//Winner, first played
                new MermaidCard(MermaidType.ALYRA),
            };
            AssertWinnerIndex(trick, 6);
        }

        //"Sonderkarten", page 11, "Charaktere", "Mermaid"
        //Captain’s Log: If a Pirate, the Skull King, and a Mermaid are all played in the same trick, the Mermaid always wins the trick, regardless of order of play. 
        [Fact]
        public void Case_12()
        {
            var trick = new List<Card>
            {
                new EscapeCard(),
                new NumberCard(CardType.LILA,   14),
                new NumberCard(CardType.GREEN,  14),
                new NumberCard(CardType.YELLOW, 14),
                new NumberCard(CardType.BLACK,  14),
                new PirateCard(PirateType.BENDT_THE_BANDIT),
                new SkullKingCard(),
                new MermaidCard(MermaidType.SIRENA),//Winner, first played and special case, Pirate, Skull King and Mermaid in the same trick
                new MermaidCard(MermaidType.ALYRA),
            };
            AssertWinnerIndex(trick, 7);
        }

        // page 12, "Sonderkarten", "Sonderkarten anspielen", "Fluchtkarte anspielen"
        // page 12, "Special Cards, "Leading with Special Cards", "Leading with an Escape"
        [Fact]
        public void Case_13()
        {
            var trick = new List<Card>
            {
                new EscapeCard(),
                new NumberCard(CardType.LILA, 11),
                new NumberCard(CardType.LILA,  6),
            };
            AssertWinnerIndex(trick, 1);
        }

        // page 12, "Special Cards, "Leading with Special Cards", "Leading with a Character"
        [Fact]
        public void Case_14()
        {
            var trick = new List<Card>
            {
                new PirateCard(PirateType.JUANITA_JADE),//Winner
                new NumberCard(CardType.GREEN, 9),
                new NumberCard(CardType.LILA,  9),
            };
            AssertWinnerIndex(trick, 0);
        }

        [Fact]
        public void Case_15()
        {
            var trick = new List<Card>
            {
                new MermaidCard(MermaidType.ALYRA),//Winner
                new NumberCard(CardType.GREEN, 9),
                new NumberCard(CardType.LILA,  9),
            };
            AssertWinnerIndex(trick, 0);
        }

        [Fact]
        public void Case_16()
        {
            var trick = new List<Card>
            {
                new SkullKingCard(),//Winner
                new NumberCard(CardType.GREEN, 9),
                new NumberCard(CardType.LILA,  9),
            };
            AssertWinnerIndex(trick, 0);
        }

        [Fact]
        public void Case_17()
        {
            var trick = new List<Card>
            {
                new WhiteWhaleCard(),
                new NumberCard(CardType.GREEN, 9),//Winner, First played
                new NumberCard(CardType.LILA,  9),
            };
            AssertWinnerIndex(trick, 1);
        }

        [Fact]
        public void Case_18()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.LILA,   8),
                new NumberCard(CardType.BLACK, 10),//Winner, highest value
                new WhiteWhaleCard(),
                new NumberCard(CardType.LILA,   4),
            };
            AssertWinnerIndex(trick, 1);
        }

        // page 24, "Weißer Wal", example
        [Fact]
        public void Case_19()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.BLACK,   2),
                new NumberCard(CardType.YELLOW, 14),//Winner, highest value
                new SkullKingCard(),
                new WhiteWhaleCard(),
            };
            AssertWinnerIndex(trick, 1);
        }

        //White Whale, but all played cards are only Special cards
        [Fact]
        public void Case_20A()
        {
            var trick = new List<Card>
            {
                new EscapeCard(),
                new MermaidCard(MermaidType.ALYRA),
                new PirateCard(PirateType.BENDT_THE_BANDIT),
                new SkullKingCard(),
                new WhiteWhaleCard(),
            };
            AssertWinnerIndex(trick, null);//no winner
        }

        [Fact]
        public void Case_20B()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.GREEN,  14),
                new NumberCard(CardType.LILA,   14),
                new NumberCard(CardType.YELLOW, 14),
                new NumberCard(CardType.BLACK,  14),
                new EscapeCard(),
                new MermaidCard(MermaidType.ALYRA),
                new PirateCard(PirateType.BENDT_THE_BANDIT),
                new SkullKingCard(),
                new KrakenCard(),
            };
            AssertWinnerIndex(trick, null);//no winner
        }

        //White Whale first played before Kraken, but Kraken destroys
        [Fact]
        public void Case_21()
        {
            var trick = new List<Card>
            {
                new WhiteWhaleCard(),
                new KrakenCard(),
            };
            AssertWinnerIndex(trick, null);
        }

        //Kraken first played before White Whale, but White Whale destroys -> White Whale effect is applied
        [Fact]
        public void Case_22()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.GREEN,  14),
                new NumberCard(CardType.LILA,   13),
                new NumberCard(CardType.YELLOW, 12),
                new NumberCard(CardType.BLACK,  11),
                new KrakenCard(),
                new WhiteWhaleCard(),
            };
            AssertWinnerIndex(trick, 0);
        }

        [Fact]
        public void Case_23()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.GREEN,   1),
                new NumberCard(CardType.BLACK,   2),
                new NumberCard(CardType.YELLOW, 11),//Winner, same highest number, but first played
                new NumberCard(CardType.GREEN,  11),
                new NumberCard(CardType.LILA,    1),
                new WhiteWhaleCard(),
            };
            AssertWinnerIndex(trick, 2);
        }

        [Fact]
        public void Case_24()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.GREEN,  2),
                new NumberCard(CardType.BLACK, 14),// Winner, brcause
                new WhiteWhaleCard(),
            };
            AssertWinnerIndex(trick, 1);
        }

        [Fact]
        public void Case_25()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.GREEN, 14),
                new NumberCard(CardType.BLACK,  1),// Winner, trump card
            };
            AssertWinnerIndex(trick, 1);
        }

        [Fact]
        public void Case_26()
        {
            var trick = new List<Card>
            {
                new PirateCard(PirateType.BENDT_THE_BANDIT),// Winner
                new MermaidCard(MermaidType.ALYRA),
            };
            AssertWinnerIndex(trick, 0);
        }

        [Fact]
        public void Case_27()
        {
            var trick = new List<Card>
            {
                new SkullKingCard(),
                new MermaidCard(MermaidType.ALYRA),// Winner
            };
            AssertWinnerIndex(trick, 1);
        }

        [Fact]
        public void Case_28()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.BLACK,   7),//Winner
                new NumberCard(CardType.YELLOW,  8),
                new NumberCard(CardType.BLACK,   4),
                new NumberCard(CardType.YELLOW,  2),
            };
            AssertWinnerIndex(trick, 0);
        }

        [Fact]
        public void Case_29()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.YELLOW,  2),
                new NumberCard(CardType.BLACK,   7),//Winner
                new NumberCard(CardType.YELLOW,  8),
                new NumberCard(CardType.BLACK,   4),
            };
            AssertWinnerIndex(trick, 1);
        }

        [Fact]
        public void Case_30()
        {
            var trick = new List<Card>
            {
                new EscapeCard(),
                new NumberCard(CardType.LILA,   1),
                new NumberCard(CardType.LILA,   6),//Winner lead color and highest number
                new NumberCard(CardType.GREEN, 14),
            };
            AssertWinnerIndex(trick, 2);
        }

        //Website:
        // Q: What happens when the Kraken and the White Whale are played in the same trick?
        // A: Whichever was played second retains its power. The first one played acts as an escape card. If the Kraken is played after the White Whale, the player who would have won the trick had neither creature been played, leads out the next trick.
        [Fact]
        public void Case_31()
        {
            var trick = new List<Card>
            {
                new EscapeCard(),//Winner
                new EscapeCard(),
                new EscapeCard(),
                new WhiteWhaleCard(),
            };
            AssertWinnerIndex(trick, 0);
        }

        //Rule Book:
        //If only special cards were played, then the trick is discarded (like the Kraken) and the person who played the White Whale is the next to lead.
        [Fact]
        public void Case_32()
        {
            var trick = new List<Card>
            {
                new EscapeCard(),
                new PirateCard(PirateType.BENDT_THE_BANDIT),
                new MermaidCard(MermaidType.ALYRA),
                new SkullKingCard(),
                new WhiteWhaleCard(),
            };
            AssertWinnerIndex(trick, null);//No Winner, trick is discarded
        }

        [Fact]
        public void Case_33()
        {
            var trick = new List<Card>
            {
                new SkullKingCard(),
                new SkullKingCard(),
                new MermaidCard(MermaidType.ALYRA),//Winner, must beat also multiple cards
                new SkullKingCard(),
                new SkullKingCard(),
            };
            AssertWinnerIndex(trick, 2);
        }

        [Fact]
        public void Case_34()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.GREEN,   1),
                new NumberCard(CardType.YELLOW,  2),
                new NumberCard(CardType.LILA,    6),
                new NumberCard(CardType.GREEN,  14),
            };
            AssertWinnerIndex(trick, 3);
        }

        //first played would win
        [Fact]
        public void Case_35()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.GREEN,   1),
                new NumberCard(CardType.YELLOW,  2),
                new NumberCard(CardType.LILA,    6),
                new NumberCard(CardType.GREEN,  14),
                new NumberCard(CardType.GREEN,  14),
            };
            AssertWinnerIndex(trick, 3);
        }

        // L1 — All escape-like (Loot counts as Escape): first played wins (Captain’s Log-compatible)
        [Fact]
        public void Case_L1()
        {
            var trick = new List<Card>
            {
                new LootCard(),     // Winner (first escape-like)
                new EscapeCard(),
                new LootCard(),
            };
            AssertWinnerIndex(trick, 0);
        }

        // L2 — First is Escape, then only Loots: still first escape-like wins
        [Fact]
        public void Case_L2()
        {
            var trick = new List<Card>
            {
                new EscapeCard(),   // Winner (first escape-like)
                new LootCard(),
                new LootCard(),
            };
            AssertWinnerIndex(trick, 0);
        }

        // L3 — Numbers present; Loot is ignored for ranking; follow lead suit
        [Fact]
        public void Case_L3()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.YELLOW, 10),
                new LootCard(),
                new NumberCard(CardType.YELLOW, 12), // Winner (lead suit, highest)
            };
            AssertWinnerIndex(trick, 2);
        }

        // L4 — Lead Loot; first number played sets suit; off-suit higher number does NOT win
        [Fact]
        public void Case_L4()
        {
            var trick = new List<Card>
            {
                new LootCard(),
                new NumberCard(CardType.YELLOW, 11), // Winner (lead suit established here)
                new NumberCard(CardType.GREEN, 14),
                new NumberCard(CardType.YELLOW, 9),
            };
            AssertWinnerIndex(trick, 1);
        }

        // L5 — Pirate in trick: Pirate beats numbers and all escape-like (Loot/Escape)
        [Fact]
        public void Case_L5()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.LILA, 14),
                new LootCard(),
                new PirateCard(PirateType.BENDT_THE_BANDIT), // Winner
            };
            AssertWinnerIndex(trick, 2);
        }

        // L6 — Skull King present (no Mermaid): Skull King wins; Loot irrelevant
        [Fact]
        public void Case_L6()
        {
            var trick = new List<Card>
            {
                new LootCard(),
                new PirateCard(PirateType.JUANITA_JADE),
                new SkullKingCard(), // Winner
            };
            AssertWinnerIndex(trick, 2);
        }

        // L7 — Mermaid + Pirate + Skull King combo (with Loot present): Mermaid wins
        [Fact]
        public void Case_L7()
        {
            var trick = new List<Card>
            {
                new LootCard(),
                new SkullKingCard(),
                new PirateCard(PirateType.HARRY_THE_GIANT),
                new MermaidCard(MermaidType.ALYRA), // Winner (combo rule)
            };
            AssertWinnerIndex(trick, 3);
        }

        // L8 — White Whale + only escape-like (Loot/Escape): FIRST escape-like wins
        [Fact]
        public void Case_L8()
        {
            var trick = new List<Card>
            {
                new WhiteWhaleCard(),
                new LootCard(),     // Winner (first escape-like after Whale)
                new EscapeCard(),
            };
            AssertWinnerIndex(trick, 1);
        }

        // L9 — White Whale + numbers + Loot: highest number wins (color ignored)
        [Fact]
        public void Case_L9()
        {
            var trick = new List<Card>
            {
                new WhiteWhaleCard(),
                new NumberCard(CardType.GREEN, 7),
                new NumberCard(CardType.YELLOW, 9),  // Winner (highest number overall)
                new LootCard(),
                new NumberCard(CardType.BLACK, 5),
            };
            AssertWinnerIndex(trick, 2);
        }

        // L10 — Kraken cancels regardless of Loot
        [Fact]
        public void Case_L10()
        {
            var trick = new List<Card>
            {
                new LootCard(),
                new KrakenCard(),
                new NumberCard(CardType.YELLOW, 14),
            };
            AssertWinnerIndex(trick, null);
        }

        // L11 — Multiple Loots mixed with numbers; normal suit resolution applies
        [Fact]
        public void Case_L11()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.GREEN, 8),
                new LootCard(),
                new LootCard(),
                new NumberCard(CardType.GREEN, 13), // Winner (lead suit, highest)
            };
            AssertWinnerIndex(trick, 3);
        }

        // L12 — Black trump still trumps colors; Loot does not affect
        [Fact]
        public void Case_L12()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.YELLOW, 14),
                new LootCard(),
                new NumberCard(CardType.BLACK, 1),  // Winner (black trumps colors)
            };
            AssertWinnerIndex(trick, 2);
        }

        // L13 — White Whale after numbers + tie on highest; earliest number wins; Loot irrelevant
        [Fact]
        public void Case_L13()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.YELLOW, 11), // Winner (tie on value; earliest number)
                new WhiteWhaleCard(),
                new NumberCard(CardType.LILA, 11),
                new LootCard(),
            };
            AssertWinnerIndex(trick, 0);
        }

        // L14 — White Whale with other specials present (Pirate) + Loot:
        // Whale nullifies → no winner (trick discarded)
        [Fact]
        public void Case_L14()
        {
            var trick = new List<Card>
            {
                new WhiteWhaleCard(),
                new LootCard(),
                new PirateCard(PirateType.RASCAL_OF_ROATAN),
            };
            AssertWinnerIndex(trick, null);
        }

        // L15 — Lead Loot; everyone else plays Escapes (Captain’s Log): leader wins, no alliance (resolver returns winner index)
        [Fact]
        public void Case_L15()
        {
            var trick = new List<Card>
            {
                new LootCard(),     // Winner (only escape-like cards played)
                new EscapeCard(),
                new EscapeCard(),
                new LootCard(),
            };
            AssertWinnerIndex(trick, 0);
        }

        // L16 - seen on Reddit
        [Fact]
        public void Case_L16()
        {
            var trick = new List<Card>
            {
                new PirateCard(PirateType.HARRY_THE_GIANT),
                new LootCard(),
                new WhiteWhaleCard(),
                new EscapeCard(),
            };
            AssertWinnerIndex(trick, null);
        }
    }
}
