using System.Text.Json;
using Dock.Core.Git;
using Dock.Core.Session;

namespace Dock.Host.Bridge;

public sealed class GitFeed : IDisposable
{
    private const string MissingGit = "Git est introuvable : installez Git pour Windows ou ajoutez git.exe au PATH.";

    private readonly Action<object> _post;
    private readonly GitRunner _runner = new();
    private readonly BackgroundQueue _reads;
    private readonly GitOperationRunner _operations;
    private readonly object _sync = new();
    private string _path = string.Empty;
    private GitLocationModel? _location;
    private GitWatcher? _watcher;
    private GitHistoryScope _scope = GitHistoryScope.All;
    private int _historyCount = GitHistoryReader.PageSize;
    private string? _lastState;
    private string? _lastSignature;
    private int _refreshQueued;

    public GitFeed(Action<object> post, Action<Exception> onError)
    {
        _post = post;
        _reads = new BackgroundQueue(onError);
        _operations = new GitOperationRunner(Open, post, ScheduleRefresh, onError);
    }

    public void Handle(BridgeCommandModel command)
    {
        if (_operations.Handle(command))
        {
            return;
        }

        switch (command.Type)
        {
            case "git.watch":
                Watch(command.Path ?? string.Empty);
                break;
            case "git.refresh":
                _reads.Enqueue(() => Refresh(true, true));
                break;
            case "git.history":
                ChangeHistory(command.Scope, command.Count);
                break;
            case "git.diff":
                Read(command, "git.diff", repository => GitDiffReader.Read(repository, DiffRequest(command)));
                break;
            case "git.details":
                Read(command, "git.details", repository => GitDiffReader.ReadCommit(repository, command.Commit));
                break;
            default:
                throw new InvalidOperationException($"Commande inconnue : {command.Type}");
        }
    }

    private void Watch(string path)
    {
        lock (_sync)
        {
            _path = path;
        }

        _reads.Enqueue(() => Follow(path));
    }

    private void Follow(string path)
    {
        GitLocationModel? location;
        try
        {
            location = path.Length == 0 ? null : GitRepository.Locate(_runner, path);
        }
        catch (GitCommandException exception)
        {
            _post(new { type = "git.state", path, error = exception.Message });
            return;
        }

        lock (_sync)
        {
            if (_path != path)
            {
                return;
            }

            if (location?.Root != _location?.Root)
            {
                _watcher?.Dispose();
                _watcher = location is null ? null : new GitWatcher(location, ScheduleRefresh);
                _location = location;
                _historyCount = GitHistoryReader.PageSize;
                _lastState = null;
                _lastSignature = null;
            }
        }

        if (path.Length == 0)
        {
            return;
        }

        if (location is null)
        {
            _post(new { type = "git.state", path, error = GitRunner.IsInstalled ? null : MissingGit });
            return;
        }

        Refresh(true, false);
    }

    private void ScheduleRefresh()
    {
        if (Interlocked.Exchange(ref _refreshQueued, 1) == 0)
        {
            _reads.Enqueue(() =>
            {
                Interlocked.Exchange(ref _refreshQueued, 0);
                Refresh(false, false, true);
            });
        }
    }

    private void Refresh(bool forceState, bool forceHistory, bool announceUnchanged = false)
    {
        GitLocationModel? location;
        string path;
        GitHistoryScope scope;
        int count;
        lock (_sync)
        {
            (location, path, scope, count) = (_location, _path, _scope, _historyCount);
        }

        if (location is null || path.Length == 0)
        {
            return;
        }

        var repository = new GitRepository(_runner, location);
        GitStateModel state;
        try
        {
            state = GitStateReader.Read(repository, _operations.Journal.Get(location.Root), branch => _operations.IsForceAllowed(location.Root, branch));
        }
        catch (GitCommandException exception)
        {
            _post(new { type = "git.state", path, error = exception.Message });
            return;
        }

        var json = JsonSerializer.Serialize(state, SessionRepository.JsonOptions);
        var signature = $"{GitStateReader.Signature(state)}\n{scope}\n{count}";
        bool postState;
        bool postHistory;
        lock (_sync)
        {
            if (!ReferenceEquals(location, _location))
            {
                return;
            }

            postState = forceState || json != _lastState;
            postHistory = forceState || forceHistory || signature != _lastSignature;
            (_lastState, _lastSignature) = (json, signature);
        }

        if (postState)
        {
            _post(new { type = "git.state", path, state });
        }
        else if (announceUnchanged)
        {
            _post(new { type = "git.changed", path });
        }

        if (postHistory)
        {
            PostHistory(repository, state, scope, count);
        }
    }

    private void PostHistory(GitRepository repository, GitStateModel state, GitHistoryScope scope, int count)
    {
        try
        {
            _post(new { type = "git.history", history = GitHistoryReader.Read(repository, scope, count, state) });
        }
        catch (GitCommandException exception)
        {
            _post(new { type = "git.history", history = GitHistoryReader.Empty(repository.Root, scope), error = exception.Message });
        }
    }

    private void ChangeHistory(string? scope, int count)
    {
        lock (_sync)
        {
            _scope = scope == "current" ? GitHistoryScope.Current : GitHistoryScope.All;
            _historyCount = Math.Clamp(count, GitHistoryReader.PageSize, GitHistoryReader.MaxCommits);
        }

        _reads.Enqueue(() => Refresh(false, true));
    }

    private void Read(BridgeCommandModel command, string type, Func<GitRepository, object> read) =>
        _reads.Enqueue(() =>
        {
            try
            {
                _post(new { type, request = command.Request, result = read(Open(command.Path)) });
            }
            catch (Exception exception) when (exception is GitCommandException or IOException or UnauthorizedAccessException)
            {
                _post(new { type, request = command.Request, error = exception.Message });
            }
        });

    private GitRepository Open(string? path)
    {
        lock (_sync)
        {
            if (_location is { } location && string.Equals(location.Root, path, StringComparison.OrdinalIgnoreCase))
            {
                return new GitRepository(_runner, location);
            }
        }

        return GitRepository.Open(_runner, path ?? string.Empty);
    }

    private static GitDiffRequestModel DiffRequest(BridgeCommandModel command)
    {
        var source = command.Source switch
        {
            "staged" => GitDiffSource.Staged,
            "commit" => GitDiffSource.Commit,
            _ => GitDiffSource.Unstaged
        };
        return new GitDiffRequestModel(source, command.File ?? string.Empty, command.OldFile, command.Commit, command.Untracked);
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
