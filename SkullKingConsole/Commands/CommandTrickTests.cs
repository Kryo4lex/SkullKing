using SkullKingCore.Test;

namespace SkullKingConsole.Commands
{
    public static class CommandTrickTests
    {

        public static void Run()
        {

            TrickTestCollection trickTestCollection = new TrickTestCollection();

            trickTestCollection.RunTestCases();

            TrickTest.PrintListFancy(trickTestCollection.TrickTests);

            trickTestCollection.PrintTotalTestResult();

        }

    }
}
