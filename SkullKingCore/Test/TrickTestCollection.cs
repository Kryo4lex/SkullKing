using SkullKingCore.Core.Cards;
using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.Implementations;
using SkullKingCore.Core.Cards.SubCardTypes;
using SkullKingCore.Logging;

namespace SkullKingCore.Test
{
    public class TrickTestCollection
    {

        public TestResult TotalTestResult { get; private set; } = TestResult.OPEN;

        public List<TrickTest> TrickTests { get; private set; }

        public TrickTestCollection()
        {
            TrickTests = new List<TrickTest>();
        }

        public void PrintTotalTestResult()
        {
            Logger.Instance.WriteToConsoleAndLog("TOTAL TEST RESULT:");
            Logger.Instance.WriteToConsoleAndLog($"{TotalTestResult}");
        }

        public void RunTestCases()
        {
            //This Test Cases are from the German Rule Book

            //Enums used in alphabetical order, or explictly used different one for distinction

            TrickTests = GenerateTrickTests();

            foreach (TrickTest trickTest in TrickTests)
            {
                trickTest.Test();
            }

            if(TrickTests.All(x => x.TestResult == TestResult.PASS))
            {
                TotalTestResult = TestResult.PASS;
            }
            else
            {
                TotalTestResult = TestResult.FAIL;
            }

        }

        public static List<TrickTest> GenerateTrickTests()
        {
            return
            new List<TrickTest>
            {
                //0x distinguishes Tricks of English rule book, which seems to be different from the German one...

                new TrickTest("0A", new List<Card>
                {
                    new NumberCard(CardType.GREEN,  14),
                    new NumberCard(CardType.YELLOW,  2),
                    new NumberCard(CardType.LILA,    6),
                    new NumberCard(CardType.BLACK,   4),//Winner, black is stronger than other suit/color cards
                }, 3),

                new TrickTest("0B", new List<Card>
                {
                    new NumberCard(CardType.YELLOW,  12),//Winner, leading color and highest value of colors
                    new NumberCard(CardType.YELLOW,   5),
                    new NumberCard(CardType.LILA,    14),
                }, 0),

                new TrickTest("0C", new List<Card>
                {
                    new NumberCard(CardType.YELLOW,  12),
                    new NumberCard(CardType.YELLOW,   5),
                    new NumberCard(CardType.BLACK,    2),//Winner, black is stronger than other suit/color cards
                }, 2),

                //"Farbkarten", page 8, bottom
                new TrickTest("1", new List<Card>
                {
                    new NumberCard(CardType.GREEN,  7),
                    new NumberCard(CardType.GREEN, 12),//Winner
                    new NumberCard(CardType.GREEN,  8),
                }, 1),

                //"Farbkarten", page 9, top
                new TrickTest("2", new List<Card>
                {
                    new NumberCard(CardType.YELLOW,  8),//Winner
                    new NumberCard(CardType.YELLOW,  5),
                    new NumberCard(CardType.LILA,   10),
                }, 0),

                //"Farbkarten", page 9, bottom
                new TrickTest("3", new List<Card>
                {
                    new NumberCard(CardType.YELLOW, 12),
                    new NumberCard(CardType.BLACK,   5),
                    new NumberCard(CardType.BLACK,   7),//Winner
                }, 2),

                //"Sonderkarten", page 10, "Flucht", Logbuch des Kapitäns
                //All cards are Escape cards (Flucht)
                new TrickTest("4", new List<Card>
                {
                    new EscapeCard(),//Winner, because first Escape card played
                    new EscapeCard(),
                    new EscapeCard(),
                }, 0),

                //"Sonderkarten", page 10, "Charaktere", "Piraten"
                //Pirates beat Escape, Color Card, incl. Trump, and Mermaids
                new TrickTest("5", new List<Card>
                {
                    new EscapeCard(),
                    new NumberCard(CardType.LILA,   14),
                    new NumberCard(CardType.GREEN,  14),
                    new NumberCard(CardType.YELLOW, 14),
                    new NumberCard(CardType.BLACK,  14),
                    new MermaidCard(MermaidType.ALYRA),
                    new PirateCard(PirateType.BENDT_THE_BANDIT),//Winner
                }, 6),

                //"Sonderkarten", page 10, "Charaktere", "Piraten"
                //All cards are Pirates
                new TrickTest("6", new List<Card>
                {
                    new PirateCard(PirateType.BENDT_THE_BANDIT),//Winner, because first Pirate card played
                    new PirateCard(PirateType.HARRY_THE_GIANT),
                    new PirateCard(PirateType.JUANITA_JADE),
                    new PirateCard(PirateType.RASCAL_OF_ROATAN),
                    new PirateCard(PirateType.ROSIE_D_LANEY),
                }, 0),

                //"Sonderkarten", page 11, "Charaktere", "Skull King"
                //The scourge of the seas is the trump of Pirates and beats all numbered cards and Pirates(including the Tigress, when played as a Pirate).
                new TrickTest("7", new List<Card>
                {
                    new EscapeCard(),
                    new NumberCard(CardType.LILA,   14),
                    new NumberCard(CardType.GREEN,  14),
                    new NumberCard(CardType.YELLOW, 14),
                    new NumberCard(CardType.BLACK,  14),
                    new PirateCard(PirateType.BENDT_THE_BANDIT),
                    new SkullKingCard(),//Winner
                }, 6),

                //"Sonderkarten", page 11, "Charaktere", "Skull King"
                //The only ones who can defeat him are the Mermaids, luring him into the sea with their precious treasure.
                new TrickTest("8", new List<Card>
                {
                    new EscapeCard(),
                    new NumberCard(CardType.LILA,   14),
                    new NumberCard(CardType.GREEN,  14),
                    new NumberCard(CardType.YELLOW, 14),
                    new NumberCard(CardType.BLACK,  14),
                    new SkullKingCard(),
                    new MermaidCard(MermaidType.ALYRA)//Winner
                }, 6),

                //"Sonderkarten", page 11, "Charaktere", "Mermaid"
                //Mermaids beat all numbered suits
                new TrickTest("9", new List<Card>
                {
                    new EscapeCard(),
                    new NumberCard(CardType.LILA,   14),
                    new NumberCard(CardType.GREEN,  14),
                    new NumberCard(CardType.YELLOW, 14),
                    new NumberCard(CardType.BLACK,  14),
                    new MermaidCard(MermaidType.SIRENA),//Winner
                }, 5),

                //"Sonderkarten", page 11, "Charaktere", "Mermaid"
                //...but lose to all of the Pirates
                new TrickTest("10", new List<Card>
                {
                    new EscapeCard(),
                    new NumberCard(CardType.LILA,   14),
                    new NumberCard(CardType.GREEN,  14),
                    new NumberCard(CardType.YELLOW, 14),
                    new NumberCard(CardType.BLACK,  14),
                    new MermaidCard(MermaidType.ALYRA),
                    new PirateCard(PirateType.BENDT_THE_BANDIT),//Winner
                }, 6),

                //"Sonderkarten", page 11, "Charaktere", "Mermaid"
                //If both Mermaids end up in the same trick, the first one played wins the trick.
                new TrickTest("11", new List<Card>
                {
                    new EscapeCard(),
                    new NumberCard(CardType.LILA,   14),
                    new NumberCard(CardType.GREEN,  14),
                    new NumberCard(CardType.YELLOW, 14),
                    new NumberCard(CardType.BLACK,  14),
                    new SkullKingCard(),
                    new MermaidCard(MermaidType.SIRENA),//Winner, first played
                    new MermaidCard(MermaidType.ALYRA),
                }, 6),

                //"Sonderkarten", page 11, "Charaktere", "Mermaid"
                //Captain’s Log: If a Pirate, the Skull King, and a Mermaid are all played in the same trick, the Mermaid always wins the trick, regardless of order of play. 
                new TrickTest("12", new List<Card>
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
                }, 7),

                // page 12, "Sonderkarten", "Sonderkarten anspielen", "Fluchtkarte anspielen"
                // page 12, "Special Cards, "Leading with Special Cards", "Leading with an Escape"
                new TrickTest("13", new List<Card>
                {
                    new EscapeCard(),
                    new NumberCard(CardType.LILA, 11),
                    new NumberCard(CardType.LILA,  6),
                }, 1),

                // page 12, "Special Cards, "Leading with Special Cards", "Leading with a Character"
                new TrickTest("14", new List<Card>
                {
                    new PirateCard(PirateType.JUANITA_JADE),//Winner
                    new NumberCard(CardType.GREEN, 9),
                    new NumberCard(CardType.LILA,  9),
                }, 0),

                new TrickTest("15", new List<Card>
                {
                    new MermaidCard(MermaidType.ALYRA),//Winner
                    new NumberCard(CardType.GREEN, 9),
                    new NumberCard(CardType.LILA,  9),
                }, 0),

                new TrickTest("16", new List<Card>
                {
                    new SkullKingCard(),//Winner
                    new NumberCard(CardType.GREEN, 9),
                    new NumberCard(CardType.LILA,  9),
                }, 0),

                new TrickTest("17", new List<Card>
                {
                    new WhiteWhaleCard(),
                    new NumberCard(CardType.GREEN, 9),//Winner, First played
                    new NumberCard(CardType.LILA,  9),
                }, 1),

                new TrickTest("18", new List<Card>
                {
                    new NumberCard(CardType.LILA,   8),
                    new NumberCard(CardType.BLACK, 10),//Winner, highest value
                    new WhiteWhaleCard(),
                    new NumberCard(CardType.LILA,   4),
                }, 1),

                // page 24, "Weißer Wal", example
                new TrickTest("19", new List<Card>
                {
                    new NumberCard(CardType.BLACK,   2),
                    new NumberCard(CardType.YELLOW, 14),//Winner, highest value
                    new SkullKingCard(),
                    new WhiteWhaleCard(),
                }, 1),

                //White Whale, but all played cards are only Special cards
                new TrickTest("20", new List<Card>
                {
                    new EscapeCard(),
                    new MermaidCard(MermaidType.ALYRA),
                    new PirateCard(PirateType.BENDT_THE_BANDIT),
                    new SkullKingCard(),
                    new WhiteWhaleCard(),
                }, null),//no winner

                new TrickTest("20", new List<Card>
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
                }, null),//no winner

                //White Whale first played before Kraken, but Kraken destroys
                new TrickTest("21", new List<Card>
                {
                    new WhiteWhaleCard(),
                    new KrakenCard(),
                }, null),

                //Kraken first played before White Whale, but White Whale destroys -> White Whale effect is applied
                new TrickTest("22", new List<Card>
                {
                    new NumberCard(CardType.GREEN,  14),
                    new NumberCard(CardType.LILA,   13),
                    new NumberCard(CardType.YELLOW, 12),
                    new NumberCard(CardType.BLACK,  11),
                    new KrakenCard(),
                    new WhiteWhaleCard(),
                }, 0),

                new TrickTest("23", new List<Card>
                {
                    new NumberCard(CardType.GREEN,   1),
                    new NumberCard(CardType.BLACK,   2),
                    new NumberCard(CardType.YELLOW, 11),//Winner, same highest number, but first played
                    new NumberCard(CardType.GREEN,  11),
                    new NumberCard(CardType.LILA,    1),
                    new WhiteWhaleCard(),
                }, 2),

                new TrickTest("24", new List<Card>
                {
                    new NumberCard(CardType.GREEN,  2),
                    new NumberCard(CardType.BLACK, 14),// Winner, brcause
                    new WhiteWhaleCard(),
                }, 1),

                new TrickTest("25", new List<Card>
                {
                    new NumberCard(CardType.GREEN, 14),
                    new NumberCard(CardType.BLACK,  1),// Winner, trump card
                }, 1),

                new TrickTest("26", new List<Card>
                {
                    new PirateCard(PirateType.BENDT_THE_BANDIT),// Winner
                    new MermaidCard(MermaidType.ALYRA),
                }, 0),

                new TrickTest("27", new List<Card>
                {
                    new SkullKingCard(),
                    new MermaidCard(MermaidType.ALYRA),// Winner
                }, 1),

                new TrickTest("28", new List<Card>
                {
                    new NumberCard(CardType.BLACK,   7),//Winner
                    new NumberCard(CardType.YELLOW,  8),
                    new NumberCard(CardType.BLACK,   4),
                    new NumberCard(CardType.YELLOW,  2),
                }, 0),

                new TrickTest("29", new List<Card>
                {
                    new NumberCard(CardType.YELLOW,  2),
                    new NumberCard(CardType.BLACK,   7),//Winner
                    new NumberCard(CardType.YELLOW,  8),
                    new NumberCard(CardType.BLACK,   4),
                }, 1),

                new TrickTest("30", new List<Card>
                {
                    new EscapeCard(),
                    new NumberCard(CardType.LILA,   1),
                    new NumberCard(CardType.LILA,   6),//Winner lead color and highest number
                    new NumberCard(CardType.GREEN, 14),
                }, 2),

                //Website:
                // Q: What happens when the Kraken and the White Whale are played in the same trick?
                // A: Whichever was played second retains its power. The first one played acts as an escape card. If the Kraken is played after the White Whale, the player who would have won the trick had neither creature been played, leads out the next trick.
                new TrickTest("31", new List<Card>
                {
                    new EscapeCard(),//Winner
                    new EscapeCard(),
                    new EscapeCard(),
                    new WhiteWhaleCard(),
                }, 0),

                //Rule Book:
                //If only special cards were played, then the trick is discarded (like the Kraken) and the person who played the White Whale is the next to lead.
                new TrickTest("32", new List<Card>
                {
                    new EscapeCard(),
                    new PirateCard(PirateType.BENDT_THE_BANDIT),
                    new MermaidCard(MermaidType.ALYRA),
                    new SkullKingCard(),
                    new WhiteWhaleCard(),
                }, null),//No Winner, trick is discarded

            };
        }

    }
}
