using SkullKingCore.Logging;

namespace SkullKingCore.Utility.UserInput
{
    public static class UserInput
    {

        public static bool TryReadInt(string prompt, out int result, int lowerLimit = int.MinValue, int upperLimit = int.MaxValue)
        {
            while (true)
            {
                //Logger.Instance.WriteToConsoleAndLog($"{prompt} (or 'E' to cancel): ");
                Logger.Instance.WriteToConsoleAndLog($"{prompt}");

                string? input = Console.ReadLine();

                if (string.Equals(input, "E", StringComparison.OrdinalIgnoreCase))
                {
                    result = 0;
                    return false; // User aborted
                }

                if (int.TryParse(input, out result))
                {
                    if (!(result >= lowerLimit && result <= upperLimit))
                    {
                        Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}Invalid range. Enter a range between {lowerLimit} and {upperLimit} Try again.{Environment.NewLine}");
                    }
                    else
                    {
                        return true; // Success
                    }
                }
                else
                {
                    Logger.Instance.WriteToConsoleAndLog($"{Environment.NewLine}Invalid number. Try again.{Environment.NewLine}");
                }
            }
        }

    }
}
