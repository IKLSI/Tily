using System.Text.Json;
using Tily.Core.Session;
using Tily.Core.Settings;
using Tily.Core.Shell;
using Tily.Core.StatusLog;

namespace Tily.Host.Bridge;

public sealed class SettingsFeed
{
    private readonly HostLoop _loop;
    private readonly SettingsService _service;
    private readonly Func<object> _describeAgents;
    private readonly Func<object> _describeMcp;
    private readonly Func<object> _describeNotifications;
    private readonly Action<SettingsModel> _applied;
    private readonly Action<object> _post;
    private readonly Action<object> _postNow;

    public SettingsFeed(HostLoop loop, string dataDirectory, Func<object> describeAgents, Func<object> describeMcp, Func<object> describeNotifications, Action<SettingsModel> applied, Action<object> post, Action<object> postNow)
    {
        _loop = loop;
        _service = new SettingsService(dataDirectory);
        _describeAgents = describeAgents;
        _describeMcp = describeMcp;
        _describeNotifications = describeNotifications;
        _applied = applied;
        _post = post;
        _postNow = postNow;
        Current = _service.Load();
    }

    public SettingsModel Current { get; private set; }

    public ShellPathsModel ShellPaths { get; private set; } = ShellPathsModel.Empty;

    public PersistenceSettingsModel Persistence { get; private set; } = PersistenceSettingsModel.Default;

    public void Handle(BridgeCommandModel command)
    {
        switch (command.Type)
        {
            case "settings.get":
                PostResult(false);
                break;
            case "settings.save":
                Save(command);
                break;
            case "appearance.fontSize":
                SaveFontSize(command);
                break;
            case "settings.export":
                Export(RequirePath(command));
                break;
            case "settings.import":
                Import(RequirePath(command));
                break;
            default:
                throw new InvalidOperationException($"Commande inconnue : {command.Type}");
        }
    }

    public void ApplyCurrent() => Apply(Current);

    public void PostResult(bool saved)
    {
        var snapshot = _service.Snapshot(Current);
        _post(new
        {
            type = "settings.result",
            settings = snapshot.Settings,
            shellSettings = snapshot.Shells,
            files = snapshot.Files,
            warnings = snapshot.Warnings,
            shells = ShellCatalog.Profiles(ShellPaths),
            persistence = Persistence,
            agents = _describeAgents(),
            mcp = _describeMcp(),
            notifications = _describeNotifications(),
            saved
        });
    }

    public void RememberWorktreeFolder(string project, string folder) =>
        _loop.TryEnqueue(() =>
        {
            try
            {
                _service.RememberWorktreeFolder(Current, project, folder);
            }
            catch (Exception exception)
            {
                _postNow(new { type = "error", message = UserErrorMessage.Of(exception) });
            }
        });

    private void Apply(SettingsModel settings)
    {
        Current = settings;
        ShellPaths = SettingsService.ShellPaths(settings);
        Persistence = settings.Persistence;
        _applied(settings);
    }

    private void SaveFontSize(BridgeCommandModel command)
    {
        Current.Appearance = _service.SaveAppearance(Current.Appearance with { FontSize = command.FontSize });
        _post(new { type = "appearance.changed", fontSize = Current.Appearance.FontSize });
    }

    private void Save(BridgeCommandModel command)
    {
        var settings = command.Settings?.Deserialize<SettingsModel>(HostBridge.JsonOptions) ?? throw new InvalidOperationException("Réglages manquants.");
        var result = _service.Save(settings);
        if (!result.IsValid)
        {
            throw new InvalidOperationException($"Réglages refusés : {result.Error}");
        }

        Apply(settings);
        PostResult(true);
    }

    private void Export(string path)
    {
        try
        {
            _service.Export(Current, path);
            _postNow(new { type = "settings.exported", path });
        }
        catch (Exception exception)
        {
            _postNow(new { type = "error", message = $"Export des préférences impossible : {exception.Message}" });
        }
    }

    private void Import(string path)
    {
        try
        {
            var result = _service.Import(path);
            if (result.Settings is null)
            {
                _postNow(new { type = "error", message = result.Error });
                return;
            }

            _postNow(new { type = "settings.imported", settings = result.Settings, path, warnings = result.Warnings });
        }
        catch (Exception exception)
        {
            _postNow(new { type = "error", message = $"Import des préférences impossible : {exception.Message}" });
        }
    }

    private static string RequirePath(BridgeCommandModel command) =>
        command.Path ?? throw new InvalidOperationException("Chemin manquant.");
}
