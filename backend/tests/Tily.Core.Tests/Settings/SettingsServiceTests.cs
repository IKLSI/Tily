using Tily.Core.Agents;
using Tily.Core.Git;
using Tily.Core.Session;
using Tily.Core.Settings;
using Tily.Core.Updates;
using Tily.Core.Worktrees;
using Xunit;

namespace Tily.Core.Tests.Settings;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "tily-tests-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Load_WhenNoFiles_ThenReturnsDefaultsAndWritesTemplates()
    {
        var service = new SettingsService(_directory);

        var settings = service.Load();

        Assert.Equal("code", settings.Editor);
        Assert.Equal(PersistenceSettingsModel.Default, settings.Persistence);
        Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Developer"), settings.ProjectsRoot);
        Assert.Equal(NotificationSettingsModel.Default, settings.Notifications);
        Assert.Equal(GitSettingsModel.Default, settings.Git);
        Assert.Equal(AppearanceSettingsModel.Default, settings.Appearance);
        Assert.Equal(8, Directory.GetFiles(_directory, "*.json").Length);
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsEveryFile()
    {
        var service = new SettingsService(_directory);
        var settings = new SettingsModel
        {
            Shells = new Dictionary<string, string> { ["bash"] = "/outils/bash", ["zsh"] = "  " },
            Editor = " subl ",
            Persistence = new PersistenceSettingsModel(60, 5000, 128),
            ProjectsRoot = _directory,
            Notifications = new NotificationSettingsModel(false, "ping", true)
        };

        var result = service.Save(settings);
        var loaded = service.Load();

        Assert.True(result.IsValid);
        Assert.Equal(new Dictionary<string, string> { ["bash"] = "/outils/bash" }, loaded.Shells);
        Assert.Equal("subl", loaded.Editor);
        Assert.Equal(new PersistenceSettingsModel(60, 5000, 128), loaded.Persistence);
        Assert.Equal(_directory, loaded.ProjectsRoot);
        Assert.Equal(new NotificationSettingsModel(false, "Ping", true), loaded.Notifications);
    }

    [Fact]
    public void Load_WhenNotificationSoundUnknown_ThenFallsBackToDefaultSound()
    {
        var service = new SettingsService(_directory);
        File.WriteAllText(Path.Combine(_directory, NotificationSettingsRepository.FileName), "{ \"systemNotification\": false, \"sound\": \"Klaxon\", \"dockBounce\": false }");

        var settings = service.Load();

        Assert.Equal(new NotificationSettingsModel(false, NotificationSettingsModel.DefaultSound, false), settings.Notifications);
    }

    [Fact]
    public void Load_WhenNotificationFlagsMissing_ThenBothEnabled()
    {
        var service = new SettingsService(_directory);
        File.WriteAllText(Path.Combine(_directory, NotificationSettingsRepository.FileName), "{ \"sound\": \"Ping\" }");

        var settings = service.Load();

        Assert.True(settings.Notifications.SystemNotification);
        Assert.True(settings.Notifications.DockBounce);
    }

    [Fact]
    public void Save_WhenNotificationsSaved_ThenWritesTheirValues()
    {
        var service = new SettingsService(_directory);
        var settings = service.Load();
        settings.ProjectsRoot = _directory;
        settings.Notifications = new NotificationSettingsModel(false, "Ping", false);

        service.Save(settings);
        var written = File.ReadAllText(Path.Combine(_directory, NotificationSettingsRepository.FileName));

        Assert.Contains("\"systemNotification\": false", written);
        Assert.Contains("\"dockBounce\": false", written);
    }

    [Fact]
    public void Load_WhenNotificationSoundIsWavPath_ThenKeepsItAndWarnsIfMissing()
    {
        var service = new SettingsService(_directory);
        File.WriteAllText(Path.Combine(_directory, NotificationSettingsRepository.FileName), "{ \"systemNotification\": true, \"sound\": \"/Sons/ding.WAV\", \"dockBounce\": true }");

        var settings = service.Load();
        var snapshot = service.Snapshot(settings);

        Assert.Equal("/Sons/ding.WAV", settings.Notifications.Sound);
        Assert.Contains(snapshot.Warnings, warning => warning.Contains("/Sons/ding.WAV"));
    }

    [Fact]
    public void Load_WhenDoneSettingsMissing_ThenNotifiesDoneWithDistinctSound()
    {
        var service = new SettingsService(_directory);
        File.WriteAllText(Path.Combine(_directory, NotificationSettingsRepository.FileName), "{ \"systemNotification\": true, \"sound\": \"Notification.Default\", \"dockBounce\": true }");

        var settings = service.Load();

        Assert.Equal(new NotificationSettingsModel(true, NotificationSettingsModel.DefaultSound, true, true, NotificationSettingsModel.DefaultDoneSound), settings.Notifications);
    }

    [Fact]
    public void Load_WhenDoneSoundUnknown_ThenFallsBackToDefaultDoneSound()
    {
        var service = new SettingsService(_directory);
        File.WriteAllText(Path.Combine(_directory, NotificationSettingsRepository.FileName), "{ \"systemNotification\": true, \"sound\": \"none\", \"dockBounce\": true, \"notifyDone\": false, \"doneSound\": \"Klaxon\" }");

        var settings = service.Load();

        Assert.Equal(new NotificationSettingsModel(true, NotificationSettingsModel.NoSound, true, false, NotificationSettingsModel.DefaultDoneSound), settings.Notifications);
    }

    [Fact]
    public void Save_WhenShellUnknown_ThenRefusesWithoutWriting()
    {
        var service = new SettingsService(_directory);
        var settings = new SettingsModel { Shells = new Dictionary<string, string> { ["cmd"] = "/cmd.exe" } };

        var result = service.Save(settings);

        Assert.False(result.IsValid);
        Assert.Equal("Shell inconnu : cmd", result.Error);
        Assert.Empty(Directory.GetFiles(_directory, "*.json"));
    }

    [Fact]
    public void Save_WhenProjectsRootRelative_ThenRefuses()
    {
        var service = new SettingsService(_directory);

        var result = service.Save(new SettingsModel { ProjectsRoot = "Projets" });

        Assert.False(result.IsValid);
        Assert.Contains("chemin absolu", result.Error);
    }

    [Fact]
    public void Snapshot_WhenPathsMissing_ThenWarnsWithoutFailing()
    {
        var service = new SettingsService(_directory);
        var settings = new SettingsModel
        {
            Shells = new Dictionary<string, string> { ["bash"] = "/introuvable/bash" },
            ProjectsRoot = "/introuvable/projets"
        };

        var snapshot = service.Snapshot(settings);

        Assert.False(snapshot.Shells.Single(shell => shell.Id == "bash").Available);
        Assert.Contains(snapshot.Warnings, warning => warning.Contains("/introuvable/bash"));
        Assert.Contains(snapshot.Warnings, warning => warning.Contains("/introuvable/projets"));
        Assert.Equal(8, snapshot.Files.Count);
    }

    [Fact]
    public void Export_ThenImport_RoundTripsWithoutTouchingSettingsFiles()
    {
        var service = new SettingsService(_directory);
        var settings = new SettingsModel
        {
            Shells = new Dictionary<string, string> { ["bash"] = "/outils/bash" },
            Editor = "subl",
            Persistence = new PersistenceSettingsModel(45, 2000, 64),
            ProjectsRoot = _directory
        };
        var exportPath = Path.Combine(_directory, "export", "prefs.json");
        Directory.CreateDirectory(Path.GetDirectoryName(exportPath)!);

        service.Export(settings, exportPath);
        var result = service.Import(exportPath);

        Assert.Null(result.Error);
        Assert.Equal(settings.Shells, result.Settings!.Shells);
        Assert.Equal("subl", result.Settings.Editor);
        Assert.Equal(new PersistenceSettingsModel(45, 2000, 64), result.Settings.Persistence);
        Assert.Equal(_directory, result.Settings.ProjectsRoot);
        Assert.Empty(Directory.GetFiles(_directory, "*.json"));
    }

    [Fact]
    public void Import_WhenVersionUnsupported_ThenRefuses()
    {
        var service = new SettingsService(_directory);
        var path = Path.Combine(_directory, "prefs.json");
        File.WriteAllText(path, "{ \"version\": 2, \"shells\": {}, \"editor\": \"code\", \"persistence\": { \"textIntervalSeconds\": 30, \"linesPerPane\": 1000, \"maxTextMebibytes\": 32 }, \"projectsRoot\": \"/Users/moi/Projets\" }");

        var result = service.Import(path);

        Assert.Null(result.Settings);
        Assert.Equal("Version de préférences non prise en charge : 2 (attendue : 1).", result.Error);
    }

    [Fact]
    public void Import_WhenKeyMissing_ThenRefuses()
    {
        var service = new SettingsService(_directory);
        var path = Path.Combine(_directory, "prefs.json");
        File.WriteAllText(path, "{ \"version\": 1, \"shells\": {}, \"editor\": \"code\", \"projectsRoot\": \"/Users/moi/Projets\" }");

        var result = service.Import(path);

        Assert.Equal("Le fichier de préférences est incomplet : clé « persistence » absente.", result.Error);
    }

    [Fact]
    public void Import_WhenNotificationsMissing_ThenUsesDefaults()
    {
        var service = new SettingsService(_directory);
        var path = Path.Combine(_directory, "prefs.json");
        File.WriteAllText(path, "{ \"version\": 1, \"shells\": {}, \"editor\": \"code\", \"persistence\": { \"textIntervalSeconds\": 30, \"linesPerPane\": 1000, \"maxTextMebibytes\": 32 }, \"projectsRoot\": \"/Users/moi/Projets\" }");

        var result = service.Import(path);

        Assert.Null(result.Error);
        Assert.Equal(NotificationSettingsModel.Default, result.Settings!.Notifications);
    }

    [Fact]
    public void Import_WhenPersistenceOutOfRange_ThenClampsValues()
    {
        var service = new SettingsService(_directory);
        var path = Path.Combine(_directory, "prefs.json");
        File.WriteAllText(path, "{ \"version\": 1, \"shells\": {}, \"editor\": \"code\", \"persistence\": { \"textIntervalSeconds\": 3000, \"linesPerPane\": 100, \"maxTextMebibytes\": 64 }, \"projectsRoot\": \"/Users/moi/Projets\" }");

        var result = service.Import(path);

        Assert.Equal(new PersistenceSettingsModel(600, 500, 64), result.Settings!.Persistence);
    }

    [Fact]
    public void Import_WhenPersistenceOutOfRange_ThenWarnsForEachClampedValue()
    {
        var service = new SettingsService(_directory);
        var path = Path.Combine(_directory, "prefs.json");
        File.WriteAllText(path, "{ \"version\": 1, \"shells\": {}, \"editor\": \"code\", \"persistence\": { \"textIntervalSeconds\": 3000, \"linesPerPane\": 100, \"maxTextMebibytes\": 64 }, \"projectsRoot\": \"/Users/moi/Projets\" }");

        var result = service.Import(path);

        Assert.Equal(
            [
                "Sauvegarde du texte (secondes) : 3000 ramené à 600 (entre 5 et 600).",
                "Lignes conservées par pane : 100 ramené à 500 (entre 500 et 100000)."
            ],
            result.Warnings);
    }

    [Fact]
    public void Import_WhenPersistenceWithinBounds_ThenReportsNoWarning()
    {
        var service = new SettingsService(_directory);
        var path = Path.Combine(_directory, "prefs.json");
        File.WriteAllText(path, "{ \"version\": 1, \"shells\": {}, \"editor\": \"code\", \"persistence\": { \"textIntervalSeconds\": 30, \"linesPerPane\": 1000, \"maxTextMebibytes\": 32 }, \"projectsRoot\": \"/Users/moi/Projets\" }");

        var result = service.Import(path);

        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Import_WhenJsonInvalid_ThenRefuses()
    {
        var service = new SettingsService(_directory);
        var path = Path.Combine(_directory, "prefs.json");
        File.WriteAllText(path, "{ version: ");

        var result = service.Import(path);

        Assert.Null(result.Settings);
        Assert.StartsWith("Le fichier de préférences est illisible", result.Error);
    }

    [Fact]
    public void Import_WhenWorktreesMissing_ThenUsesDefaults()
    {
        var service = new SettingsService(_directory);
        var path = Path.Combine(_directory, "prefs.json");
        File.WriteAllText(path, "{ \"version\": 1, \"shells\": {}, \"editor\": \"code\", \"persistence\": { \"textIntervalSeconds\": 30, \"linesPerPane\": 1000, \"maxTextMebibytes\": 32 }, \"projectsRoot\": \"/Users/moi/Projets\" }");

        var result = service.Import(path);

        Assert.Equal(WorktreeSettingsModel.Default, result.Settings!.Worktrees);
    }

    [Fact]
    public void Save_ThenLoad_KeepsWorktreeSettingsInProjectsFile()
    {
        var service = new SettingsService(_directory);
        var settings = service.Load();
        settings.Worktrees = new WorktreeSettingsModel(" /wt ", " main ");

        service.Save(settings);

        Assert.Equal(new WorktreeSettingsModel("/wt", "main"), service.Load().Worktrees);
    }

    [Fact]
    public void RememberWorktreeFolder_ThenLoadKeepsItWithoutTouchingOtherSettings()
    {
        var service = new SettingsService(_directory);
        var settings = service.Load();
        settings.Worktrees = new WorktreeSettingsModel("/wt", "main");
        service.Save(settings);

        service.RememberWorktreeFolder(settings, "/Projets/tily", "/arbres");

        var loaded = service.Load();
        Assert.Equal(("/wt", "main"), (loaded.Worktrees.Folder, loaded.Worktrees.DefaultBase));
        Assert.Equal(new WorktreeProjectFolderModel("/Projets/tily", "/arbres"), Assert.Single(loaded.WorktreeFolders));
        Assert.Equal("/arbres", settings.WorktreeFolderOf("/projets/TILY/"));
    }

    [Fact]
    public void RememberWorktreeFolder_WhenFolderIsDefault_ThenForgetsProjectFolder()
    {
        var service = new SettingsService(_directory);
        var settings = service.Load();
        settings.ProjectsRoot = "/Projets";
        service.RememberWorktreeFolder(settings, "/Projets/tily", "/arbres");

        service.RememberWorktreeFolder(settings, "/Projets/tily", "/Projets/worktrees/");

        Assert.Empty(service.Load().WorktreeFolders);
    }

    [Fact]
    public void Save_WhenProjectFolderRemoved_ThenForgetsItButKeepsRepository()
    {
        var service = new SettingsService(_directory);
        var projects = new WorktreeProjectsRepository(_directory);
        projects.SaveRepository("/Projets/tily", "/Projets/tily/app");
        var settings = service.Load();
        service.RememberWorktreeFolder(settings, "/Projets/tily", "/arbres");

        settings.WorktreeFolders = [];
        service.Save(settings);

        Assert.Empty(service.Load().WorktreeFolders);
        Assert.Equal("/Projets/tily/app", projects.RepositoryOf("/Projets/tily"));
    }

    [Fact]
    public void Save_WhenProjectFolderRelative_ThenRefusesInFrench()
    {
        var service = new SettingsService(_directory);
        var settings = service.Load();
        settings.WorktreeFolders = [new WorktreeProjectFolderModel("/Projets/tily", "relatif")];

        var result = service.Save(settings);

        Assert.Equal("Le dossier des worktrees de /Projets/tily doit être un chemin absolu : relatif", result.Error);
    }

    [Fact]
    public void Save_WhenWorktreeFolderRelative_ThenRefuses()
    {
        var service = new SettingsService(_directory);
        var settings = service.Load();
        settings.Worktrees = new WorktreeSettingsModel("relatif", "develop");

        var result = service.Save(settings);

        Assert.Equal("Le dossier des worktrees doit être un chemin absolu : relatif", result.Error);
    }

    [Fact]
    public void Save_ThenLoad_KeepsGitSettingsInGitFile()
    {
        var service = new SettingsService(_directory);
        var settings = service.Load();
        settings.Git = new GitSettingsModel(false);

        service.Save(settings);

        Assert.Equal(new GitSettingsModel(false), service.Load().Git);
    }

    [Fact]
    public void Load_WhenGitFileHasNoAutoFetch_ThenEnablesAutoFetch()
    {
        var service = new SettingsService(_directory);
        File.WriteAllText(Path.Combine(_directory, GitSettingsRepository.FileName), "{}");

        var settings = service.Load();

        Assert.True(settings.Git.AutoFetch, "Le fetch automatique est activé par défaut.");
    }

    [Fact]
    public void Import_WhenGitMissing_ThenUsesDefaults()
    {
        var service = new SettingsService(_directory);
        var path = Path.Combine(_directory, "prefs.json");
        File.WriteAllText(path, "{ \"version\": 1, \"shells\": {}, \"editor\": \"code\", \"persistence\": { \"textIntervalSeconds\": 30, \"linesPerPane\": 1000, \"maxTextMebibytes\": 32 }, \"projectsRoot\": \"/Users/moi/Projets\" }");

        var result = service.Import(path);

        Assert.Equal(GitSettingsModel.Default, result.Settings!.Git);
    }

    [Fact]
    public void Export_ThenImport_KeepsGitSettings()
    {
        var service = new SettingsService(_directory);
        var settings = new SettingsModel { ProjectsRoot = _directory, Git = new GitSettingsModel(false) };
        var exportPath = Path.Combine(_directory, "prefs.json");

        service.Export(settings, exportPath);
        var result = service.Import(exportPath);

        Assert.Equal(new GitSettingsModel(false), result.Settings!.Git);
    }

    [Fact]
    public void Import_WhenUpdatesMissing_ThenKeepsAutomaticCheck()
    {
        var service = new SettingsService(_directory);
        var path = Path.Combine(_directory, "prefs.json");
        File.WriteAllText(path, "{ \"version\": 1, \"shells\": {}, \"editor\": \"code\", \"persistence\": { \"textIntervalSeconds\": 30, \"linesPerPane\": 1000, \"maxTextMebibytes\": 32 }, \"projectsRoot\": \"/Users/moi/Projets\" }");

        var result = service.Import(path);

        Assert.Equal(UpdateSettingsModel.Default, result.Settings!.Updates);
    }

    [Fact]
    public void Save_ThenLoad_KeepsAutomaticCheckDisabled()
    {
        var service = new SettingsService(_directory);
        var settings = service.Load();
        settings.Updates = new UpdateSettingsModel(false);

        service.Save(settings);

        Assert.False(service.Load().Updates.AutoCheck);
    }

    [Fact]
    public void Load_WhenUpdatesFileEmptyObject_ThenChecksAutomatically()
    {
        var service = new SettingsService(_directory);
        File.WriteAllText(Path.Combine(_directory, UpdateSettingsRepository.FileName), "{}");

        var settings = service.Load();

        Assert.True(settings.Updates.AutoCheck);
    }

    [Fact]
    public void Save_ThenLoad_KeepsFontSizeClampedInAppearanceFile()
    {
        var service = new SettingsService(_directory);
        var settings = service.Load();
        settings.Appearance = new AppearanceSettingsModel(99);

        service.Save(settings);

        Assert.Equal(new AppearanceSettingsModel(AppearanceSettingsModel.MaxFontSize), service.Load().Appearance);
    }

    [Fact]
    public void SaveAppearance_WhenFontSizeTooSmall_ThenWritesOnlyClampedAppearanceFile()
    {
        var service = new SettingsService(_directory);

        var saved = service.SaveAppearance(new AppearanceSettingsModel(3));

        Assert.Equal(new AppearanceSettingsModel(AppearanceSettingsModel.MinFontSize), saved);
        Assert.Equal([AppearanceSettingsRepository.FileName], Directory.GetFiles(_directory).Select(Path.GetFileName));
        Assert.Equal(saved, service.Load().Appearance);
    }

    [Fact]
    public void Load_WhenAppearanceFileEmptyObject_ThenDefaultFontSize()
    {
        var service = new SettingsService(_directory);
        File.WriteAllText(Path.Combine(_directory, AppearanceSettingsRepository.FileName), "{}");

        var settings = service.Load();

        Assert.Equal(AppearanceSettingsModel.DefaultFontSize, settings.Appearance.FontSize);
    }

    [Fact]
    public void Export_ThenImport_KeepsFontSize()
    {
        var service = new SettingsService(_directory);
        var settings = new SettingsModel { ProjectsRoot = _directory, Appearance = new AppearanceSettingsModel(18) };
        var exportPath = Path.Combine(_directory, "prefs.json");

        service.Export(settings, exportPath);
        var result = service.Import(exportPath);

        Assert.Equal(new AppearanceSettingsModel(18), result.Settings!.Appearance);
    }

    [Fact]
    public void Load_WhenAppearanceFileHasOnlyFontSize_ThenDefaultFontFamily()
    {
        var service = new SettingsService(_directory);
        File.WriteAllText(Path.Combine(_directory, AppearanceSettingsRepository.FileName), "{ \"fontSize\": 16 }");

        var settings = service.Load();

        Assert.Equal(new AppearanceSettingsModel(16, AppearanceSettingsModel.DefaultFontFamily), settings.Appearance);
    }

    [Theory]
    [InlineData("hack nerd font ", "Hack Nerd Font")]
    [InlineData("Comic Sans MS", AppearanceSettingsModel.DefaultFontFamily)]
    [InlineData("", AppearanceSettingsModel.DefaultFontFamily)]
    public void SaveAppearance_WhenFontFamilyGiven_ThenKeepsKnownFontOrFallsBackToDefault(string fontFamily, string expected)
    {
        var service = new SettingsService(_directory);

        var saved = service.SaveAppearance(new AppearanceSettingsModel(14, fontFamily));

        Assert.Equal(expected, saved.FontFamily);
        Assert.Equal(saved, service.Load().Appearance);
    }

    [Fact]
    public void Export_ThenImport_KeepsFontFamily()
    {
        var service = new SettingsService(_directory);
        var settings = new SettingsModel { ProjectsRoot = _directory, Appearance = new AppearanceSettingsModel(14, "Monaco") };
        var exportPath = Path.Combine(_directory, "prefs.json");

        service.Export(settings, exportPath);
        var result = service.Import(exportPath);

        Assert.Equal("Monaco", result.Settings!.Appearance.FontFamily);
    }

    [Fact]
    public void Import_WhenAppearanceMissing_ThenDefaultFontSize()
    {
        var service = new SettingsService(_directory);
        var path = Path.Combine(_directory, "prefs.json");
        File.WriteAllText(path, "{ \"version\": 1, \"shells\": {}, \"editor\": \"code\", \"persistence\": { \"textIntervalSeconds\": 30, \"linesPerPane\": 1000, \"maxTextMebibytes\": 32 }, \"projectsRoot\": \"/Users/moi/Projets\" }");

        var result = service.Import(path);

        Assert.Equal(AppearanceSettingsModel.Default, result.Settings!.Appearance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
