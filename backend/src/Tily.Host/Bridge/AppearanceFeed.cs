using System.Text.Json;
using Tily.Core.Appearance;
using Tily.Core.Session;
using Tily.Core.Settings;

namespace Tily.Host.Bridge;

public sealed class AppearanceFeed
{
    private readonly SettingsFeed _settings;
    private readonly Action<object> _post;
    private readonly BackgroundQueue _queue;
    private IReadOnlyList<string> _paths = [];
    private string? _postedState;
    private string? _postedProjects;

    public AppearanceFeed(SettingsFeed settings, Action<object> post, Action<Exception> onError)
    {
        _settings = settings;
        _post = post;
        _queue = new BackgroundQueue(onError);
    }

    public object Hello()
    {
        var state = AccentResolver.Resolve(_settings.Current.Appearance);
        _postedState = JsonSerializer.Serialize(state, SessionRepository.JsonOptions);
        _queue.Enqueue(() => _postedProjects = null);
        return new { state, accentPresets = AccentPresets.All };
    }

    public void Handle(BridgeCommandModel command)
    {
        switch (command.Type)
        {
            case "appearance.projects":
                _paths = command.Paths ?? [];
                RefreshProjects();
                break;
            case "appearance.projectColor":
                _settings.SaveProjectColor(command.Project ?? throw new InvalidOperationException("Projet manquant."), command.Color);
                RefreshProjects();
                break;
            case "appearance.preview":
                Preview(command.Color, command.ColorScheme);
                break;
            default:
                throw new InvalidOperationException($"Commande inconnue : {command.Type}");
        }
    }

    public void Refresh() => PostTheme(_settings.Current.Appearance);

    private void Preview(string? accent, string? colorScheme)
    {
        var current = _settings.Current.Appearance;
        PostTheme((current with { Accent = accent, ColorScheme = colorScheme ?? current.ColorScheme }).Normalized());
    }

    private void PostTheme(AppearanceSettingsModel appearance)
    {
        var state = AccentResolver.Resolve(appearance);
        var serialized = JsonSerializer.Serialize(state, SessionRepository.JsonOptions);
        if (serialized != _postedState)
        {
            _postedState = serialized;
            _post(new { type = "appearance.theme", state });
        }

        RefreshProjects();
    }

    private void RefreshProjects()
    {
        var settings = _settings.Current;
        var paths = _paths;
        _queue.Enqueue(() =>
        {
            var projects = AccentResolver.ProjectsOf(settings, paths);
            var serialized = JsonSerializer.Serialize(projects, SessionRepository.JsonOptions);
            if (serialized == _postedProjects)
            {
                return;
            }

            _postedProjects = serialized;
            _post(new { type = "appearance.projectsResolved", projects });
        });
    }
}
