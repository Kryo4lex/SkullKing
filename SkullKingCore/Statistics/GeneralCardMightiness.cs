using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Game;
using SkullKingCore.Logging;
using SkullKingCore.Test;

namespace SkullKingCore.Statistics
{
    public class GeneralCardMightiness
    {

        public GeneralCardMightiness()
        {

        }

        public void Test()
        {

            int decimalPlaces = 5;

            List<Card> allGameCards = new List<Card>();

            allGameCards = Deck.CreateDeck();

            Logger.Instance.WriteToConsoleAndLog("General Mightiness:");

            Logger.Instance.WriteToConsoleAndLog($"# Cards of Deck {allGameCards.Count}");

            float granularity = (1.0f / allGameCards.Count) * 100;

            Logger.Instance.WriteToConsoleAndLog($"Granulartiy: {granularity.ToString($"F{decimalPlaces}")} %");

            List<GeneralCardMightinessResult> generalCardsMightinessResult = new List<GeneralCardMightinessResult>();

            for (int allGameCardsCounter = 0; allGameCardsCounter < allGameCards.Count; allGameCardsCounter++)
            {

                List<Card> opposingCards = Deck.CreateDeck();

                opposingCards.RemoveAt(allGameCardsCounter);

                GeneralCardMightinessResult generalCardMightinessResult = new GeneralCardMightinessResult(allGameCards[allGameCardsCounter], opposingCards);

                generalCardsMightinessResult.Add(generalCardMightinessResult);

                generalCardMightinessResult.Test();

            }

            List<GeneralCardMightinessResult> sortedGeneralCardsMightinessResult = generalCardsMightinessResult.OrderByDescending(x => x.BestCaseProbability).ToList();

            PrintListFancy(sortedGeneralCardsMightinessResult, decimalPlaces);

        }

        public static void PrintListFancy(
            List<GeneralCardMightinessResult> sortedGeneralCardsMightinessResult,
            int decimalPlaces,
            string headerCardType = "Card Type",
            string headerSubType = "Sub Type",
            string headerBestCase = "Best Case (%)",
            string headerWorstCase = "Worst Case (%)")
        {
            // Determine max lengths based on content and headers
            int maxLengthCardType = Math.Max(headerCardType.Length, sortedGeneralCardsMightinessResult.Max(obj => obj.CardToTest.CardType.ToString().Length));
            int maxLengthSubType = Math.Max(headerSubType.Length, sortedGeneralCardsMightinessResult.Max(obj => obj.CardToTest.SubType().Length));
            int maxLengthBest = Math.Max(headerBestCase.Length, sortedGeneralCardsMightinessResult.Max(obj => obj.BestCaseProbability.ToString($"F{decimalPlaces}").Length));
            int maxLengthWorst = Math.Max(headerWorstCase.Length, sortedGeneralCardsMightinessResult.Max(obj => obj.WorstCaseProbability.ToString($"F{decimalPlaces}").Length));

            string separator = "+" +
                new string('-', maxLengthCardType + 2) + "+" +
                new string('-', maxLengthSubType + 2) + "+" +
                new string('-', maxLengthBest + 2) + "+" +
                new string('-', maxLengthWorst + 2) + "+";

            // Print the table header
            string headerLine = $"| {headerCardType.PadRight(maxLengthCardType)} " +
                                $"| {headerSubType.PadRight(maxLengthSubType)} " +
                                $"| {headerBestCase.PadLeft(maxLengthBest)} " +
                                $"| {headerWorstCase.PadLeft(maxLengthWorst)} |";

            Logger.Instance.WriteToConsoleAndLog(separator);
            Logger.Instance.WriteToConsoleAndLog(headerLine);
            Logger.Instance.WriteToConsoleAndLog(separator);

            // Print each data row
            foreach (var gcmr in sortedGeneralCardsMightinessResult)
            {
                string line = $"| {gcmr.CardToTest.CardType.ToString().PadRight(maxLengthCardType)} " +
                              $"| {gcmr.CardToTest.SubType().PadRight(maxLengthSubType)} " +
                              $"| {gcmr.BestCaseProbability.ToString($"F{decimalPlaces}").PadLeft(maxLengthBest)} " +
                              $"| {gcmr.WorstCaseProbability.ToString($"F{decimalPlaces}").PadLeft(maxLengthWorst)} |";
                Logger.Instance.WriteToConsoleAndLog(line);
            }

            Logger.Instance.WriteToConsoleAndLog(separator);
        }


        public class GeneralCardMightinessResult
        {

            public Card CardToTest { get; private set; }
            public List<Card> OpposingCards { get; private set; }

            public double BestCaseProbability { get; private set; }
            public double WorstCaseProbability { get; private set; }

            public GeneralCardMightinessResult(Card cardToTest, List<Card> opposingCards)
            {
                CardToTest = cardToTest;
                OpposingCards = opposingCards;
            }

            public void Test()
            {

                int currentCardVictoriesBestCase = 0;
                int currentCardVictoriesWorstCase = 0;

                for (int opposingCardCounter = 0; opposingCardCounter < OpposingCards.Count; opposingCardCounter++)
                {
                    TrickTest trickTestBestCase = new TrickTest("",
                    new List<Card>()
                    {
                        CardToTest,
                        OpposingCards[opposingCardCounter],
                    }
                    , 0);

                    trickTestBestCase.Test();

                    TrickTest trickWorstBestCase = new TrickTest("",
                    new List<Card>()
                    {
                        OpposingCards[opposingCardCounter],
                        CardToTest,
                    }
                    , 1);

                    trickWorstBestCase.Test();

                    if (trickTestBestCase.TestResult == TestResult.PASS)
                    {
                        currentCardVictoriesBestCase++;
                    }

                    if (trickWorstBestCase.TestResult == TestResult.PASS)
                    {
                        currentCardVictoriesWorstCase++;
                    }

                }

                WorstCaseProbability = (currentCardVictoriesWorstCase / (double)OpposingCards.Count) * 100.0;
                BestCaseProbability = (currentCardVictoriesBestCase / (double)OpposingCards.Count) * 100.0;
            }

        }

    }
}
