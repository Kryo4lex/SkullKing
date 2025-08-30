using SkullKingCore.Core.Cards;
using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Cards.Implementations;
using SkullKingCore.Core.Cards.SubCardTypes;

namespace SkullKingUnitTests.BonusPointsTests
{
    public class BonusPointsTestCases
    {

        public static IEnumerable<object?[]> Rows()
        {
            foreach (var b in All())
                yield return new object?[] { b.TestCaseName, b.Trick, b.ExpectedBonusPoints, b.WinnerIndex };
        }

        // Strongly-typed, allocation-light, xUnit-friendly
        public static IEnumerable<BonusPointsTest> All()
        {

            yield return new BonusPointsTest("1", new List<Card>
            {
                new NumberCard(CardType.GREEN,  14),
                new NumberCard(CardType.YELLOW,  2),
                new NumberCard(CardType.LILA,    6),
                new NumberCard(CardType.BLACK,   4),
            }, expectedBonusPoints: 10, winnerIndex: 3);

            yield return new BonusPointsTest("2", new List<Card>
            {
                new NumberCard(CardType.YELLOW,  14),
                new NumberCard(CardType.YELLOW,  2),
                new NumberCard(CardType.LILA,    6),
                new NumberCard(CardType.BLACK,   4),
            }, expectedBonusPoints: 10, winnerIndex: 3);

            yield return new BonusPointsTest("3", new List<Card>
            {
                new NumberCard(CardType.LILA,  14),
                new NumberCard(CardType.YELLOW,  2),
                new NumberCard(CardType.LILA,    6),
                new NumberCard(CardType.BLACK,   4),
            }, expectedBonusPoints: 10, winnerIndex: 3);

            yield return new BonusPointsTest("4", new List<Card>
            {
                new NumberCard(CardType.BLACK,  14),
                new NumberCard(CardType.YELLOW,  2),
                new NumberCard(CardType.LILA,    6),
                new SkullKingCard(),
            }, expectedBonusPoints: 20, winnerIndex: 3);

            //get even points by your own
            //Captain’s Log: Every 14 you have at the end of the round
            //earns you a bonus, whether played by you or an opponent.

            yield return new BonusPointsTest("5", new List<Card>
            {
                new NumberCard(CardType.GREEN,   1),
                new NumberCard(CardType.YELLOW,  2),
                new NumberCard(CardType.LILA,    6),
                new NumberCard(CardType.GREEN,  14),
            }, expectedBonusPoints: 10, winnerIndex: 3);

            yield return new BonusPointsTest("6", new List<Card>
            {
                new NumberCard(CardType.GREEN,   1),
                new NumberCard(CardType.YELLOW,  2),
                new NumberCard(CardType.LILA,    6),
                new NumberCard(CardType.BLACK,  14),
            }, expectedBonusPoints: 20, winnerIndex: 3);

            //rule book first example
            yield return new BonusPointsTest("7", new List<Card>
            {
                new NumberCard(CardType.YELLOW,  14),
                new PirateCard(PirateType.BENDT_THE_BANDIT),
                new SkullKingCard(),
                new MermaidCard(MermaidType.ALYRA),
            }, expectedBonusPoints: 50, winnerIndex: 3);

            //even with White Whale award Bonus Points
            yield return new BonusPointsTest("8", new List<Card>
            {
                new NumberCard(CardType.GREEN,  14),
                new NumberCard(CardType.YELLOW,  2),
                new NumberCard(CardType.LILA,    6),
                new NumberCard(CardType.BLACK,  14),
                new WhiteWhaleCard(),
            }, expectedBonusPoints: 30, winnerIndex: 3);

            yield return new BonusPointsTest("9", new List<Card>
            {
                new NumberCard(CardType.YELLOW,  14),
                new PirateCard(PirateType.BENDT_THE_BANDIT),
                new SkullKingCard(),
                new MermaidCard(MermaidType.ALYRA),
                new KrakenCard(),
            }, expectedBonusPoints: 0, winnerIndex: null);
        }
    }
}
