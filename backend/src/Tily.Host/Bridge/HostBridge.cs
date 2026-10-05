using System.Text.Json;
using Tily.Core.Agents;
using Tily.Core.Context;
using Tily.Core.Projects;
using Tily.Core.Session;
using Tily.Core.Settings;
using Tily.Core.Shell;
using Tily.Core.StatusLog;
using Tily.Core.Terminal;
using Tily.Core.Worktrees;

namespace Tily.Host.Bridge;

public sealed class HostBridge : IDisposable
{
    private const string TextSavePrefix = """{"type":"text.save",""";
    private const string AttentionDone = "done";
    private static readonly string ApplicationVersion = typeof(HostBridge).Assembly.GetName().Version?.ToString(3) ?? string.Empty;
    private const string ShellIntegrationFolder = "shell-integration";
    private static readonly TimeSpan WriteDrainTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan CloseGrace = TimeSpan.FromSeconds(3);
    private static readonly JsonSerializerOptions JsonOptions = new(SessionRepository.JsonOptions) { WriteIndented = false };

    private readonly HostLoop _loop;
    private readonly Action<string> _send;
    private readonly string _dataDirectory;
    private readonly SessionRepository _sessions;
    private readonly SettingsService _settingsService;
    private readonly TerminalManager _terminals;
    private readonly TerminalFeed _terminalFeed;
    private readonly AgentStateFeed _agents;
    private readonly AttentionNotifier _notifier;
    private readonly FileExplorerFeed _files;
    private readonly FilePreviewFeed _preview;
    private readonly PreviewRequestFeed _previewRequests;
    private readonly GitFeed _git;
    private readonly WorktreeFeed _worktrees;
    private readonly UpdateFeed _updates;
    private readonly StatusLogFeed _statusLog;
    private readonly McpFeed _mcp;
    private readonly BrowserFeed _browsers;
    private SettingsModel _settings;
    private ShellPathsModel _shellPaths = ShellPathsModel.Empty;
    private PersistenceSettingsModel _persistence = PersistenceSettingsModel.Default;
    private PaneTextRepository _texts;
    private readonly BackgroundQueue _writes;
    private readonly BackgroundQueue _queries;
    private bool _webReady;
    private bool _closing;
    private IDisposable? _closeTimer;

    public HostBridge(HostLoop loop, string dataDirectory, Action<string> send)
    {
        _loop = loop;
        _send = send;
        _dataDirectory = dataDirectory;
        _sessions = new SessionRepository(dataDirectory);
        _settingsService = new SettingsService(dataDirectory);
        _settings = _settingsService.Load();
        _texts = new PaneTextRepository(dataDirectory, _persistence.MaxTextBytes);
        _terminals = new TerminalManager(integrationDirectory: Path.Combine(dataDirectory, ShellIntegrationFolder));
        _writes = new BackgroundQueue(PostBackgroundError);
        _queries = new BackgroundQueue(PostBackgroundError);
        _statusLog = new StatusLogFeed(dataDirectory, _writes, Post);
        _agents = new AgentStateFeed(dataDirectory, _terminals, Post);
        _notifier = new AttentionNotifier(PostNow);
        _files = new FileExplorerFeed(() => _settings.Editor, Post, PostBackgroundError);
        _preview = new FilePreviewFeed(() => _settings.Editor, Post, PostBackgroundError);
        _previewRequests = new PreviewRequestFeed(dataDirectory, Post);
        _git = new GitFeed(Post, () => _settings.Git.AutoFetch, PostBackgroundError);
        _worktrees = new WorktreeFeed(Post, () => _settings, RememberWorktreeFolder, _git.RefreshSoon, PostBackgroundError, dataDirectory);
        _updates = new UpdateFeed(Post, ApplicationVersion, dataDirectory);
        _mcp = new McpFeed(dataDirectory, Post, PostBackgroundError);
        _browsers = new BrowserFeed(loop, Post, PostNow);
        ApplySettings(_settings);
        _terminalFeed = new TerminalFeed(loop, _terminals, _agents.Forget, _queries, Post, PostNow);
    }

    public void Start()
    {
        _agents.Start();
        _mcp.Start();
    }

    public void Receive(string json)
    {
        if (json.StartsWith(BrowserFeed.Prefix, StringComparison.Ordinal) || json.StartsWith(BrowserFeed.DriverPrefix, StringComparison.Ordinal))
        {
            _browsers.Receive(json);
        }
        else if (json.StartsWith(TextSavePrefix, StringComparison.Ordinal))
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
            Post(new { type = "error", pane = FailedTerminalPane(command), message = UserErrorMessage.Of(exception) });
        }
    }

    private void RequestClose()
    {
        if (!_webReady)
        {
            CloseWindow();
            return;
        }

        if (_closing)
        {
            return;
        }

        _closing = true;
        PostNow(new { type = "app.closing", activity = _terminals.Activity() });
        _closeTimer = _loop.Schedule(CloseGrace, CloseWindow);
    }

    private void CancelClose()
    {
        _closing = false;
        _closeTimer?.Dispose();
        _closeTimer = null;
    }

    private void CloseWindow()
    {
        _closeTimer?.Dispose();
        _closeTimer = null;
        PostNow(new { type = "host.quit" });
        _loop.Stop();
    }

    private void Dispatch(BridgeCommandModel command)
    {
        switch (command.Type)
        {
            case "app.ready":
                _webReady = true;
                SendHello();
                _mcp.MarkReady();
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
            case "appearance.fontSize":
                SaveFontSize(command);
                break;
            case "attention.raise":
                RaiseAttention(RequirePane(command), command);
                break;
            case "attention.flash":
                _notifier.FlashWhenInactive(_settings.Notifications);
                break;
            case "attention.test":
                TestAttention(RequirePane(command), command);
                break;
            case "agents.installHooks":
                _agents.Hooks.Install();
                PostSettings(false);
                break;
            case "agents.removeHooks":
                _agents.Hooks.Remove();
                PostSettings(false);
                break;
            case "mcp.install":
                _mcp.Install();
                PostSettings(false);
                break;
            case "mcp.remove":
                _mcp.Remove();
                PostSettings(false);
                break;
            case "mcp.response":
                _mcp.Receive(command);
                break;
            case "settings.export":
                ExportPreferences(RequirePath(command));
                break;
            case "settings.import":
                ImportPreferences(RequirePath(command));
                break;
            case var type when type.StartsWith(TerminalFeed.Prefix, StringComparison.Ordinal):
                _terminalFeed.Handle(command);
                break;
            case "projects.list":
                ListProjects(_settings.ProjectsRoot, _settings.Worktrees.FolderFor(_settings.ProjectsRoot), _settings.WorktreeFolders);
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
            case var type when type.StartsWith("preview.", StringComparison.Ordinal):
                _preview.Handle(command);
                break;
            case var type when type.StartsWith("git.", StringComparison.Ordinal):
                _git.Handle(command);
                break;
            case "projects.repositories":
            case "projects.rememberRepository":
            case var type when type.StartsWith("worktrees.", StringComparison.Ordinal):
                _worktrees.Handle(command);
                break;
            case var type when type.StartsWith("update.", StringComparison.Ordinal):
                _updates.Handle(command);
                break;
            case var type when type.StartsWith("statusLog.", StringComparison.Ordinal):
                _statusLog.Handle(command);
                break;
            case "link.open":
                LocalActions.OpenLink(command.Url ?? throw new InvalidOperationException("Lien manquant."));
                break;
            case "window.close":
                CloseWindow();
                break;
            case "host.closeRequested":
                RequestClose();
                break;
            case "host.active":
                _notifier.WindowActive = command.Active;
                break;
            case "host.notificationClicked":
                PostNow(new { type = "agent.join", pane = RequirePane(command) });
                break;
            case "host.resource":
                _preview.Resolve(command.Id ?? throw new InvalidOperationException("Identifiant de ressource manquant."), command.Url ?? string.Empty);
                break;
            case "host.previewNavigate":
                _preview.FollowFromPage(command.Url ?? string.Empty);
                break;
            case "window.closeCancel":
                CancelClose();
                break;
            default:
                Post(new { type = "error", pane = FailedTerminalPane(command), message = $"Commande inconnue : {command.Type}" });
                break;
        }
    }

    private void RaiseAttention(string paneId, BridgeCommandModel command)
    {
        var settings = _settings.Notifications;
        var done = command.Kind == AttentionDone;
        if (done && !settings.NotifyDone)
        {
            return;
        }

        Notify(paneId, [command.Title ?? "Tily", command.Body, command.Location], done ? settings.DoneSound : settings.Sound, settings, done);
    }

    private void TestAttention(string paneId, BridgeCommandModel command)
    {
        var settings = (command.Notifications?.Deserialize<NotificationSettingsModel>(JsonOptions) ?? _settings.Notifications).Normalized();
        string?[] lines = command.Kind == AttentionDone
            ? ["Tily : test de fin", "Voici l’apparence d’un agent qui a terminé.", command.Location]
            : ["Tily : test de notification", "Voici l’apparence d’une demande d’attention.", command.Location];
        Notify(paneId, lines, command.Kind == AttentionDone ? settings.DoneSound : settings.Sound, settings, true);
    }

    private void Notify(string paneId, IEnumerable<string?> lines, string sound, NotificationSettingsModel settings, bool force)
    {
        try
        {
            _notifier.Notify(paneId, lines, sound, settings, force);
        }
        catch (Exception exception)
        {
            Post(new { type = "error", message = UserErrorMessage.Of(exception) });
        }
    }

    private void ApplySettings(SettingsModel settings)
    {
        _settings = settings;
        _shellPaths = SettingsService.ShellPaths(settings);
        _persistence = settings.Persistence;
        _texts = new PaneTextRepository(_dataDirectory, _persistence.MaxTextBytes);
        _terminals.UpdatePaths(_shellPaths);
        _updates.Configure(settings.Updates.AutoCheck);
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
            mcp = _mcp.Describe(),
            notifications = _notifier.Describe(),
            saved
        });
    }

    private void SaveFontSize(BridgeCommandModel command)
    {
        _settings.Appearance = _settingsService.SaveAppearance(_settings.Appearance with { FontSize = command.FontSize });
        Post(new { type = "appearance.changed", fontSize = _settings.Appearance.FontSize });
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
        var recovery = string.Join(" ", new[] { loaded.Error, text.Error, _statusLog.LoadError }.Where(error => error is not null));
        Post(new
        {
            type = "app.hello",
            version = ApplicationVersion,
            session,
            shells = ShellCatalog.Profiles(_shellPaths),
            home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            text = text.Text,
            persistence = _persistence,
            appearance = _settings.Appearance,
            statusLog = _statusLog.Entries(),
            recovery = recovery.Length > 0 ? recovery : null
        });
        _updates.PostState();
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

    private void ListProjects(string root, string worktreeFolder, IReadOnlyList<WorktreeProjectFolderModel> projectFolders) =>
        _queries.Enqueue(() =>
        {
            var projects = ProjectCatalog.List(root, worktreeFolder, projectFolders);
            Post(new { type = "projects.listed", root = projects.Root, projects = projects.Projects, error = projects.Error });
        });

    private void RememberWorktreeFolder(string project, string folder) =>
        _loop.TryEnqueue(() =>
        {
            try
            {
                _settingsService.RememberWorktreeFolder(_settings, project, folder);
            }
            catch (Exception exception)
            {
                PostNow(new { type = "error", message = UserErrorMessage.Of(exception) });
            }
        });

    private void QueryContext(string paneId, string path) =>
        _queries.Enqueue(() => Post(new { type = "context.result", pane = paneId, path, git = GitContext.Resolve(path) }));

    private void PostBackgroundError(Exception exception) => Post(new { type = "error", message = UserErrorMessage.Of(exception) });

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

    private void ExportPreferences(string path)
    {
        try
        {
            _settingsService.Export(_settings, path);
            PostNow(new { type = "settings.exported", path });
        }
        catch (Exception exception)
        {
            PostNow(new { type = "error", message = $"Export des préférences impossible : {exception.Message}" });
        }
    }

    private void ImportPreferences(string path)
    {
        try
        {
            var result = _settingsService.Import(path);
            if (result.Settings is null)
            {
                PostNow(new { type = "error", message = result.Error });
                return;
            }

            PostNow(new { type = "settings.imported", settings = result.Settings, path, warnings = result.Warnings });
        }
        catch (Exception exception)
        {
            PostNow(new { type = "error", message = $"Import des préférences impossible : {exception.Message}" });
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

    private static string? FailedTerminalPane(BridgeCommandModel command) =>
        command.Type is { } type && type.StartsWith(TerminalFeed.Prefix, StringComparison.Ordinal) ? command.Pane : null;

    private void Post(object message) => _loop.TryEnqueue(() => PostNow(message));

    private void PostNow(object message) => _send(JsonSerializer.Serialize(message, JsonOptions));

    public void Dispose()
    {
        _writes.Drain(WriteDrainTimeout);
        _agents.Dispose();
        _files.Dispose();
        _preview.Dispose();
        _previewRequests.Dispose();
        _mcp.Dispose();
        _browsers.Dispose();
        _git.Dispose();
        _terminalFeed.Dispose();
        _terminals.Dispose();
        _updates.LaunchPendingInstaller();
        _updates.Dispose();
    }
}
