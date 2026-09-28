using System.Collections.Concurrent;
using Dock.Core.Agents;
using Dock.Core.Context;
using Dock.Core.Shell;

namespace Dock.Core.Terminal;

public sealed class TerminalManager : IDisposable
{
    private readonly ConcurrentDictionary<string, TerminalSession> _sessions = new();
    private ShellPathsModel _paths;

    public TerminalManager(ShellPathsModel? paths = null)
    {
        _paths = paths ?? ShellPathsModel.Empty;
    }

    public void UpdatePaths(ShellPathsModel paths) => _paths = paths;

    public event Action<string, ReadOnlyMemory<byte>>? OutputReceived;
    public event Action<string, string>? CurrentDirectoryChanged;
    public event Action<string, uint>? Exited;

    public TerminalSession Start(string paneId, string shellId, string workingDirectory, int columns, int rows)
    {
        Stop(paneId);
        var profile = ShellCatalog.Resolve(shellId, _paths);
        var directory = Directory.Exists(workingDirectory) ? workingDirectory : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var session = new TerminalSession(new TerminalSessionOptions
        {
            PaneId = paneId,
            CommandLine = ShellCatalog.CommandLine(profile),
            WorkingDirectory = directory,
            Columns = Math.Max(columns, 20),
            Rows = Math.Max(rows, 5)
        });
        session.OutputReceived += data => OutputReceived?.Invoke(paneId, data);
        session.CurrentDirectoryChanged += path => CurrentDirectoryChanged?.Invoke(paneId, path);
        session.Exited += code =>
        {
            if (_sessions.TryGetValue(paneId, out var current) && ReferenceEquals(current, session))
            {
                Exited?.Invoke(paneId, code);
            }
        };
        _sessions[paneId] = session;
        return session;
    }

    public IReadOnlyList<MissingDirectoryModel> MissingDirectories() =>
        _sessions.Values
            .Where(session => !session.HasExited && session.CurrentDirectory is not null && !Directory.Exists(session.CurrentDirectory))
            .Select(session => new MissingDirectoryModel(session.PaneId, session.CurrentDirectory!, PathFallback.NearestExisting(session.CurrentDirectory!)))
            .ToList();

    public IReadOnlyList<PaneActivityModel> Activity() => Activity(_sessions.Keys.ToList());

    public IReadOnlyList<PaneActivityModel> Activity(IEnumerable<string> paneIds)
    {
        var activity = new List<PaneActivityModel>();
        foreach (var paneId in paneIds)
        {
            if (!_sessions.TryGetValue(paneId, out var session))
            {
                continue;
            }

            var processes = session.ActiveProcessNames();
            if (processes.Count > 0)
            {
                activity.Add(new PaneActivityModel(paneId, processes));
            }
        }

        return activity;
    }

    public IReadOnlyList<PaneProbeModel> Probes() =>
        _sessions.Values
            .Where(session => !session.HasExited)
            .Select(session => new PaneProbeModel(session.PaneId, session.StartedAtUtc, session.ActiveProcessNames()))
            .ToList();

    public TerminalSession Require(string paneId) =>
        _sessions.TryGetValue(paneId, out var session) ? session : throw new InvalidOperationException($"Aucun terminal pour le pane {paneId}.");

    public void Stop(string paneId)
    {
        if (_sessions.TryRemove(paneId, out var session))
        {
            session.Dispose();
        }
    }

    public void Dispose()
    {
        foreach (var paneId in _sessions.Keys.ToList())
        {
            Stop(paneId);
        }
    }
}
