using SkullKingCore.Core.Cards.Base;

namespace SkullKingUnitTests.BonusPointsTests
{
    public class BonusPointsTest
    {
        public string TestCaseName { get; }
        public List<Card> Trick { get; }
        public int ExpectedBonusPoints { get; }
        public int? WinnerIndex { get; }

        public BonusPointsTest(string testCaseName, List<Card> trick, int expectedBonusPoints, int? winnerIndex)
        {
            TestCaseName = testCaseName ?? throw new ArgumentNullException(nameof(testCaseName));
            Trick = trick ?? throw new ArgumentNullException(nameof(trick));
            ExpectedBonusPoints = expectedBonusPoints;
            WinnerIndex = winnerIndex;
        }

        public override string ToString() => TestCaseName; // nice display if you use ClassData later
    }

}
