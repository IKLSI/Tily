namespace Tily.Core.Git;

public sealed class GitAutoFetchSchedule
{
    public static readonly TimeSpan MinimumInterval = TimeSpan.FromMinutes(5);

    private readonly Dictionary<string, DateTimeOffset> _lastFetches = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();

    public bool TryStart(string root, DateTimeOffset now)
    {
        lock (_sync)
        {
            if (_lastFetches.TryGetValue(root, out var last) && now - last < MinimumInterval)
            {
                return false;
            }

            _lastFetches[root] = now;
            return true;
        }
    }

    public void Record(string root, DateTimeOffset now)
    {
        lock (_sync)
        {
            _lastFetches[root] = now;
        }
    }
}
