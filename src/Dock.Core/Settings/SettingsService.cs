using System.Text.Json;
using Dock.Core.Agents;
using Dock.Core.Context;
using Dock.Core.Git;
using Dock.Core.Projects;
using Dock.Core.Session;
using Dock.Core.Shell;
using Dock.Core.Worktrees;

namespace Dock.Core.Settings;

public sealed class SettingsService
{
    private readonly ShellPathsRepository _shells;
    private readonly EditorSettingsRepository _editor;
    private readonly PersistenceSettingsRepository _persistence;
    private readonly ProjectsSettingsRepository _projects;
    private readonly NotificationSettingsRepository _notifications;
    private readonly GitSettingsRepository _git;

    public SettingsService(string directory)
    {
        _shells = new ShellPathsRepository(directory);
        _editor = new EditorSettingsRepository(directory);
        _persistence = new PersistenceSettingsRepository(directory);
        _projects = new ProjectsSettingsRepository(directory);
        _notifications = new NotificationSettingsRepository(directory);
        _git = new GitSettingsRepository(directory);
    }

    public SettingsModel Load()
    {
        var projects = _projects.Load();
        return new SettingsModel
        {
            Shells = _shells.Load().Executables.ToDictionary(pair => pair.Key, pair => pair.Value),
            Editor = _editor.Load().Command,
            Persistence = _persistence.Load(),
            ProjectsRoot = projects.Root,
            Notifications = _notifications.Load(),
            Worktrees = projects.Worktrees ?? WorktreeSettingsModel.Default,
            Git = _git.Load()
        };
    }

    public ValidationResultModel Validate(SettingsModel settings)
    {
        var knownShells = ShellCatalog.Profiles().Select(profile => profile.Id).ToHashSet();
        var unknown = settings.Shells.Keys.FirstOrDefault(id => !knownShells.Contains(id));
        if (unknown is not null)
        {
            return ValidationResultModel.Fail($"Shell inconnu : {unknown}");
        }

        if (string.IsNullOrWhiteSpace(settings.Editor))
        {
            return ValidationResultModel.Fail("La commande de l’éditeur est vide.");
        }

        if (string.IsNullOrWhiteSpace(settings.ProjectsRoot))
        {
            return ValidationResultModel.Fail("Le dossier des projets est vide.");
        }

        if (!Path.IsPathRooted(settings.ProjectsRoot))
        {
            return ValidationResultModel.Fail($"Le dossier des projets doit être un chemin absolu : {settings.ProjectsRoot}");
        }

        return (settings.Worktrees ?? WorktreeSettingsModel.Default).Error() is { } worktreeError
            ? ValidationResultModel.Fail(worktreeError)
            : ValidationResultModel.Ok();
    }

    public ValidationResultModel Save(SettingsModel settings)
    {
        var result = Validate(settings);
        if (!result.IsValid)
        {
            return result;
        }

        settings.Shells = settings.Shells.Where(pair => !string.IsNullOrWhiteSpace(pair.Value)).ToDictionary(pair => pair.Key, pair => pair.Value.Trim());
        settings.Editor = settings.Editor.Trim();
        settings.Persistence = settings.Persistence.Clamped();
        settings.ProjectsRoot = settings.ProjectsRoot.Trim();
        settings.Notifications = settings.Notifications.Normalized();
        settings.Worktrees = (settings.Worktrees ?? WorktreeSettingsModel.Default).Normalized();
        settings.Git ??= GitSettingsModel.Default;
        _shells.Save(settings.Shells);
        _editor.Save(new EditorSettingsModel(settings.Editor));
        _persistence.Save(settings.Persistence);
        _projects.Save(new ProjectsSettingsModel(settings.ProjectsRoot, settings.Worktrees));
        _notifications.Save(settings.Notifications);
        _git.Save(settings.Git);
        return result;
    }

    public void Export(SettingsModel settings, string filePath) =>
        AtomicFile.Write(filePath, JsonSerializer.Serialize(PreferencesDocumentModel.From(settings), SessionRepository.JsonOptions));

    public PreferencesImportResultModel Import(string filePath)
    {
        PreferencesDocumentModel? document;
        try
        {
            document = JsonSerializer.Deserialize<PreferencesDocumentModel>(File.ReadAllText(filePath), SessionRepository.JsonOptions);
        }
        catch (JsonException exception)
        {
            return PreferencesImportResultModel.Failed($"Le fichier de préférences est illisible : {exception.Message}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return PreferencesImportResultModel.Failed($"Impossible de lire {filePath} : {exception.Message}");
        }

        if (document is null)
        {
            return PreferencesImportResultModel.Failed("Le fichier de préférences est vide.");
        }

        if (document.Version != PreferencesDocumentModel.CurrentVersion)
        {
            var version = document.Version?.ToString() ?? "absente";
            return PreferencesImportResultModel.Failed($"Version de préférences non prise en charge : {version} (attendue : {PreferencesDocumentModel.CurrentVersion}).");
        }

        var missing = document.MissingKey();
        if (missing is not null)
        {
            return PreferencesImportResultModel.Failed($"Le fichier de préférences est incomplet : clé « {missing} » absente.");
        }

        var settings = new SettingsModel
        {
            Shells = document.Shells!,
            Editor = document.Editor!,
            Persistence = document.Persistence!.Clamped(),
            ProjectsRoot = document.ProjectsRoot!,
            Notifications = (document.Notifications ?? NotificationSettingsModel.Default).Normalized(),
            Worktrees = (document.Worktrees ?? WorktreeSettingsModel.Default).Normalized(),
            Git = document.Git ?? GitSettingsModel.Default
        };
        var validation = Validate(settings);
        return validation.IsValid
            ? new PreferencesImportResultModel(settings, null, document.Persistence!.OutOfRangeWarnings())
            : PreferencesImportResultModel.Failed($"Préférences refusées : {validation.Error}");
    }

    public SettingsSnapshotModel Snapshot(SettingsModel settings)
    {
        var paths = ShellPaths(settings);
        var defaults = ShellCatalog.Profiles(ShellPathsModel.Empty).ToDictionary(profile => profile.Id, profile => profile.Executable);
        var shells = ShellCatalog.Profiles(paths)
            .Select(profile => new ShellSettingModel(profile.Id, profile.Name, defaults[profile.Id], paths.ExecutableFor(profile.Id) ?? string.Empty, profile.Available))
            .ToList();
        var warnings = shells.Where(shell => !shell.Available).Select(shell => $"Le shell « {shell.Name} » est introuvable : {(shell.Configured.Length > 0 ? shell.Configured : shell.DefaultExecutable)}").ToList();
        if (Path.IsPathRooted(settings.Editor) && !File.Exists(settings.Editor))
        {
            warnings.Add($"La commande de l’éditeur est introuvable : {settings.Editor}");
        }

        if (settings.Notifications.UsesFile && !File.Exists(settings.Notifications.Sound))
        {
            warnings.Add($"Le fichier son est introuvable : {settings.Notifications.Sound}");
        }

        if (NotificationSettingsModel.IsWavPath(settings.Notifications.DoneSound) && !File.Exists(settings.Notifications.DoneSound))
        {
            warnings.Add($"Le fichier son de fin est introuvable : {settings.Notifications.DoneSound}");
        }

        if (!Directory.Exists(settings.ProjectsRoot))
        {
            warnings.Add($"Le dossier des projets est introuvable : {settings.ProjectsRoot}");
        }

        var worktreeFolder = settings.Worktrees.FolderFor(settings.ProjectsRoot);
        if (!string.IsNullOrWhiteSpace(settings.Worktrees.Folder) && !Directory.Exists(worktreeFolder))
        {
            warnings.Add($"Le dossier des worktrees est introuvable : {worktreeFolder}");
        }

        var files = new Dictionary<string, string>
        {
            ["shells"] = _shells.FilePath,
            ["editor"] = _editor.FilePath,
            ["persistence"] = _persistence.FilePath,
            ["projects"] = _projects.FilePath,
            ["notifications"] = _notifications.FilePath,
            ["git"] = _git.FilePath
        };
        return new SettingsSnapshotModel(settings, shells, files, warnings);
    }

    public static ShellPathsModel ShellPaths(SettingsModel settings) =>
        new(settings.Shells.Where(pair => !string.IsNullOrWhiteSpace(pair.Value)).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase));
}
