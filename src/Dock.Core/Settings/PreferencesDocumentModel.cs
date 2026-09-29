using Dock.Core.Agents;
using Dock.Core.Session;

namespace Dock.Core.Settings;

public sealed class PreferencesDocumentModel
{
    public const int CurrentVersion = 1;

    public int? Version { get; set; }
    public Dictionary<string, string>? Shells { get; set; }
    public string? Editor { get; set; }
    public PersistenceSettingsModel? Persistence { get; set; }
    public string? ProjectsRoot { get; set; }
    public NotificationSettingsModel? Notifications { get; set; }

    public static PreferencesDocumentModel From(SettingsModel settings) => new()
    {
        Version = CurrentVersion,
        Shells = settings.Shells,
        Editor = settings.Editor,
        Persistence = settings.Persistence,
        ProjectsRoot = settings.ProjectsRoot,
        Notifications = settings.Notifications
    };

    public string? MissingKey()
    {
        if (Shells is null)
        {
            return "shells";
        }

        if (Editor is null)
        {
            return "editor";
        }

        if (Persistence is null)
        {
            return "persistence";
        }

        return ProjectsRoot is null ? "projectsRoot" : null;
    }
}

public sealed record PreferencesImportResultModel(SettingsModel? Settings, string? Error, IReadOnlyList<string> Warnings)
{
    public static PreferencesImportResultModel Failed(string error) => new(null, error, []);
}
