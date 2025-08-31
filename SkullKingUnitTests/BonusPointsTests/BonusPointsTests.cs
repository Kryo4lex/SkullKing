using SkullKingCore.Core.Cards;
using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.Implementations;
using SkullKingCore.Core.Cards.SubCardTypes;
using SkullKingCore.Core.Game;

namespace SkullKingUnitTests.BonusPointsTests
{
    /// <summary>
    /// Stand-alone Facts that validate TrickBonusPointResolver.ComputeTrickBonus, no loot tracker.
    /// </summary>
    public class BonusPointsTests
    {
        // Simple helper so each fact stays tiny
        private static void AssertTrickBonus(
            string caseName,
            List<Card> trick,
            int? winnerIndex,
            int expectedBonus)
        {
            int actual = TrickBonusPointResolver.ComputeTrickBonus(trick, winnerIndex);
            Assert.Equal(expectedBonus, actual);
        }

        [Fact]
        public void Case_1()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.GREEN,  14),
                new NumberCard(CardType.YELLOW, 2),
                new NumberCard(CardType.LILA,   6),
                new NumberCard(CardType.BLACK,  4),
            };
            // expectedBonusPoints: 10, winnerIndex: 3
            AssertTrickBonus("1", trick, 3, 10);
        }

        [Fact]
        public void Case_2()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.YELLOW, 14),
                new NumberCard(CardType.YELLOW,  2),
                new NumberCard(CardType.LILA,    6),
                new NumberCard(CardType.BLACK,   4),
            };
            AssertTrickBonus("2", trick, 3, 10);
        }

        [Fact]
        public void Case_3()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.LILA,   14),
                new NumberCard(CardType.YELLOW,  2),
                new NumberCard(CardType.LILA,    6),
                new NumberCard(CardType.BLACK,   4),
            };
            AssertTrickBonus("3", trick, 3, 10);
        }

        [Fact]
        public void Case_4()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.BLACK,  14),
                new NumberCard(CardType.YELLOW,  2),
                new NumberCard(CardType.LILA,    6),
                new SkullKingCard(),
            };
            AssertTrickBonus("4", trick, 3, 20);
        }

        [Fact]
        public void Case_5()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.GREEN,   1),
                new NumberCard(CardType.YELLOW,  2),
                new NumberCard(CardType.LILA,    6),
                new NumberCard(CardType.GREEN,  14),
            };
            AssertTrickBonus("5", trick, 3, 10);
        }

        [Fact]
        public void Case_6()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.GREEN,   1),
                new NumberCard(CardType.YELLOW,  2),
                new NumberCard(CardType.LILA,    6),
                new NumberCard(CardType.BLACK,  14),
            };
            AssertTrickBonus("6", trick, 3, 20);
        }

        [Fact]
        public void Case_7()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.YELLOW,  14),
                new PirateCard(PirateType.BENDT_THE_BANDIT),
                new SkullKingCard(),
                new MermaidCard(MermaidType.ALYRA),
            };
            // rule book first example: Mermaid wins (winnerIndex:3), +50 for SK captured
            AssertTrickBonus("7", trick, 3, 50);
        }

        [Fact]
        public void Case_8()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.GREEN,  14),
                new NumberCard(CardType.YELLOW,  2),
                new NumberCard(CardType.LILA,    6),
                new NumberCard(CardType.BLACK,  14),
                new WhiteWhaleCard(),
            };
            // even with White Whale, bonus is counted for 14s present: 10 (G14) + 20 (B14) = 30
            AssertTrickBonus("8", trick, 3, 30);
        }

        [Fact]
        public void Case_9()
        {
            var trick = new List<Card>
            {
                new NumberCard(CardType.YELLOW,  14),
                new PirateCard(PirateType.BENDT_THE_BANDIT),
                new SkullKingCard(),
                new MermaidCard(MermaidType.ALYRA),
                new KrakenCard(),
            };
            // Kraken cancels trick: winnerIndex = null => 0 bonus
            AssertTrickBonus("9", trick, null, 0);
        }
    }
}
