using Dock.Core.Git;

namespace Dock.Host.Bridge;

public sealed class GitWatcher : IDisposable
{
    private const int BufferSize = 64 * 1024;
    private static readonly TimeSpan ChangeDelay = TimeSpan.FromMilliseconds(300);
    private static readonly HashSet<string> GitEntries = new(StringComparer.OrdinalIgnoreCase)
    {
        "HEAD", "index", "packed-refs", "refs", "MERGE_HEAD", "CHERRY_PICK_HEAD", "REVERT_HEAD", "REBASE_HEAD", "rebase-merge", "rebase-apply"
    };

    private readonly string[] _gitDirectories;
    private readonly List<FileSystemWatcher> _watchers = [];
    private readonly Action _changed;
    private readonly Timer _timer;
    private int _scheduled;
    private bool _disposed;

    public GitWatcher(GitLocationModel location, Action changed)
    {
        _changed = changed;
        _gitDirectories = [location.GitDirectory, location.CommonDirectory];
        _timer = new Timer(_ => Fire());
        foreach (var directory in new[] { location.Root, location.GitDirectory, location.CommonDirectory }.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (directory == location.Root || !IsWithin(directory, location.Root))
            {
                Add(directory);
            }
        }
    }

    private void Add(string directory)
    {
        try
        {
            var watcher = new FileSystemWatcher(directory)
            {
                IncludeSubdirectories = true,
                InternalBufferSize = BufferSize,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size
            };
            watcher.Changed += (_, args) => Signal(args.FullPath);
            watcher.Created += (_, args) => Signal(args.FullPath);
            watcher.Deleted += (_, args) => Signal(args.FullPath);
            watcher.Renamed += (_, args) => Signal(args.FullPath);
            watcher.Error += (_, _) => Schedule();
            watcher.EnableRaisingEvents = true;
            _watchers.Add(watcher);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
        }
    }

    private void Signal(string path)
    {
        if (IsRelevant(path))
        {
            Schedule();
        }
    }

    private void Schedule()
    {
        if (Volatile.Read(ref _disposed) || Interlocked.Exchange(ref _scheduled, 1) != 0)
        {
            return;
        }

        try
        {
            _timer.Change(ChangeDelay, Timeout.InfiniteTimeSpan);
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void Fire()
    {
        Interlocked.Exchange(ref _scheduled, 0);
        if (!Volatile.Read(ref _disposed))
        {
            _changed();
        }
    }

    private bool IsRelevant(string path)
    {
        var gitDirectory = _gitDirectories.FirstOrDefault(directory => IsWithin(path, directory));
        if (gitDirectory is null)
        {
            return true;
        }

        var relative = Path.GetRelativePath(gitDirectory, path);
        return !relative.EndsWith(".lock", StringComparison.OrdinalIgnoreCase) && GitEntries.Contains(relative.Split(Path.DirectorySeparatorChar)[0]);
    }

    private static bool IsWithin(string path, string directory) =>
        path.StartsWith(directory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
        || string.Equals(path, directory, StringComparison.OrdinalIgnoreCase);

    public void Dispose()
    {
        Volatile.Write(ref _disposed, true);
        _timer.Dispose();
        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }
    }
}
