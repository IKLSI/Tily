using Dock.Core.Context;
using Dock.Core.Files;

namespace Dock.Host.Bridge;

public sealed class FileExplorerFeed : IDisposable
{
    private static readonly TimeSpan ChangeDelay = TimeSpan.FromMilliseconds(150);

    private readonly nint _windowHandle;
    private readonly Func<string> _editorCommand;
    private readonly Action<object> _post;
    private readonly BackgroundQueue _queue;
    private readonly Dictionary<string, FileSystemWatcher?> _watchers = new(StringComparer.Ordinal);
    private readonly HashSet<string> _changed = new(StringComparer.Ordinal);
    private readonly object _sync = new();
    private readonly Timer _changeTimer;
    private readonly ExplorerGitMarks _gitMarks;

    public FileExplorerFeed(nint windowHandle, Func<string> editorCommand, Action<object> post, Action<Exception> onError)
    {
        _windowHandle = windowHandle;
        _editorCommand = editorCommand;
        _post = post;
        _queue = new BackgroundQueue(onError);
        _changeTimer = new Timer(_ => ListChanged());
        _gitMarks = new ExplorerGitMarks(post, _queue);
    }

    public void Handle(BridgeCommandModel command)
    {
        switch (command.Type)
        {
            case "files.watch":
                Watch(command.Paths ?? []);
                break;
            case "files.refresh":
                List(WatchedPaths());
                _gitMarks.Refresh();
                break;
            case "files.open":
                LocalActions.OpenFileInEditor(RequirePath(command), _editorCommand());
                break;
            case "files.reveal":
                LocalActions.RevealInExplorer(RequirePath(command));
                break;
            case "files.create":
                Create(RequirePath(command), command.Name, command.Kind);
                break;
            case "files.rename":
                Rename(RequirePath(command), RequireParent(command), command.Name);
                break;
            case "files.delete":
                Delete(RequirePath(command), RequireParent(command));
                break;
            default:
                throw new InvalidOperationException($"Commande inconnue : {command.Type}");
        }
    }

    private void Watch(string[] paths)
    {
        _gitMarks.Follow(paths.FirstOrDefault() ?? string.Empty);
        List<string> added;
        lock (_sync)
        {
            var wanted = paths.ToHashSet(StringComparer.Ordinal);
            foreach (var removed in _watchers.Keys.Where(path => !wanted.Contains(path)).ToList())
            {
                _watchers[removed]?.Dispose();
                _watchers.Remove(removed);
            }

            added = wanted.Where(path => !_watchers.ContainsKey(path)).ToList();
            foreach (var path in added)
            {
                _watchers[path] = CreateWatcher(path);
            }
        }

        List(added);
    }

    private FileSystemWatcher? CreateWatcher(string path)
    {
        try
        {
            FileExplorer.RequireFullPath(path);
            var watcher = new FileSystemWatcher(path) { NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName };
            watcher.Created += (_, _) => MarkChanged(path);
            watcher.Deleted += (_, _) => MarkChanged(path);
            watcher.Renamed += (_, _) => MarkChanged(path);
            watcher.Error += (_, _) => MarkChanged(path);
            watcher.EnableRaisingEvents = true;
            return watcher;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return null;
        }
    }

    private void MarkChanged(string path)
    {
        lock (_sync)
        {
            _changed.Add(path);
        }

        _changeTimer.Change(ChangeDelay, Timeout.InfiniteTimeSpan);
    }

    private void ListChanged()
    {
        List<string> changed;
        lock (_sync)
        {
            changed = _changed.Where(_watchers.ContainsKey).ToList();
            _changed.Clear();
        }

        List(changed);
    }

    private List<string> WatchedPaths()
    {
        lock (_sync)
        {
            return _watchers.Keys.ToList();
        }
    }

    private void List(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            _queue.Enqueue(() => PostListing(path));
        }
    }

    private void PostListing(string path)
    {
        var listing = FileExplorer.List(path);
        _post(new { type = "files.listed", path = listing.Path, entries = listing.Entries, total = listing.Total, error = listing.Error });
    }

    private void Create(string parent, string? name, string? kind) =>
        _queue.Enqueue(() =>
        {
            var path = kind == "folder" ? FileExplorer.CreateFolder(parent, name ?? string.Empty) : FileExplorer.CreateFile(parent, name ?? string.Empty);
            PostListing(parent);
            _post(new { type = "files.created", path });
        });

    private void Rename(string path, string parent, string? name) =>
        _queue.Enqueue(() =>
        {
            var target = FileExplorer.Rename(path, name ?? string.Empty);
            PostListing(parent);
            _post(new { type = "files.renamed", path, target });
        });

    private void Delete(string path, string parent) =>
        _queue.Enqueue(() =>
        {
            RecycleBin.Send(path, _windowHandle);
            PostListing(parent);
            _post(new { type = "files.deleted", path });
        });

    private static string RequirePath(BridgeCommandModel command) =>
        command.Path ?? throw new InvalidOperationException("Chemin manquant.");

    private static string RequireParent(BridgeCommandModel command) =>
        command.Parent ?? throw new InvalidOperationException("Dossier parent manquant.");

    public void Dispose()
    {
        _changeTimer.Dispose();
        _gitMarks.Dispose();
        lock (_sync)
        {
            foreach (var watcher in _watchers.Values)
            {
                watcher?.Dispose();
            }

            _watchers.Clear();
        }
    }
}
