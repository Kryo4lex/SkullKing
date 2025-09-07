using SkullKingCore.Core.Cards.Base;
using SkullKingCore.Core.Game;
using SkullKingCore.Logging;
namespace SkullKingSandboxConsole.Commands
{
    public static class CommandCardNameGen
    {

        public static void Run()
        {

            List<Card> deck = Deck.CreateDeck();

            foreach (Card card in deck)
            {
                string totalName = "";

                totalName = card.CardType.ToString();

                if (!string.IsNullOrEmpty(card.SubType().ToString()))
                {
                    totalName += "_" + card.SubType().ToString();
                }

                Logger.Instance.WriteToConsoleAndLog(totalName);
            }

        }

    }
}
