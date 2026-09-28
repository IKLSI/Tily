using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Dock.Core.Agents;
using Dock.Core.Context;
using Dock.Core.Projects;
using Dock.Core.Session;
using Dock.Core.Settings;
using Dock.Core.Shell;
using Dock.Core.Terminal;
using Microsoft.UI.Dispatching;
using Microsoft.Web.WebView2.Core;

namespace Dock.Host.Bridge;

public sealed class HostBridge : IDisposable
{
    private const int MaxCharsPerMessage = 512 * 1024;
    private const string TextSavePrefix = """{"type":"text.save",""";
    private static readonly TimeSpan WriteDrainTimeout = TimeSpan.FromSeconds(10);
    private static readonly JsonSerializerOptions JsonOptions = SessionRepository.JsonOptions;

    private readonly DispatcherQueue _dispatcher;
    private readonly Action _closeWindow;
    private readonly nint _windowHandle;
    private readonly string _dataDirectory;
    private readonly SessionRepository _sessions;
    private readonly SettingsService _settingsService;
    private readonly TerminalManager _terminals;
    private readonly AgentStateFeed _agents;
    private readonly AttentionNotifier _notifier;
    private readonly FileExplorerFeed _files;
    private readonly GitFeed _git;
    private SettingsModel _settings;
    private ShellPathsModel _shellPaths = ShellPathsModel.Empty;
    private PersistenceSettingsModel _persistence = PersistenceSettingsModel.Default;
    private PaneTextRepository _texts;
    private readonly ConcurrentDictionary<string, PaneOutputBuffer> _buffers = new();
    private readonly BackgroundQueue _writes;
    private readonly BackgroundQueue _queries;
    private CoreWebView2? _core;
    private int _flushScheduled;
    private bool _closing;
    private DispatcherQueueTimer? _closeTimer;

    public HostBridge(DispatcherQueue dispatcher, string dataDirectory, nint windowHandle, Action closeWindow)
    {
        _dispatcher = dispatcher;
        _windowHandle = windowHandle;
        _closeWindow = closeWindow;
        _dataDirectory = dataDirectory;
        _sessions = new SessionRepository(dataDirectory);
        _settingsService = new SettingsService(dataDirectory);
        _settings = _settingsService.Load();
        _texts = new PaneTextRepository(dataDirectory, _persistence.MaxTextBytes);
        _terminals = new TerminalManager();
        _writes = new BackgroundQueue(PostBackgroundError);
        _queries = new BackgroundQueue(PostBackgroundError);
        _agents = new AgentStateFeed(dataDirectory, _terminals, Post);
        _notifier = new AttentionNotifier(dispatcher, windowHandle, paneId => PostNow(new { type = "agent.join", pane = paneId }));
        _notifier.Register();
        _files = new FileExplorerFeed(windowHandle, () => _settings.Editor, Post, PostBackgroundError);
        _git = new GitFeed(Post, PostBackgroundError);
        ApplySettings(_settings);
        _terminals.OutputReceived += HandleOutput;
        _terminals.CurrentDirectoryChanged += HandleCurrentDirectoryChanged;
        _terminals.Exited += (paneId, code) => Post(new { type = "terminal.exit", pane = paneId, code });
    }

    public void Attach(CoreWebView2 core)
    {
        _core = core;
        core.WebMessageReceived += (_, args) => Receive(args.WebMessageAsJson);
        _agents.Start();
    }

    private void Receive(string json)
    {
        if (json.StartsWith(TextSavePrefix, StringComparison.Ordinal))
        {
            _writes.Enqueue(() => Handle(json));
        }
        else
        {
            Handle(json);
        }
    }

    private void Handle(string json)
    {
        BridgeCommandModel? command;
        try
        {
            command = JsonSerializer.Deserialize<BridgeCommandModel>(json, JsonOptions);
        }
        catch (JsonException)
        {
            Post(new { type = "error", message = "Message du pont illisible." });
            return;
        }

        if (command is null)
        {
            return;
        }

        try
        {
            Dispatch(command);
        }
        catch (Exception exception)
        {
            Post(new { type = "error", pane = command.Pane, message = exception.Message });
        }
    }

    public void SetWindowActive(bool active) => _notifier.WindowActive = active;

    public bool RequestClose()
    {
        if (_core is null)
        {
            return false;
        }

        if (_closing)
        {
            return true;
        }

        _closing = true;
        PostNow(new { type = "app.closing", activity = _terminals.Activity() });
        _closeTimer = _dispatcher.CreateTimer();
        _closeTimer.Interval = TimeSpan.FromSeconds(3);
        _closeTimer.IsRepeating = false;
        _closeTimer.Tick += (_, _) => _closeWindow();
        _closeTimer.Start();
        return true;
    }

    private void CancelClose()
    {
        _closing = false;
        _closeTimer?.Stop();
        _closeTimer = null;
    }

    private void Dispatch(BridgeCommandModel command)
    {
        switch (command.Type)
        {
            case "app.ready":
                SendHello();
                break;
            case "session.save":
                SaveSession(command);
                break;
            case "text.save":
                SaveText(command);
                break;
            case "settings.get":
                PostSettings(false);
                break;
            case "settings.save":
                SaveSettings(command);
                break;
            case "attention.raise":
                Notify(RequirePane(command), command.Title ?? "Dock", command.Body ?? string.Empty, _settings.Notifications, false);
                break;
            case "attention.test":
                var notifications = command.Notifications?.Deserialize<NotificationSettingsModel>(JsonOptions) ?? _settings.Notifications;
                Notify(RequirePane(command), "Dock : test de notification", "Voici l’apparence d’une demande d’attention.", notifications.Normalized(), true);
                break;
            case "agents.installHooks":
                _agents.Hooks.Install();
                PostSettings(false);
                break;
            case "agents.removeHooks":
                _agents.Hooks.Remove();
                PostSettings(false);
                break;
            case "settings.export":
                _ = ExportPreferencesAsync();
                break;
            case "settings.import":
                _ = ImportPreferencesAsync();
                break;
            case "dialog.pick":
                _ = PickPathAsync(command.Field ?? throw new InvalidOperationException("Champ manquant."), command.Target);
                break;
            case "terminal.create":
                CreateTerminal(command);
                break;
            case "terminal.input":
                _terminals.Require(RequirePane(command)).Write(Encoding.UTF8.GetBytes(command.Data ?? string.Empty));
                break;
            case "terminal.resize":
                _terminals.Require(RequirePane(command)).Resize(command.Cols, command.Rows);
                break;
            case "terminal.ack":
                if (_buffers.TryGetValue(RequirePane(command), out var buffer))
                {
                    buffer.Acknowledge(command.Chars);
                }

                break;
            case "terminal.close":
                CloseTerminal(RequirePane(command));
                break;
            case "terminal.activity":
                Post(new { type = "terminal.activityResult", panes = _terminals.Activity(command.Panes ?? []) });
                break;
            case "projects.list":
                ListProjects(_settings.ProjectsRoot);
                break;
            case "context.query":
                QueryContext(RequirePane(command), RequirePath(command));
                break;
            case "context.open":
                OpenFolder(RequirePath(command), command.Target);
                break;
            case var type when type.StartsWith("files.", StringComparison.Ordinal):
                _files.Handle(command);
                break;
            case var type when type.StartsWith("git.", StringComparison.Ordinal):
                _git.Handle(command);
                break;
            case "link.open":
                LocalActions.OpenLink(command.Url ?? throw new InvalidOperationException("Lien manquant."));
                break;
            case "window.close":
                _closeWindow();
                break;
            case "window.closeCancel":
                CancelClose();
                break;
            default:
                Post(new { type = "error", pane = command.Pane, message = $"Commande inconnue : {command.Type}" });
                break;
        }
    }

    private void Notify(string paneId, string title, string body, NotificationSettingsModel settings, bool force)
    {
        try
        {
            _notifier.Notify(paneId, title, body, settings, force);
        }
        catch (Exception exception)
        {
            Post(new { type = "error", message = exception.Message });
        }
    }

    private void ApplySettings(SettingsModel settings)
    {
        _settings = settings;
        _shellPaths = SettingsService.ShellPaths(settings);
        _persistence = settings.Persistence;
        _texts = new PaneTextRepository(_dataDirectory, _persistence.MaxTextBytes);
        _terminals.UpdatePaths(_shellPaths);
    }

    private void PostSettings(bool saved)
    {
        var snapshot = _settingsService.Snapshot(_settings);
        Post(new
        {
            type = "settings.result",
            settings = snapshot.Settings,
            shellSettings = snapshot.Shells,
            files = snapshot.Files,
            warnings = snapshot.Warnings,
            shells = ShellCatalog.Profiles(_shellPaths),
            persistence = _persistence,
            agents = _agents.Describe(),
            notifications = _notifier.Describe(),
            saved
        });
    }

    private void SaveSettings(BridgeCommandModel command)
    {
        var settings = command.Settings?.Deserialize<SettingsModel>(JsonOptions) ?? throw new InvalidOperationException("Réglages manquants.");
        var result = _settingsService.Save(settings);
        if (!result.IsValid)
        {
            throw new InvalidOperationException($"Réglages refusés : {result.Error}");
        }

        ApplySettings(settings);
        PostSettings(true);
    }

    private void SendHello()
    {
        var loaded = _sessions.Load();
        var session = loaded.Session ?? SessionFactory.Initial();
        _texts.MoveClosedTabText(session);
        var text = _texts.Load();
        var recovery = string.Join(" ", new[] { loaded.Error, text.Error }.Where(error => error is not null));
        Post(new
        {
            type = "app.hello",
            session,
            shells = ShellCatalog.Profiles(_shellPaths),
            home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            text = text.Text,
            persistence = _persistence,
            recovery = recovery.Length > 0 ? recovery : null
        });
    }

    private void SaveSession(BridgeCommandModel command)
    {
        var element = command.Session ?? throw new InvalidOperationException("Session manquante.");
        _writes.Enqueue(() =>
        {
            var session = element.Deserialize<SessionModel>(JsonOptions) ?? throw new InvalidOperationException("Session manquante.");
            Persist(_sessions.FilePath, () =>
            {
                var result = _sessions.Save(session);
                return result.IsValid ? null : $"Session refusée : {result.Error}";
            });
        });
    }

    private void SaveText(BridgeCommandModel command)
    {
        var element = command.Text ?? throw new InvalidOperationException("Texte des terminaux manquant.");
        var keep = command.Keep ?? throw new InvalidOperationException("Liste des panes à conserver manquante.");
        var texts = _texts;
        _writes.Enqueue(() =>
        {
            var text = element.Deserialize<Dictionary<string, string>>(JsonOptions) ?? throw new InvalidOperationException("Texte des terminaux manquant.");
            Persist(texts.DirectoryPath, () =>
            {
                texts.Save(text, keep);
                return null;
            });
        });
    }

    private void ListProjects(string root) =>
        _queries.Enqueue(() =>
        {
            var projects = ProjectCatalog.List(root);
            Post(new { type = "projects.listed", root = projects.Root, projects = projects.Projects, error = projects.Error });
        });

    private void QueryContext(string paneId, string path) =>
        _queries.Enqueue(() => Post(new { type = "context.result", pane = paneId, path, git = GitContext.Resolve(path) }));

    private void PostBackgroundError(Exception exception) => Post(new { type = "error", message = exception.Message });

    private void Persist(string filePath, Func<string?> write)
    {
        string? failure;
        try
        {
            failure = write();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            failure = $"Impossible d’écrire {filePath} : {exception.Message}";
        }

        if (failure is null)
        {
            Post(new { type = "session.saved" });
        }
        else
        {
            Post(new { type = "session.saveFailed", message = failure });
        }
    }

    private void CreateTerminal(BridgeCommandModel command)
    {
        var paneId = RequirePane(command);
        CloseTerminal(paneId);
        _buffers[paneId] = new PaneOutputBuffer(paneId);
        var cwd = command.Cwd ?? string.Empty;
        var session = _terminals.Start(paneId, command.Shell ?? ShellCatalog.DefaultShellId, cwd, command.Cols, command.Rows);
        Post(new { type = "terminal.created", pane = paneId, pid = session.ProcessId });
        if (cwd.Length > 0 && !Directory.Exists(cwd))
        {
            Post(new { type = "terminal.pathMissing", pane = paneId, path = cwd, fallback = PathFallback.NearestExisting(cwd) });
        }
    }

    private void HandleCurrentDirectoryChanged(string paneId, string path)
    {
        Post(new { type = "terminal.cwd", pane = paneId, path });
        foreach (var missing in _terminals.MissingDirectories())
        {
            Post(new { type = "terminal.pathMissing", pane = missing.PaneId, path = missing.Path, fallback = missing.Fallback });
        }
    }

    private void CloseTerminal(string paneId)
    {
        if (_buffers.TryRemove(paneId, out var buffer))
        {
            buffer.Release();
        }

        _terminals.Stop(paneId);
        _agents.Forget(paneId);
    }

    private void HandleOutput(string paneId, ReadOnlyMemory<byte> data)
    {
        if (!_buffers.TryGetValue(paneId, out var buffer))
        {
            return;
        }

        buffer.Append(data.Span);
        if (Interlocked.CompareExchange(ref _flushScheduled, 1, 0) == 0)
        {
            _dispatcher.TryEnqueue(DispatcherQueuePriority.High, Flush);
        }
    }

    private void Flush()
    {
        Interlocked.Exchange(ref _flushScheduled, 0);
        foreach (var buffer in _buffers.Values)
        {
            var text = buffer.Take();
            if (text is null)
            {
                continue;
            }

            for (var offset = 0; offset < text.Length; offset += MaxCharsPerMessage)
            {
                var length = Math.Min(MaxCharsPerMessage, text.Length - offset);
                PostNow(new { type = "terminal.output", pane = buffer.PaneId, data = text.Substring(offset, length) });
            }
        }
    }

    private async Task ExportPreferencesAsync()
    {
        try
        {
            var path = await PathPicker.SaveJsonAsync(_windowHandle, "dock-preferences");
            if (path is null)
            {
                return;
            }

            _settingsService.Export(_settings, path);
            PostNow(new { type = "settings.exported", path });
        }
        catch (Exception exception)
        {
            PostNow(new { type = "error", message = $"Export des préférences impossible : {exception.Message}" });
        }
    }

    private async Task ImportPreferencesAsync()
    {
        try
        {
            var path = await PathPicker.PickJsonAsync(_windowHandle);
            if (path is null)
            {
                return;
            }

            var result = _settingsService.Import(path);
            if (result.Settings is null)
            {
                PostNow(new { type = "error", message = result.Error });
                return;
            }

            PostNow(new { type = "settings.imported", settings = result.Settings, path });
        }
        catch (Exception exception)
        {
            PostNow(new { type = "error", message = $"Import des préférences impossible : {exception.Message}" });
        }
    }

    private async Task PickPathAsync(string field, string? target)
    {
        try
        {
            var path = await PathPicker.PickAsync(_windowHandle, target);
            if (path is not null)
            {
                PostNow(new { type = "dialog.picked", field, path });
            }
        }
        catch (Exception exception)
        {
            PostNow(new { type = "error", message = $"Impossible d’ouvrir le sélecteur : {exception.Message}" });
        }
    }

    private void OpenFolder(string path, string? target)
    {
        switch (target)
        {
            case "editor":
                LocalActions.OpenInEditor(path, _settings.Editor);
                break;
            case "explorer":
                LocalActions.OpenInExplorer(path);
                break;
            default:
                throw new InvalidOperationException($"Cible d’ouverture inconnue : {target}");
        }
    }

    private static string RequirePath(BridgeCommandModel command) =>
        command.Path ?? throw new InvalidOperationException("Chemin manquant.");

    private static string RequirePane(BridgeCommandModel command) =>
        command.Pane ?? throw new InvalidOperationException("Identifiant de pane manquant.");

    private void Post(object message) => _dispatcher.TryEnqueue(() => PostNow(message));

    private void PostNow(object message) => _core?.PostWebMessageAsJson(JsonSerializer.Serialize(message, JsonOptions));

    public void Dispose()
    {
        _writes.Drain(WriteDrainTimeout);
        _agents.Dispose();
        _notifier.Dispose();
        _files.Dispose();
        _git.Dispose();
        foreach (var buffer in _buffers.Values)
        {
            buffer.Release();
        }

        _terminals.Dispose();
    }
}
