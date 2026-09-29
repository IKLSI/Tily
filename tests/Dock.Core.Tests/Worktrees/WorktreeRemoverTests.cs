using Dock.Core.Tests.Git;
using Dock.Core.Worktrees;
using Xunit;

namespace Dock.Core.Tests.Worktrees;

public sealed class WorktreeRemoverTests : IDisposable
{
    private const string Config = "{ \"ConnectionStrings\": { \"DefaultConnection\": \"Host=localhost;Database=app;Username=app\" } }";

    private readonly GitSandbox _sandbox = new();
    private readonly RecordingReplicator _replicator = new();
    private readonly WorktreeRemover _remover;
    private readonly string _worktree;

    public WorktreeRemoverTests()
    {
        _sandbox.Commit("Base", (@"Api\appsettings.Development.json", Config));
        _worktree = Path.Combine(_sandbox.Root, "worktrees", "dépôt-vue");
        _sandbox.Git("worktree", "add", "-q", "-b", "feat/vue", _worktree);
        _remover = new WorktreeRemover(_sandbox.Runner, new WorktreeDatabase([_replicator]));
    }

    [Fact]
    public void Remove_WhenWorktree_ThenDeletesFolderAndBranch()
    {
        _remover.Remove(_worktree, false, false, true, _ => { });

        Assert.False(Directory.Exists(_worktree));
        Assert.Null(_sandbox.Repository.RefValue("refs/heads/feat/vue"));
    }

    [Fact]
    public void Remove_WhenKeepBranch_ThenKeepsBranch()
    {
        _remover.Remove(_worktree, true, false, true, _ => { });

        Assert.NotNull(_sandbox.Repository.RefValue("refs/heads/feat/vue"));
    }

    [Fact]
    public void Remove_WhenMainWorktree_ThenRefusesInFrench()
    {
        var failure = Assert.Throws<WorktreeException>(() => _remover.Remove(_sandbox.Work, false, false, true, _ => { }));

        Assert.Equal($"Refus : impossible de supprimer le dépôt principal ({_sandbox.Work}).", failure.Message);
    }

    [Fact]
    public void Remove_WhenNotConfirmed_ThenRefusesWithoutDeleting()
    {
        Assert.Throws<WorktreeException>(() => _remover.Remove(_worktree, false, false, false, _ => { }));

        Assert.True(Directory.Exists(_worktree));
    }

    [Fact]
    public void Remove_WhenDatabaseSameAsMain_ThenNeverDropsIt()
    {
        _remover.Remove(_worktree, false, true, true, _ => { });

        Assert.Empty(_replicator.Dropped);
    }

    [Fact]
    public void Remove_WhenDatabaseReplicated_ThenDropsWorktreeDatabase()
    {
        File.WriteAllText(Path.Combine(_worktree, @"Api\appsettings.Development.json"), Config.Replace("Database=app;", "Database=app_vue;"));

        _remover.Remove(_worktree, false, true, true, _ => { });

        Assert.Equal(["app_vue"], _replicator.Dropped);
    }

    [Fact]
    public void Remove_WhenFileLocked_ThenFailsInFrenchAndNamesProcess()
    {
        var locked = Path.Combine(_worktree, "verrou.txt");
        File.WriteAllText(locked, "x");
        WorktreeException failure;
        using (new FileStream(locked, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            failure = Assert.Throws<WorktreeException>(() => _remover.Remove(_worktree, false, false, true, _ => { }));
        }

        Assert.StartsWith("Le dossier du worktree est verrouillé", failure.Message);
        Assert.NotEmpty(failure.LockedBy);
    }

    [Fact]
    public void Remove_WhenFileLocked_ThenLeavesWorktreeIntactForRetry()
    {
        var locked = Path.Combine(_worktree, "verrou.txt");
        File.WriteAllText(locked, "x");
        using (new FileStream(locked, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.Throws<WorktreeException>(() => _remover.Remove(_worktree, false, false, true, _ => { }));
        }

        _remover.Remove(_worktree, false, false, true, _ => { });

        Assert.False(Directory.Exists(_worktree));
    }

    public void Dispose() => _sandbox.Dispose();

    private sealed class RecordingReplicator : IDatabaseReplicator
    {
        public List<string> Dropped { get; } = [];

        public DatabaseProvider Provider => DatabaseProvider.PostgreSql;

        public DatabaseOutcomeModel Replicate(DatabaseInfoModel info, string targetDb, DatabaseContextModel context) => new(true, "répliquée");

        public DatabaseOutcomeModel Drop(DatabaseInfoModel info, string targetDb, DatabaseContextModel context)
        {
            Dropped.Add(targetDb);
            return new DatabaseOutcomeModel(true, "supprimée");
        }
    }
}
