using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Game;
using SkullKingCore.Logging;

namespace SkullKingCore.Test
{
    public class TrickTest
    {

        public string TestCaseName { get; }

        public List<Card> Trick { get; }

        public int? ExpectedWinnerIndex { get; }

        public int? WinnerIndex { get; private set; }

        public Card? WinnerCard { get; private set; }

        public TestResult TestResult { get; private set; } = TestResult.OPEN;

        public TrickTest(string testCaseName, List<Card> trick, int? expectedWinnerIndex)
        { 
            TestCaseName = testCaseName;
            Trick = trick;
            ExpectedWinnerIndex = expectedWinnerIndex;
        }

        public void Test()
        {

            WinnerIndex = TrickResolver.GetWinningPlayerIndex(Trick);

            if (WinnerIndex != null)
            {
                WinnerCard = Trick[(int)WinnerIndex];
            }

            if(WinnerIndex == ExpectedWinnerIndex)
            {
                TestResult = TestResult.PASS;
            }
            else
            {
                TestResult = TestResult.FAIL;
            }

        }

        public static void PrintListFancy(List<TrickTest> trickTests)
        {
            if (trickTests == null || trickTests.Count == 0)
            {
                Logger.Instance.WriteToConsoleAndLog("No trick tests to display.");
                return;
            }

            // Headers
            string headerTestCase = "Test Case";
            string headerResult = "Result";
            string headerCardType = "Card Type";
            string headerSubType = "Sub Type";

            // Determine max lengths
            int maxLengthTestCase = Math.Max(headerTestCase.Length, trickTests.Max(t => t.TestCaseName?.Length ?? 0));
            int maxLengthResult = Math.Max(headerResult.Length, trickTests.Max(t => t.TestResult.ToString().Length));
            int maxLengthCardType = Math.Max(headerCardType.Length, trickTests.Max(t => t.WinnerCard?.CardType.ToString().Length ?? 0));
            int maxLengthSubType = Math.Max(headerSubType.Length, trickTests.Max(t => t.WinnerCard?.SubType()?.Length ?? 0));

            // Build separator line
            string separator = "+" +
                new string('-', maxLengthTestCase + 2) + "+" +
                new string('-', maxLengthResult + 2) + "+" +
                new string('-', maxLengthCardType + 2) + "+" +
                new string('-', maxLengthSubType + 2) + "+";

            // Header line
            string headerLine = $"| {headerTestCase.PadRight(maxLengthTestCase)} | " +
                                $"{headerResult.PadRight(maxLengthResult)} | " +
                                $"{headerCardType.PadRight(maxLengthCardType)} | " +
                                $"{headerSubType.PadRight(maxLengthSubType)} |";

            Logger.Instance.WriteToConsoleAndLog(separator);
            Logger.Instance.WriteToConsoleAndLog(headerLine);
            Logger.Instance.WriteToConsoleAndLog(separator);

            // Print each row
            foreach (var trickTest in trickTests)
            {
                string testCaseName = trickTest.TestCaseName ?? "";
                string testResult = trickTest.TestResult.ToString();
                string cardType = trickTest.WinnerCard?.CardType.ToString() ?? "(none)";
                string subType = trickTest.WinnerCard?.SubType() ?? "(none)";

                string row = $"| {testCaseName.PadRight(maxLengthTestCase)} | " +
                             $"{testResult.PadRight(maxLengthResult)} | " +
                             $"{cardType.PadRight(maxLengthCardType)} | " +
                             $"{subType.PadRight(maxLengthSubType)} |";

                Logger.Instance.WriteToConsoleAndLog(row);
            }

            Logger.Instance.WriteToConsoleAndLog(separator);
        }

    }
}
