using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Game;
using Xunit.Abstractions;

namespace SkullKingUnitTests.WinningPlayerTests
{
    public class WinningPlayerTests
    {
        private readonly ITestOutputHelper _output;
        public WinningPlayerTests(ITestOutputHelper output) => _output = output;

        [Theory] // let xUnit show the arg in the test title
        [MemberData(nameof(TrickTestCases.Rows),
                    MemberType = typeof(TrickTestCases),
                    DisableDiscoveryEnumeration = false)]
        public void GetWinningPlayerIndex_Case(string testCaseName, List<Card> trick, int? expectedWinnerIndex)
        {
            // Use the parameter so xUnit1026 is satisfied, and you see it in test output
            _output.WriteLine($"Case: {testCaseName}");

            var winner = TrickResolver.GetWinningPlayerIndex(new List<Card>(trick));
            Assert.Equal(expectedWinnerIndex, winner);
        }
    }
}
