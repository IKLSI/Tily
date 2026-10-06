using System.Text.Json;
using Tily.Core.Agents;
using Tily.Core.Context;
using Tily.Core.Projects;
using Tily.Core.Session;
using Tily.Core.Settings;
using Tily.Core.StatusLog;
using Tily.Core.Terminal;

namespace Tily.Host.Bridge;

public sealed class HostBridge : IDisposable
{
    public static readonly JsonSerializerOptions JsonOptions = new(SessionRepository.JsonOptions) { WriteIndented = false };

    private const string AttentionDone = "done";
    private static readonly string ApplicationVersion = typeof(HostBridge).Assembly.GetName().Version?.ToString(3) ?? string.Empty;
    private const string ShellIntegrationFolder = "shell-integration";
    private static readonly TimeSpan WriteDrainTimeout = TimeSpan.FromSeconds(10);

    private readonly HostLoop _loop;
    private readonly Action<string> _send;
    private readonly SettingsFeed _settings;
    private readonly SessionFeed _session;
    private readonly WindowCloseFeed _windowClose;
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
    private readonly BackgroundQueue _writes;
    private readonly BackgroundQueue _queries;

    public HostBridge(HostLoop loop, string dataDirectory, Action<string> send, Action<ReadOnlySpan<byte>> sendUtf8Line)
    {
        _loop = loop;
        _send = send;
        _writes = new BackgroundQueue(PostBackgroundError);
        _queries = new BackgroundQueue(PostBackgroundError);
        _settings = new SettingsFeed(loop, dataDirectory, DescribeAgents, DescribeMcp, DescribeNotifications, ApplySettings, Post, PostNow);
        _statusLog = new StatusLogFeed(dataDirectory, _writes, Post);
        _session = new SessionFeed(dataDirectory, ApplicationVersion, _settings, _statusLog, _writes, Post);
        _terminals = new TerminalManager(integrationDirectory: Path.Combine(dataDirectory, ShellIntegrationFolder));
        _windowClose = new WindowCloseFeed(loop, _terminals, PostNow);
        _agents = new AgentStateFeed(dataDirectory, _terminals, Post);
        _notifier = new AttentionNotifier(PostNow);
        _files = new FileExplorerFeed(() => _settings.Current.Editor, Post, PostBackgroundError);
        _preview = new FilePreviewFeed(() => _settings.Current.Editor, Post, PostBackgroundError);
        _previewRequests = new PreviewRequestFeed(dataDirectory, Post);
        _git = new GitFeed(Post, () => _settings.Current.Git.AutoFetch, PostBackgroundError);
        _worktrees = new WorktreeFeed(Post, () => _settings.Current, _settings.RememberWorktreeFolder, _git.RefreshSoon, PostBackgroundError, dataDirectory);
        _updates = new UpdateFeed(Post, ApplicationVersion, dataDirectory);
        _mcp = new McpFeed(dataDirectory, _terminals.PaneOwnsProcess, Post, PostBackgroundError);
        _browsers = new BrowserFeed(loop, Post, PostNow);
        _settings.ApplyCurrent();
        _terminalFeed = new TerminalFeed(loop, _terminals, _agents.Forget, _queries, Post, PostNow, new TerminalOutputWriter(JsonOptions.Encoder, sendUtf8Line));
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
        else if (json.StartsWith(SessionFeed.TextSavePrefix, StringComparison.Ordinal))
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

    private void Dispatch(BridgeCommandModel command)
    {
        switch (command.Type)
        {
            case "app.ready":
                _windowClose.MarkWebReady();
                _session.SendHello();
                _updates.PostState();
                _mcp.MarkReady();
                break;
            case "session.save":
            case "text.save":
                _session.Handle(command);
                break;
            case "settings.get":
            case "settings.save":
            case "appearance.fontSize":
                _settings.Handle(command);
                break;
            case "attention.raise":
                RaiseAttention(RequirePane(command), command);
                break;
            case "attention.flash":
                _notifier.FlashWhenInactive(_settings.Current.Notifications);
                break;
            case "attention.test":
                TestAttention(RequirePane(command), command);
                break;
            case "agents.installHooks":
                _agents.Hooks.Install();
                _settings.PostResult(false);
                break;
            case "agents.removeHooks":
                _agents.Hooks.Remove();
                _settings.PostResult(false);
                break;
            case "mcp.install":
                _mcp.Install();
                _settings.PostResult(false);
                break;
            case "mcp.remove":
                _mcp.Remove();
                _settings.PostResult(false);
                break;
            case "mcp.response":
                _mcp.Receive(command);
                break;
            case "settings.export":
            case "settings.import":
                _settings.Handle(command);
                break;
            case var type when type.StartsWith(TerminalFeed.Prefix, StringComparison.Ordinal):
                _terminalFeed.Handle(command);
                break;
            case "projects.list":
                ListProjects(_settings.Current);
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
            case "host.closeRequested":
                _windowClose.Handle(command);
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
                _windowClose.Handle(command);
                break;
            default:
                Post(new { type = "error", pane = FailedTerminalPane(command), message = $"Commande inconnue : {command.Type}" });
                break;
        }
    }

    private void RaiseAttention(string paneId, BridgeCommandModel command)
    {
        var settings = _settings.Current.Notifications;
        var done = command.Kind == AttentionDone;
        if (done && !settings.NotifyDone)
        {
            return;
        }

        Notify(paneId, [command.Title ?? "Tily", command.Body, command.Location], done ? settings.DoneSound : settings.Sound, settings, done);
    }

    private void TestAttention(string paneId, BridgeCommandModel command)
    {
        var settings = (command.Notifications?.Deserialize<NotificationSettingsModel>(JsonOptions) ?? _settings.Current.Notifications).Normalized();
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
        _session.UsePersistence(settings.Persistence);
        _terminals.UpdatePaths(_settings.ShellPaths);
        _updates.Configure(settings.Updates.AutoCheck);
    }

    private object DescribeAgents() => _agents.Describe();

    private object DescribeMcp() => _mcp.Describe();

    private object DescribeNotifications() => _notifier.Describe();

    private void ListProjects(SettingsModel settings)
    {
        var root = settings.ProjectsRoot;
        var worktreeFolder = settings.Worktrees.FolderFor(root);
        var projectFolders = settings.WorktreeFolders;
        _queries.Enqueue(() =>
        {
            var projects = ProjectCatalog.List(root, worktreeFolder, projectFolders);
            Post(new { type = "projects.listed", root = projects.Root, projects = projects.Projects, error = projects.Error });
        });
    }

    private void QueryContext(string paneId, string path) =>
        _queries.Enqueue(() => Post(new { type = "context.result", pane = paneId, path, git = GitContext.Resolve(path) }));

    private void PostBackgroundError(Exception exception) => Post(new { type = "error", message = UserErrorMessage.Of(exception) });

    private void OpenFolder(string path, string? target)
    {
        switch (target)
        {
            case "editor":
                LocalActions.OpenInEditor(path, _settings.Current.Editor);
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
