using Tily.Core.Git;
using Tily.Core.Tests.Git;
using Tily.Core.Worktrees;
using Xunit;

namespace Tily.Core.Tests.Worktrees;

public sealed class WorktreeCreatorTests : IDisposable
{
    private readonly GitSandbox _sandbox = new();
    private readonly WorktreeSettingsModel _settings;

    public WorktreeCreatorTests()
    {
        _sandbox.Commit("Base", ("a.txt", "a\n"));
        _sandbox.Git("branch", "develop");
        _sandbox.CreateRemote();
        _sandbox.Git("push", "-q", "origin", "main", "develop");
        _settings = new WorktreeSettingsModel(Path.Combine(_sandbox.Root, "worktrees"), "develop");
    }

    [Fact]
    public void Create_WhenNewBranchFromDefaultBase_ThenChecksItOutWithoutUpstream()
    {
        var creation = Create(new WorktreeRequestModel(_sandbox.Work, "feat/vue", WorktreeBranchMode.New, null));

        Assert.Equal(Path.Combine(_settings.Folder, "dépôt avec espaces-vue"), creation.Path);
        Assert.Equal("feat/vue", _sandbox.GitIn(creation.Path, "symbolic-ref", "--short", "HEAD").Trim());
        Assert.Null(GitRepository.ValueOf(_sandbox.Runner.Run(creation.Path, ["config", "--get", "branch.feat/vue.merge"])));
    }

    [Fact]
    public void Create_WhenExistingLocalBranch_ThenChecksItOut()
    {
        _sandbox.Git("branch", "correctif");

        var creation = Create(new WorktreeRequestModel(_sandbox.Work, "correctif", WorktreeBranchMode.Local, null));

        Assert.Equal("correctif", _sandbox.GitIn(creation.Path, "symbolic-ref", "--short", "HEAD").Trim());
    }

    [Fact]
    public void Create_WhenRemoteBranch_ThenCreatesTrackingBranch()
    {
        _sandbox.Git("push", "-q", "origin", "main:distante");
        _sandbox.Git("fetch", "-q", "origin");

        var creation = Create(new WorktreeRequestModel(_sandbox.Work, "origin/distante", WorktreeBranchMode.Remote, null));

        Assert.Equal("origin/distante", _sandbox.GitIn(creation.Path, "rev-parse", "--abbrev-ref", "distante@{upstream}").Trim());
    }

    [Fact]
    public void Plan_WhenNewBranchAlreadyExists_ThenExplainsInFrench()
    {
        var plan = Plan(new WorktreeRequestModel(_sandbox.Work, "develop", WorktreeBranchMode.New, null));

        Assert.Equal("La branche « develop » existe déjà : choisissez « Branche existante ».", plan.Error);
    }

    [Fact]
    public void Plan_WhenBranchUsedByAnotherWorktree_ThenRefuses()
    {
        var plan = Plan(new WorktreeRequestModel(_sandbox.Work, "main", WorktreeBranchMode.Local, null));

        Assert.Equal($"La branche « main » est déjà utilisée par le worktree {_sandbox.Work}.", plan.Error);
    }

    [Fact]
    public void Plan_WhenNameInvalid_ThenRefuses()
    {
        var plan = Plan(new WorktreeRequestModel(_sandbox.Work, "mauvais..nom", WorktreeBranchMode.New, null));

        Assert.Equal("Nom de branche invalide : « mauvais..nom ».", plan.Error);
    }

    [Fact]
    public void Plan_WhenFolderAlreadyExists_ThenRefuses()
    {
        var existing = Path.Combine(_settings.Folder, "dépôt avec espaces-vue");
        Directory.CreateDirectory(existing);

        var plan = Plan(new WorktreeRequestModel(_sandbox.Work, "feat/vue", WorktreeBranchMode.New, null));

        Assert.Equal($"Le dossier existe déjà : {existing}", plan.Error);
    }

    [Fact]
    public void Plan_WhenValid_ThenGivesPathAndRemoteDefaultBase()
    {
        var plan = Plan(new WorktreeRequestModel(_sandbox.Work, "feat/vue", WorktreeBranchMode.New, null));

        Assert.Equal((Path.Combine(_settings.Folder, "dépôt avec espaces-vue"), "origin/develop", null), (plan.Path, plan.DefaultBase, plan.Error));
    }

    [Fact]
    public void Plan_WhenFolderChosen_ThenTargetsIt()
    {
        var folder = Path.Combine(_sandbox.Root, "ailleurs");

        var plan = Plan(new WorktreeRequestModel(_sandbox.Work, "feat/vue", WorktreeBranchMode.New, null, folder));

        Assert.Equal((folder, Path.Combine(folder, "dépôt avec espaces-vue")), (plan.Folder, plan.Path));
    }

    [Fact]
    public void Plan_WhenBranchEmpty_ThenPreviewsFinalFolder()
    {
        var plan = Plan(new WorktreeRequestModel(_sandbox.Work, string.Empty, WorktreeBranchMode.New, null));

        Assert.Equal((null, Path.Combine(_settings.Folder, "dépôt avec espaces-<branche>")), (plan.Path, plan.PathPreview));
    }

    [Fact]
    public void Plan_WhenFolderRelative_ThenExplainsInFrench()
    {
        var plan = Plan(new WorktreeRequestModel(_sandbox.Work, "feat/vue", WorktreeBranchMode.New, null, "relatif"));

        Assert.Equal(("Le dossier des worktrees doit être un chemin absolu : relatif", null), (plan.Error, plan.Path));
    }

    [Fact]
    public void Create_WhenFolderChosen_ThenCreatesWorktreeInIt()
    {
        var folder = Path.Combine(_sandbox.Root, "ailleurs");

        var creation = Create(new WorktreeRequestModel(_sandbox.Work, "feat/vue", WorktreeBranchMode.New, null, folder));

        Assert.Equal(Path.Combine(folder, "dépôt avec espaces-vue"), creation.Path);
        Assert.True(Directory.Exists(creation.Path));
    }

    [Fact]
    public void Plan_WhenConfiguredBaseMissing_ThenFallsBackToRemoteMain()
    {
        var settings = new WorktreeSettingsModel(_settings.Folder, "absente");

        var plan = WorktreeCreator.Plan(_sandbox.Runner, new WorktreeRequestModel(_sandbox.Work, "feat/vue", WorktreeBranchMode.New, null), settings, _sandbox.Root);

        Assert.Equal(("origin/main", "origin/absente"), (plan.DefaultBase, plan.ConfiguredBase));
    }

    [Fact]
    public void Plan_WhenConfiguredBaseIsTagWithoutRemote_ThenKeepsIt()
    {
        using var local = new GitSandbox();
        local.Commit("Base", ("a.txt", "a\n"));
        local.Git("tag", "v1");
        var settings = new WorktreeSettingsModel(Path.Combine(local.Root, "worktrees"), "v1");

        var plan = WorktreeCreator.Plan(local.Runner, new WorktreeRequestModel(local.Work, "feat/vue", WorktreeBranchMode.New, null), settings, local.Root);

        Assert.Equal(("v1", null), (plan.DefaultBase, plan.Error));
    }

    [Fact]
    public void Create_WhenConfiguredBaseMissing_ThenStartsFromRemoteMain()
    {
        var settings = new WorktreeSettingsModel(_settings.Folder, "absente");
        var main = _sandbox.Git("rev-parse", "origin/main").Trim();

        var creation = WorktreeCreator.Create(_sandbox.Runner, new WorktreeRequestModel(_sandbox.Work, "feat/vue", WorktreeBranchMode.New, null), settings, _sandbox.Root, _ => { });

        Assert.Equal(main, _sandbox.GitIn(creation.Path, "rev-parse", "HEAD").Trim());
    }

    [Fact]
    public void Create_WhenBaseUnknownOnRemote_ThenFailsOnFetch()
    {
        var failure = Assert.Throws<WorktreeException>(() => Create(new WorktreeRequestModel(_sandbox.Work, "feat/vue", WorktreeBranchMode.New, "origin/inconnue")));

        Assert.Equal(("Échec du fetch de origin/inconnue.", WorktreeSteps.Fetch), (failure.Message, failure.Step));
    }

    [Fact]
    public void InstallCommandFor_WhenPackageJsonPresent_ThenPnpmInstall()
    {
        _sandbox.Write("package.json", "{}");

        var command = WorktreeCreator.InstallCommandFor(_sandbox.Work, true);

        Assert.Equal("pnpm install", command);
    }

    private WorktreeCreationModel Create(WorktreeRequestModel request) => WorktreeCreator.Create(_sandbox.Runner, request, _settings, _sandbox.Root, _ => { });

    private WorktreePlanModel Plan(WorktreeRequestModel request) => WorktreeCreator.Plan(_sandbox.Runner, request, _settings, _sandbox.Root);

    public void Dispose() => _sandbox.Dispose();
}
