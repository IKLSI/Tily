using System.Text.Json;
using Tily.Core.Session;
using Tily.Core.Settings;
using Tily.Core.Shell;

namespace Tily.Host.Bridge;

public sealed class SessionFeed
{
    public const string TextSavePrefix = """{"type":"text.save",""";

    private readonly string _dataDirectory;
    private readonly string _version;
    private readonly SessionRepository _sessions;
    private readonly SettingsFeed _settings;
    private readonly StatusLogFeed _statusLog;
    private readonly BackgroundQueue _writes;
    private readonly Action<object> _post;
    private PaneTextRepository _texts;

    public SessionFeed(string dataDirectory, string version, SettingsFeed settings, StatusLogFeed statusLog, BackgroundQueue writes, Action<object> post)
    {
        _dataDirectory = dataDirectory;
        _version = version;
        _sessions = new SessionRepository(dataDirectory);
        _settings = settings;
        _statusLog = statusLog;
        _writes = writes;
        _post = post;
        _texts = new PaneTextRepository(dataDirectory, PersistenceSettingsModel.Default.MaxTextBytes);
    }

    public void Handle(BridgeCommandModel command)
    {
        switch (command.Type)
        {
            case "session.save":
                SaveSession(command);
                break;
            case "text.save":
                SaveText(command);
                break;
            default:
                throw new InvalidOperationException($"Commande inconnue : {command.Type}");
        }
    }

    public void UsePersistence(PersistenceSettingsModel persistence) =>
        _texts = new PaneTextRepository(_dataDirectory, persistence.MaxTextBytes);

    public void SendHello()
    {
        var loaded = _sessions.Load();
        var session = loaded.Session ?? SessionFactory.Initial();
        _texts.MoveClosedTabText(session);
        var text = _texts.Load();
        var recovery = string.Join(" ", new[] { loaded.Error, text.Error, _statusLog.LoadError }.Where(error => error is not null));
        _post(new
        {
            type = "app.hello",
            version = _version,
            session,
            shells = ShellCatalog.Profiles(_settings.ShellPaths),
            home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            text = text.Text,
            persistence = _settings.Persistence,
            appearance = _settings.Current.Appearance,
            statusLog = _statusLog.Entries(),
            recovery = recovery.Length > 0 ? recovery : null
        });
    }

    private void SaveSession(BridgeCommandModel command)
    {
        var element = command.Session ?? throw new InvalidOperationException("Session manquante.");
        _writes.Enqueue(() =>
        {
            var session = element.Deserialize<SessionModel>(HostBridge.JsonOptions) ?? throw new InvalidOperationException("Session manquante.");
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
            var text = element.Deserialize<Dictionary<string, string>>(HostBridge.JsonOptions) ?? throw new InvalidOperationException("Texte des terminaux manquant.");
            Persist(texts.DirectoryPath, () =>
            {
                texts.Save(text, keep);
                return null;
            });
        });
    }

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
            _post(new { type = "session.saved" });
        }
        else
        {
            _post(new { type = "session.saveFailed", message = failure });
        }
    }
}
