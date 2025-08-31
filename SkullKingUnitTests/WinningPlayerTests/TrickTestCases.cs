using SkullKingCore.Core.Cards;
using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.Implementations;
using SkullKingCore.Core.Cards.SubCardTypes;
using SkullKingUnitTests.BonusPointsTests;

namespace SkullKingUnitTests.WinningPlayerTests
{
    public class TrickTestCases
    {

        public static IEnumerable<object?[]> Rows()
        {
            foreach (var t in All())
                yield return new object?[] { t.TestCaseName, t.Trick, t.ExpectedWinnerIndex };
        }


        // Strongly-typed, allocation-light, xUnit-friendly
        public static IEnumerable<TrickTest> All()
        {

            //0x distinguishes Tricks of English rule book, which seems to be different from the German one...

            yield return new TrickTest("0A", new List<Card>
            {
                new NumberCard(CardType.GREEN,  14),
                new NumberCard(CardType.YELLOW,  2),
                new NumberCard(CardType.LILA,    6),
                new NumberCard(CardType.BLACK,   4),//Winner, black is stronger than other suit/color cards
            }, 3);

            yield return new TrickTest("0B", new List<Card>
            {
                new NumberCard(CardType.YELLOW,  12),//Winner, leading color and highest value of colors
                new NumberCard(CardType.YELLOW,   5),
                new NumberCard(CardType.LILA,    14),
            }, 0);

            yield return new TrickTest("0C", new List<Card>
            {
                new NumberCard(CardType.YELLOW,  12),
                new NumberCard(CardType.YELLOW,   5),
                new NumberCard(CardType.BLACK,    2),//Winner, black is stronger than other suit/color cards
            }, 2);

            //"Farbkarten", page 8, bottom
            yield return new TrickTest("1", new List<Card>
            {
                new NumberCard(CardType.GREEN,  7),
                new NumberCard(CardType.GREEN, 12),//Winner
                new NumberCard(CardType.GREEN,  8),
            }, 1);

            //"Farbkarten", page 9, top
            yield return new TrickTest("2", new List<Card>
            {
                new NumberCard(CardType.YELLOW,  8),//Winner
                new NumberCard(CardType.YELLOW,  5),
                new NumberCard(CardType.LILA,   10),
            }, 0);

            //"Farbkarten", page 9, bottom
            yield return new TrickTest("3", new List<Card>
            {
                new NumberCard(CardType.YELLOW, 12),
                new NumberCard(CardType.BLACK,   5),
                new NumberCard(CardType.BLACK,   7),//Winner
            }, 2);

            //"Sonderkarten", page 10, "Flucht", Logbuch des Kapitäns
            //All cards are Escape cards (Flucht)
            yield return new TrickTest("4", new List<Card>
            {
                new EscapeCard(),//Winner, because first Escape card played
                new EscapeCard(),
                new EscapeCard(),
            }, 0);

            //"Sonderkarten", page 10, "Charaktere", "Piraten"
            //Pirates beat Escape, Color Card, incl. Trump, and Mermaids
            yield return new TrickTest("5", new List<Card>
            {
                new EscapeCard(),
                new NumberCard(CardType.LILA,   14),
                new NumberCard(CardType.GREEN,  14),
                new NumberCard(CardType.YELLOW, 14),
                new NumberCard(CardType.BLACK,  14),
                new MermaidCard(MermaidType.ALYRA),
                new PirateCard(PirateType.BENDT_THE_BANDIT),//Winner
            }, 6);

            //"Sonderkarten", page 10, "Charaktere", "Piraten"
            //All cards are Pirates
            yield return new TrickTest("6", new List<Card>
            {
                new PirateCard(PirateType.BENDT_THE_BANDIT),//Winner, because first Pirate card played
                new PirateCard(PirateType.HARRY_THE_GIANT),
                new PirateCard(PirateType.JUANITA_JADE),
                new PirateCard(PirateType.RASCAL_OF_ROATAN),
                new PirateCard(PirateType.ROSIE_D_LANEY),
            }, 0);

            //"Sonderkarten", page 11, "Charaktere", "Skull King"
            //The scourge of the seas is the trump of Pirates and beats all numbered cards and Pirates(including the Tigress, when played as a Pirate).
            yield return new TrickTest("7", new List<Card>
            {
                new EscapeCard(),
                new NumberCard(CardType.LILA,   14),
                new NumberCard(CardType.GREEN,  14),
                new NumberCard(CardType.YELLOW, 14),
                new NumberCard(CardType.BLACK,  14),
                new PirateCard(PirateType.BENDT_THE_BANDIT),
                new SkullKingCard(),//Winner
            }, 6);

            //"Sonderkarten", page 11, "Charaktere", "Skull King"
            //The only ones who can defeat him are the Mermaids, luring him into the sea with their precious treasure.
            yield return new TrickTest("8", new List<Card>
            {
                new EscapeCard(),
                new NumberCard(CardType.LILA,   14),
                new NumberCard(CardType.GREEN,  14),
                new NumberCard(CardType.YELLOW, 14),
                new NumberCard(CardType.BLACK,  14),
                new SkullKingCard(),
                new MermaidCard(MermaidType.ALYRA)//Winner
            }, 6);

            //"Sonderkarten", page 11, "Charaktere", "Mermaid"
            //Mermaids beat all numbered suits
            yield return new TrickTest("9", new List<Card>
            {
                new EscapeCard(),
                new NumberCard(CardType.LILA,   14),
                new NumberCard(CardType.GREEN,  14),
                new NumberCard(CardType.YELLOW, 14),
                new NumberCard(CardType.BLACK,  14),
                new MermaidCard(MermaidType.SIRENA),//Winner
            }, 5);

            //"Sonderkarten", page 11, "Charaktere", "Mermaid"
            //...but lose to all of the Pirates
            yield return new TrickTest("10", new List<Card>
            {
                new EscapeCard(),
                new NumberCard(CardType.LILA,   14),
                new NumberCard(CardType.GREEN,  14),
                new NumberCard(CardType.YELLOW, 14),
                new NumberCard(CardType.BLACK,  14),
                new MermaidCard(MermaidType.ALYRA),
                new PirateCard(PirateType.BENDT_THE_BANDIT),//Winner
            }, 6);

            //"Sonderkarten", page 11, "Charaktere", "Mermaid"
            //If both Mermaids end up in the same trick, the first one played wins the trick.
            yield return new TrickTest("11", new List<Card>
            {
                new EscapeCard(),
                new NumberCard(CardType.LILA,   14),
                new NumberCard(CardType.GREEN,  14),
                new NumberCard(CardType.YELLOW, 14),
                new NumberCard(CardType.BLACK,  14),
                new SkullKingCard(),
                new MermaidCard(MermaidType.SIRENA),//Winner, first played
                new MermaidCard(MermaidType.ALYRA),
            }, 6);

            //"Sonderkarten", page 11, "Charaktere", "Mermaid"
            //Captain’s Log: If a Pirate, the Skull King, and a Mermaid are all played in the same trick, the Mermaid always wins the trick, regardless of order of play. 
            yield return new TrickTest("12", new List<Card>
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
            }, 7);

            // page 12, "Sonderkarten", "Sonderkarten anspielen", "Fluchtkarte anspielen"
            // page 12, "Special Cards, "Leading with Special Cards", "Leading with an Escape"
            yield return new TrickTest("13", new List<Card>
            {
                new EscapeCard(),
                new NumberCard(CardType.LILA, 11),
                new NumberCard(CardType.LILA,  6),
            }, 1);

            // page 12, "Special Cards, "Leading with Special Cards", "Leading with a Character"
            yield return new TrickTest("14", new List<Card>
            {
                new PirateCard(PirateType.JUANITA_JADE),//Winner
                new NumberCard(CardType.GREEN, 9),
                new NumberCard(CardType.LILA,  9),
            }, 0);

            yield return new TrickTest("15", new List<Card>
            {
                new MermaidCard(MermaidType.ALYRA),//Winner
                new NumberCard(CardType.GREEN, 9),
                new NumberCard(CardType.LILA,  9),
            }, 0);

            yield return new TrickTest("16", new List<Card>
            {
                new SkullKingCard(),//Winner
                new NumberCard(CardType.GREEN, 9),
                new NumberCard(CardType.LILA,  9),
            }, 0);

            yield return new TrickTest("17", new List<Card>
            {
                new WhiteWhaleCard(),
                new NumberCard(CardType.GREEN, 9),//Winner, First played
                new NumberCard(CardType.LILA,  9),
            }, 1);

            yield return new TrickTest("18", new List<Card>
            {
                new NumberCard(CardType.LILA,   8),
                new NumberCard(CardType.BLACK, 10),//Winner, highest value
                new WhiteWhaleCard(),
                new NumberCard(CardType.LILA,   4),
            }, 1);

            // page 24, "Weißer Wal", example
            yield return new TrickTest("19", new List<Card>
            {
                new NumberCard(CardType.BLACK,   2),
                new NumberCard(CardType.YELLOW, 14),//Winner, highest value
                new SkullKingCard(),
                new WhiteWhaleCard(),
            }, 1);

            //White Whale, but all played cards are only Special cards
            yield return new TrickTest("20A", new List<Card>
            {
                new EscapeCard(),
                new MermaidCard(MermaidType.ALYRA),
                new PirateCard(PirateType.BENDT_THE_BANDIT),
                new SkullKingCard(),
                new WhiteWhaleCard(),
            }, null);//no winner

            yield return new TrickTest("20B", new List<Card>
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
            }, null);//no winner

            //White Whale first played before Kraken, but Kraken destroys
            yield return new TrickTest("21", new List<Card>
            {
                new WhiteWhaleCard(),
                new KrakenCard(),
            }, null);

            //Kraken first played before White Whale, but White Whale destroys -> White Whale effect is applied
            yield return new TrickTest("22", new List<Card>
            {
                new NumberCard(CardType.GREEN,  14),
                new NumberCard(CardType.LILA,   13),
                new NumberCard(CardType.YELLOW, 12),
                new NumberCard(CardType.BLACK,  11),
                new KrakenCard(),
                new WhiteWhaleCard(),
            }, 0);

            yield return new TrickTest("23", new List<Card>
            {
                new NumberCard(CardType.GREEN,   1),
                new NumberCard(CardType.BLACK,   2),
                new NumberCard(CardType.YELLOW, 11),//Winner, same highest number, but first played
                new NumberCard(CardType.GREEN,  11),
                new NumberCard(CardType.LILA,    1),
                new WhiteWhaleCard(),
            }, 2);

            yield return new TrickTest("24", new List<Card>
            {
                new NumberCard(CardType.GREEN,  2),
                new NumberCard(CardType.BLACK, 14),// Winner, brcause
                new WhiteWhaleCard(),
            }, 1);

            yield return new TrickTest("25", new List<Card>
            {
                new NumberCard(CardType.GREEN, 14),
                new NumberCard(CardType.BLACK,  1),// Winner, trump card
            }, 1);

            yield return new TrickTest("26", new List<Card>
            {
                new PirateCard(PirateType.BENDT_THE_BANDIT),// Winner
                new MermaidCard(MermaidType.ALYRA),
            }, 0);

            yield return new TrickTest("27", new List<Card>
            {
                new SkullKingCard(),
                new MermaidCard(MermaidType.ALYRA),// Winner
            }, 1);

            yield return new TrickTest("28", new List<Card>
            {
                new NumberCard(CardType.BLACK,   7),//Winner
                new NumberCard(CardType.YELLOW,  8),
                new NumberCard(CardType.BLACK,   4),
                new NumberCard(CardType.YELLOW,  2),
            }, 0);

            yield return new TrickTest("29", new List<Card>
            {
                new NumberCard(CardType.YELLOW,  2),
                new NumberCard(CardType.BLACK,   7),//Winner
                new NumberCard(CardType.YELLOW,  8),
                new NumberCard(CardType.BLACK,   4),
            }, 1);

            yield return new TrickTest("30", new List<Card>
            {
                new EscapeCard(),
                new NumberCard(CardType.LILA,   1),
                new NumberCard(CardType.LILA,   6),//Winner lead color and highest number
                new NumberCard(CardType.GREEN, 14),
            }, 2);

            //Website:
            // Q: What happens when the Kraken and the White Whale are played in the same trick?
            // A: Whichever was played second retains its power. The first one played acts as an escape card. If the Kraken is played after the White Whale, the player who would have won the trick had neither creature been played, leads out the next trick.
            yield return new TrickTest("31", new List<Card>
            {
                new EscapeCard(),//Winner
                new EscapeCard(),
                new EscapeCard(),
                new WhiteWhaleCard(),
            }, 0);

            //Rule Book:
            //If only special cards were played, then the trick is discarded (like the Kraken) and the person who played the White Whale is the next to lead.
            yield return new TrickTest("32", new List<Card>
            {
                new EscapeCard(),
                new PirateCard(PirateType.BENDT_THE_BANDIT),
                new MermaidCard(MermaidType.ALYRA),
                new SkullKingCard(),
                new WhiteWhaleCard(),
            }, null);//No Winner, trick is discarded

            yield return new TrickTest("33", new List<Card>
            {
                new SkullKingCard(),
                new SkullKingCard(),
                new MermaidCard(MermaidType.ALYRA),//Winner, must beat also multiple cards
                new SkullKingCard(),
                new SkullKingCard(),
            }, 2);

            yield return new TrickTest("34", new List<Card>
            {
                new NumberCard(CardType.GREEN,   1),
                new NumberCard(CardType.YELLOW,  2),
                new NumberCard(CardType.LILA,    6),
                new NumberCard(CardType.GREEN,  14),
            }, 3);

            //first played would win
            yield return new TrickTest("35", new List<Card>
            {
                new NumberCard(CardType.GREEN,   1),
                new NumberCard(CardType.YELLOW,  2),
                new NumberCard(CardType.LILA,    6),
                new NumberCard(CardType.GREEN,  14),
                new NumberCard(CardType.GREEN,  14),
            }, 3);

            // L1 — All escape-like (Loot counts as Escape): first played wins (Captain’s Log-compatible)
            yield return new TrickTest("L1", new List<Card>
            {
                new LootCard(),     // Winner (first escape-like)
                new EscapeCard(),
                new LootCard(),
            }, 0);

            // L2 — First is Escape, then only Loots: still first escape-like wins
            yield return new TrickTest("L2", new List<Card>
            {
                new EscapeCard(),   // Winner (first escape-like)
                new LootCard(),
                new LootCard(),
            }, 0);

            // L3 — Numbers present; Loot is ignored for ranking; follow lead suit
            yield return new TrickTest("L3", new List<Card>
            {
                new NumberCard(CardType.YELLOW, 10),
                new LootCard(),
                new NumberCard(CardType.YELLOW, 12), // Winner (lead suit, highest)
            }, 2);

            // L4 — Lead Loot; first number played sets suit; off-suit higher number does NOT win
            yield return new TrickTest("L4", new List<Card>
            {
                new LootCard(),
                new NumberCard(CardType.YELLOW, 11), // Winner (lead suit established here)
                new NumberCard(CardType.GREEN, 14),
                new NumberCard(CardType.YELLOW, 9),
            }, 1);

            // L5 — Pirate in trick: Pirate beats numbers and all escape-like (Loot/Escape)
            yield return new TrickTest("L5", new List<Card>
            {
                new NumberCard(CardType.LILA, 14),
                new LootCard(),
                new PirateCard(PirateType.BENDT_THE_BANDIT), // Winner
            }, 2);

            // L6 — Skull King present (no Mermaid): Skull King wins; Loot irrelevant
            yield return new TrickTest("L6", new List<Card>
            {
                new LootCard(),
                new PirateCard(PirateType.JUANITA_JADE),
                new SkullKingCard(), // Winner
            }, 2);

            // L7 — Mermaid + Pirate + Skull King combo (with Loot present): Mermaid wins
            yield return new TrickTest("L7", new List<Card>
            {
                new LootCard(),
                new SkullKingCard(),
                new PirateCard(PirateType.HARRY_THE_GIANT),
                new MermaidCard(MermaidType.ALYRA), // Winner (combo rule)
            }, 3);

            // L8 — White Whale + only escape-like (Loot/Escape): FIRST escape-like wins
            yield return new TrickTest("L8", new List<Card>
            {
                new WhiteWhaleCard(),
                new LootCard(),     // Winner (first escape-like after Whale)
                new EscapeCard(),
            }, 1);

            // L9 — White Whale + numbers + Loot: highest number wins (color ignored)
            yield return new TrickTest("L9", new List<Card>
            {
                new WhiteWhaleCard(),
                new NumberCard(CardType.GREEN, 7),
                new NumberCard(CardType.YELLOW, 9),  // Winner (highest number overall)
                new LootCard(),
                new NumberCard(CardType.BLACK, 5),
            }, 2);

            // L10 — Kraken cancels regardless of Loot
            yield return new TrickTest("L10", new List<Card>
            {
                new LootCard(),
                new KrakenCard(),
                new NumberCard(CardType.YELLOW, 14),
            }, null);

            // L11 — Multiple Loots mixed with numbers; normal suit resolution applies
            yield return new TrickTest("L11", new List<Card>
            {
                new NumberCard(CardType.GREEN, 8),
                new LootCard(),
                new LootCard(),
                new NumberCard(CardType.GREEN, 13), // Winner (lead suit, highest)
            }, 3);

            // L12 — Black trump still trumps colors; Loot does not affect
            yield return new TrickTest("L12", new List<Card>
            {
                new NumberCard(CardType.YELLOW, 14),
                new LootCard(),
                new NumberCard(CardType.BLACK, 1),  // Winner (black trumps colors)
            }, 2);

            // L13 — White Whale after numbers + tie on highest; earliest number wins; Loot irrelevant
            yield return new TrickTest("L13", new List<Card>
            {
                new NumberCard(CardType.YELLOW, 11), // Winner (tie on value; earliest number)
                new WhiteWhaleCard(),
                new NumberCard(CardType.LILA, 11),
                new LootCard(),
            }, 0);

            // L14 — White Whale with other specials present (Pirate) + Loot:
            // Whale nullifies → no winner (trick discarded)
            yield return new TrickTest("L14", new List<Card>
            {
                new WhiteWhaleCard(),
                new LootCard(),
                new PirateCard(PirateType.RASCAL_OF_ROATAN),
            }, null);

            // L15 — Lead Loot; everyone else plays Escapes (Captain’s Log): leader wins, no alliance (resolver returns winner index)
            yield return new TrickTest("L15", new List<Card>
            {
                new LootCard(),     // Winner (only escape-like cards played)
                new EscapeCard(),
                new EscapeCard(),
                new LootCard(),
            }, 0);

            // L16 - seen on Reddit
            yield return new TrickTest("L16", new List<Card>
            {
                new PirateCard(PirateType.HARRY_THE_GIANT),
                new LootCard(),
                new WhiteWhaleCard(),
                new EscapeCard(),
            }, null);

        }

    }
}
