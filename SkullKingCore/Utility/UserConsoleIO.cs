using SkullKingCore.Logging;
using SkullKingCore.Network;

namespace SkullKingCore.Utility
{
    public static class UserConsoleIO
    {

        public static int ReadIntUntilValid(string prompt, int min, int max)
        {
            int value;
            while (!TryReadInt(prompt, out value, min, max))
            {
                // just loop until TryReadInt returns true
            }
            return value;
        }

        public static bool TryReadInt(string prompt, out int result, int lowerLimit = int.MinValue, int upperLimit = int.MaxValue)
        {
            while (true)
            {
                //Logger.Instance.WriteToConsoleAndLog($"{prompt} (or 'E' to cancel): ");
                Logger.Instance.WriteToConsoleAndLog($"{prompt} [range: {lowerLimit} - {upperLimit}]:");

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

        /// <summary>
        /// Continuously prompts until a valid "host:port" is entered.
        /// Supports IPv4, hostnames, and IPv6 with brackets ([::1]:1234).
        /// Prints errors instead of throwing.
        /// </summary>
        public static void ParseHostPortUntilValid(string prompt, out string host, out int port)
        {
            host = "";
            port = 0;

            while (true)
            {
                Logger.Instance.WriteToConsoleAndLog(prompt);

                var input = Console.ReadLine() ?? "";

                if (string.IsNullOrWhiteSpace(input))
                {
                    Logger.Instance.WriteToConsoleAndLog("Error: input must not be empty.");
                    continue;
                }

                if (Uri.TryCreate($"tcp://{input}", UriKind.Absolute, out var uri) &&
                    !string.IsNullOrEmpty(uri.Host) &&
                    uri.Port > 0 && uri.Port <= 65535)
                {
                    host = uri.Host;
                    port = uri.Port;
                    return;
                }

                Logger.Instance.WriteToConsoleAndLog("Error: invalid host:port format. Example: 127.0.0.1:1234 or [::1]:5678");
            }
        }

        public static TransportKind PromptTransportKind()
        {
            var items = Enum.GetValues(typeof(TransportKind))
                            .Cast<TransportKind>()
                            .OrderBy(k => Convert.ToInt32(k))
                            .ToArray();

            Logger.Instance.WriteToConsoleAndLog("Network Transport Kinds:");

            for (int i = 0; i < items.Length; i++)
            {
                Logger.Instance.WriteToConsoleAndLog($"  {i + 1}) {Misc.GetEnumLabel(items[i])}");
            }

            var choice = UserConsoleIO.ReadIntUntilValid($"{Environment.NewLine}Enter a choice for the Transport Kind", 1, items.Length);

            return items[choice - 1];
        }

    }
}
