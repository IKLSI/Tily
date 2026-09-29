using System.Text.Json;
using Dock.Core.Git;

namespace Dock.Host.Bridge;

public sealed class ExplorerGitMarks : IDisposable
{
    private readonly GitRunner _runner = new();
    private readonly Action<object> _post;
    private readonly BackgroundQueue _queue;
    private readonly object _sync = new();
    private string _folder = string.Empty;
    private GitLocationModel? _location;
    private GitWatcher? _watcher;
    private string? _lastMarks;

    public ExplorerGitMarks(Action<object> post, BackgroundQueue queue)
    {
        _post = post;
        _queue = queue;
    }

    public void Follow(string folder)
    {
        lock (_sync)
        {
            if (string.Equals(_folder, folder, StringComparison.Ordinal))
            {
                return;
            }

            _folder = folder;
        }

        _queue.Enqueue(() => Locate(folder));
    }

    public void Refresh() => _queue.Enqueue(() => PostMarks(true));

    private void Locate(string folder)
    {
        var location = LocationOf(folder);
        lock (_sync)
        {
            if (!string.Equals(_folder, folder, StringComparison.Ordinal))
            {
                return;
            }

            if (!string.Equals(location?.Root, _location?.Root, StringComparison.OrdinalIgnoreCase))
            {
                _watcher?.Dispose();
                _watcher = location is null ? null : new GitWatcher(location, () => _queue.Enqueue(() => PostMarks(false)));
                _location = location;
                _lastMarks = null;
            }
        }

        PostMarks(false);
    }

    private GitLocationModel? LocationOf(string folder)
    {
        if (folder.Length == 0 || !GitRunner.IsInstalled)
        {
            return null;
        }

        try
        {
            return GitRepository.Locate(_runner, folder);
        }
        catch (GitCommandException)
        {
            return null;
        }
    }

    private void PostMarks(bool force)
    {
        GitLocationModel? location;
        lock (_sync)
        {
            location = _location;
        }

        var marks = location is null ? [] : Read(location);
        var json = JsonSerializer.Serialize(marks);
        lock (_sync)
        {
            if (!ReferenceEquals(location, _location) || (!force && json == _lastMarks))
            {
                return;
            }

            _lastMarks = json;
        }

        _post(new { type = "files.gitMarks", root = location?.Root, marks });
    }

    private IReadOnlyList<GitPathMarkModel> Read(GitLocationModel location)
    {
        try
        {
            return GitPathMarks.From(location.Root, new GitRepository(_runner, location).Status());
        }
        catch (GitCommandException)
        {
            return [];
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _watcher?.Dispose();
            _watcher = null;
            _location = null;
        }
    }
}
