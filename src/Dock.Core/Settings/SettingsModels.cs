using Dock.Core.Agents;
using Dock.Core.Context;
using Dock.Core.Git;
using Dock.Core.Projects;
using Dock.Core.Session;
using Dock.Core.Updates;
using Dock.Core.Worktrees;

namespace Dock.Core.Settings;

public sealed class SettingsModel
{
    public Dictionary<string, string> Shells { get; set; } = new();
    public string Editor { get; set; } = EditorSettingsModel.DefaultCommand;
    public PersistenceSettingsModel Persistence { get; set; } = PersistenceSettingsModel.Default;
    public string ProjectsRoot { get; set; } = ProjectCatalog.DefaultRoot;
    public NotificationSettingsModel Notifications { get; set; } = NotificationSettingsModel.Default;
    public WorktreeSettingsModel Worktrees { get; set; } = WorktreeSettingsModel.Default;
    public GitSettingsModel Git { get; set; } = GitSettingsModel.Default;
    public UpdateSettingsModel Updates { get; set; } = UpdateSettingsModel.Default;
}

public sealed record ShellSettingModel(string Id, string Name, string DefaultExecutable, string Configured, bool Available);

public sealed record SettingsSnapshotModel(SettingsModel Settings, IReadOnlyList<ShellSettingModel> Shells, IReadOnlyDictionary<string, string> Files, IReadOnlyList<string> Warnings);
