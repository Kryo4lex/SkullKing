using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Game;
using Xunit.Abstractions;

namespace SkullKingUnitTests.BonusPointsTests
{
    public class BonusPointsTests
    {
        private readonly ITestOutputHelper _output;
        public BonusPointsTests(ITestOutputHelper output) => _output = output;

        [Theory] // let xUnit show the arg in the test title
        [MemberData(nameof(BonusPointsTestCases.Rows),
                    MemberType = typeof(BonusPointsTestCases),
                    DisableDiscoveryEnumeration = false)]
        public void ComputeTrickBonus_Case(string testCaseName, List<Card> trick, int expectedBonusPoints, int? winnerIndex)
        {
            _output.WriteLine($"Case: {testCaseName}");

            //string testCaseName, List<Card> trick, int expectedBonusPoints, int winnerIndex

            int bonuspoints = TrickBonusPointResolver.ComputeTrickBonus(new List<Card>(trick), winnerIndex);
            Assert.Equal(expectedBonusPoints, bonuspoints);
        }
    }
}
