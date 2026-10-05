using Tily.Core.Tests.Git;
using Tily.Core.Worktrees;
using Xunit;

namespace Tily.Core.Tests.Worktrees;

public sealed class WorktreeRemoverTests : IDisposable
{
    private const string Config = "{ \"ConnectionStrings\": { \"DefaultConnection\": \"Host=localhost;Database=app;Username=app\" } }";

    private readonly GitSandbox _sandbox = new();
    private readonly RecordingReplicator _replicator = new();
    private readonly WorktreeRemover _remover;
    private readonly string _worktree;

    public WorktreeRemoverTests()
    {
        _sandbox.Commit("Base", ("Api/appsettings.Development.json", Config));
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
    public void Remove_WhenWorktree_ThenMovesFolderToTrashForPurge()
    {
        File.WriteAllText(Path.Combine(_worktree, "fichier.txt"), "x");

        var removal = _remover.Remove(_worktree, false, false, true, _ => { });

        Assert.Contains(Directory.EnumerateFiles(removal.Trash, "fichier.txt", SearchOption.AllDirectories), file => file.Contains("dépôt-vue"));
    }

    [Fact]
    public void Remove_WhenWorktree_ThenGitNoLongerListsIt()
    {
        _remover.Remove(_worktree, false, false, true, _ => { });

        Assert.DoesNotContain(WorktreeLister.List(_sandbox.Repository), worktree => WorktreeTarget.SamePath(worktree.Path, _worktree));
    }

    [Fact]
    public void Remove_WhenWorktreeLocked_ThenRemovesIt()
    {
        _sandbox.Git("worktree", "lock", _worktree);

        _remover.Remove(_worktree, false, false, true, _ => { });

        Assert.DoesNotContain(WorktreeLister.List(_sandbox.Repository), worktree => WorktreeTarget.SamePath(worktree.Path, _worktree));
    }

    [Fact]
    public void Purge_WhenWorktreeRemoved_ThenDeletesTrash()
    {
        var removal = _remover.Remove(_worktree, false, false, true, _ => { });

        WorktreeTrash.Purge(removal.Trash, _ => { });

        Assert.False(Directory.Exists(removal.Trash));
    }

    [Fact]
    public void Purge_WhenWorktreeRemoved_ThenNamesWorktree()
    {
        var removal = _remover.Remove(_worktree, false, false, true, _ => { });

        var purge = WorktreeTrash.Purge(removal.Trash, _ => { });

        Assert.Equal(["dépôt-vue"], purge.Names);
    }

    [Fact]
    public void Purge_WhenSymbolicLinkPointsOutside_ThenKeepsTargetFiles()
    {
        var outside = Path.Combine(_sandbox.Root, "dehors");
        Directory.CreateDirectory(outside);
        File.WriteAllText(Path.Combine(outside, "précieux.txt"), "x");
        Directory.CreateSymbolicLink(Path.Combine(_worktree, "node_modules"), outside);
        var removal = _remover.Remove(_worktree, false, false, true, _ => { });

        WorktreeTrash.Purge(removal.Trash, _ => { });

        Assert.True(File.Exists(Path.Combine(outside, "précieux.txt")));
    }

    [Theory]
    [InlineData("R&D")]
    [InlineData("100%")]
    [InlineData("-rf")]
    public void Purge_WhenNameHasShellCharacters_ThenDeletesTrashedFolder(string name)
    {
        var root = Path.Combine(_sandbox.Root, "corbeille");
        var trashed = Path.Combine(root, "abcd1234", name);
        Directory.CreateDirectory(trashed);
        File.WriteAllText(Path.Combine(trashed, "fichier.txt"), "x");

        WorktreeTrash.Purge(root, _ => { });

        Assert.False(Directory.Exists(root));
    }

    [Fact]
    public void Purge_WhenFolderNotWritable_ThenReportsRemainingFolder()
    {
        var removal = _remover.Remove(_worktree, false, false, true, _ => { });
        var kept = Directory.EnumerateDirectories(removal.Trash).Single();
        var locked = Path.Combine(kept, "dépôt-vue", "verrouillé");
        Directory.CreateDirectory(locked);
        File.WriteAllText(Path.Combine(locked, "fichier.txt"), "x");
        File.SetUnixFileMode(locked, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        WorktreePurgeModel purge;
        try
        {
            purge = WorktreeTrash.Purge(removal.Trash, _ => { });
        }
        finally
        {
            File.SetUnixFileMode(locked, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        Assert.Equal([Path.Combine(kept, "dépôt-vue")], purge.Remaining);
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
        File.WriteAllText(Path.Combine(_worktree, "Api/appsettings.Development.json"), Config.Replace("Database=app;", "Database=app_vue;"));

        _remover.Remove(_worktree, false, true, true, _ => { });

        Assert.Equal(["app_vue"], _replicator.Dropped);
    }

    [Fact]
    public void Remove_WhenFileOpen_ThenStillRemovesWorktree()
    {
        var opened = Path.Combine(_worktree, "ouvert.txt");
        File.WriteAllText(opened, "x");
        using (new FileStream(opened, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            _remover.Remove(_worktree, false, false, true, _ => { });
        }

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
