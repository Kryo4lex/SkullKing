using SkullKingCore.Core.Cards.Base;

namespace SkullKingUnitTests.WinningPlayerTests
{
    public class TrickTest
    {
        public string TestCaseName { get; }
        public List<Card> Trick { get; }
        public int? ExpectedWinnerIndex { get; }

        public TrickTest(string testCaseName, List<Card> trick, int? expectedWinnerIndex)
        {
            TestCaseName = testCaseName ?? throw new ArgumentNullException(nameof(testCaseName));
            Trick = trick ?? throw new ArgumentNullException(nameof(trick));
            ExpectedWinnerIndex = expectedWinnerIndex;
        }

        public override string ToString() => TestCaseName; // nice display if you use ClassData later
    }

}
