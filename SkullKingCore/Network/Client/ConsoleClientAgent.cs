namespace SkullKingCore.Network.Client;

/// <summary>Console UI agent: prompts user for bid and card; decides Tigress mode locally.</summary>
public sealed class ConsoleClientAgent
{
    private readonly string _playerName;
    public ConsoleClientAgent(string playerName) => _playerName = playerName;

    public Task<int> ChooseBidAsync(int round, int minBid, int maxBid, CancellationToken _)
    {
        while (true)
        {
            Console.Write($"[{_playerName}] Enter bid for round {round} [{minBid}-{maxBid}]: ");
            var s = Console.ReadLine();
            if (int.TryParse(s, out var bid) && bid >= minBid && bid <= maxBid)
                return Task.FromResult(bid);
        }
    }

    public Task<(int index, string? tigressMode)> ChooseCardAsync(
        IReadOnlyList<string> allowedLabels,
        bool requireTigressMode,
        CancellationToken _)
    {
        Console.WriteLine($"[{_playerName}] Allowed cards:");
        for (int i = 0; i < allowedLabels.Count; i++)
            Console.WriteLine($"  [{i}] {allowedLabels[i]}");

        int idx;
        while (true)
        {
            Console.Write($"[{_playerName}] Choose index: ");
            var s = Console.ReadLine();
            if (int.TryParse(s, out idx) && idx >= 0 && idx < allowedLabels.Count) break;
        }

        string? tigressMode = null;
        if (requireTigressMode && allowedLabels[idx].StartsWith("TIGRESS", StringComparison.OrdinalIgnoreCase))
        {
            while (true)
            {
                Console.Write($"[{_playerName}] Tigress mode (E=Escape, P=Pirate): ");
                var ans = Console.ReadLine()?.Trim().ToUpperInvariant();
                if (ans == "E") { tigressMode = "ESCAPE"; break; }
                if (ans == "P") { tigressMode = "PIRATE"; break; }
            }
        }

        return Task.FromResult((idx, tigressMode));
    }

    public Task Show(string msg) { Console.WriteLine($"[{_playerName}] {msg}"); return Task.CompletedTask; }
}
