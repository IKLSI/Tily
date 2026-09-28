using Dock.Core.Git;
using Xunit;

namespace Dock.Core.Tests.Git;

public sealed class GitHistoryCommandsTests : IDisposable
{
    private readonly GitSandbox _sandbox = new();

    private void CreateConflict()
    {
        _sandbox.Commit("Base", ("conflit.txt", "base\n"));
        _sandbox.Git("switch", "-q", "-c", "feature");
        _sandbox.Commit("Feature", ("conflit.txt", "feature\n"));
        _sandbox.Git("switch", "-q", "main");
        _sandbox.Commit("Main", ("conflit.txt", "main\n"));
    }

    [Fact]
    public void Merge_WhenConflict_ThenReportsPendingOperationAndConflicts()
    {
        CreateConflict();

        var outcome = GitHistoryCommands.Merge(_sandbox.Repository, "feature");

        Assert.True(outcome.Warning);
        Assert.True(outcome.Undo?.Pending);
        Assert.Equal(GitOperationKind.Merge, _sandbox.Repository.Operation());
        Assert.Equal([new GitConflictModel("conflit.txt", GitConflictKind.BothModified)], _sandbox.Repository.Status().Conflicts);
    }

    [Fact]
    public void Abort_WhenMergeInProgress_ThenRestoresBranch()
    {
        CreateConflict();
        var head = _sandbox.Head();
        GitHistoryCommands.Merge(_sandbox.Repository, "feature");

        var outcome = GitChangeCommands.Abort(_sandbox.Repository, true);

        Assert.True(outcome.ClearUndo);
        Assert.Null(_sandbox.Repository.Operation());
        Assert.Equal(head, _sandbox.Head());
    }

    [Fact]
    public void Resolve_WhenMarkersRemain_ThenRefusesWithCode()
    {
        CreateConflict();
        GitHistoryCommands.Merge(_sandbox.Repository, "feature");

        var exception = Assert.Throws<GitCommandException>(() => GitChangeCommands.Resolve(_sandbox.Repository, ["conflit.txt"], false));

        Assert.Equal(GitFailureCode.ConflictMarkers, exception.Code);
    }

    [Fact]
    public void ContinueThenUndo_WhenConflictResolved_ThenMergeCommitThenBack()
    {
        CreateConflict();
        var head = _sandbox.Head();
        var pending = GitHistoryCommands.Merge(_sandbox.Repository, "feature").Undo;
        _sandbox.Write("conflit.txt", "résolu\n");
        GitChangeCommands.Resolve(_sandbox.Repository, ["conflit.txt"], false);

        var finished = GitChangeCommands.Continue(_sandbox.Repository, pending);
        var merged = _sandbox.Repository.Resolve("HEAD^2");
        GitUndo.Apply(_sandbox.Repository, finished.Undo);

        Assert.Equal("Merge terminé.", finished.Message);
        Assert.NotNull(merged);
        Assert.Equal(head, _sandbox.Head());
        Assert.Equal("main\n", _sandbox.Read("conflit.txt"));
    }

    [Fact]
    public void UndoHardReset_WhenLocalChangesExisted_ThenRestoresCommitAndChanges()
    {
        var first = _sandbox.Commit("Un", ("a.txt", "1\n"));
        var second = _sandbox.Commit("Deux", ("a.txt", "2\n"));
        _sandbox.Write("a.txt", "local\n");

        var outcome = GitHistoryCommands.Reset(_sandbox.Repository, first, "hard", true);
        var afterReset = (_sandbox.Head(), _sandbox.Read("a.txt"));
        GitUndo.Apply(_sandbox.Repository, outcome.Undo);

        Assert.Equal((first, "1\n"), afterReset);
        Assert.Equal(second, _sandbox.Head());
        Assert.Equal("local\n", _sandbox.Read("a.txt"));
    }

    [Fact]
    public void UndoMixedReset_WhenFilesWereStaged_ThenRestoresIndex()
    {
        var first = _sandbox.Commit("Un", ("a.txt", "1\n"));
        _sandbox.Commit("Deux", ("a.txt", "2\n"));
        _sandbox.Write("b.txt", "b\n");
        _sandbox.Git("add", "b.txt");

        var outcome = GitHistoryCommands.Reset(_sandbox.Repository, first, "mixed", false);
        GitUndo.Apply(_sandbox.Repository, outcome.Undo);

        Assert.Equal(["b.txt"], _sandbox.Repository.Status().Staged.Select(change => change.Path));
    }

    [Fact]
    public void Reset_WhenHardNotConfirmed_ThenRefuses()
    {
        var first = _sandbox.Commit("Un", ("a.txt", "1\n"));
        var second = _sandbox.Commit("Deux", ("a.txt", "2\n"));

        Assert.Throws<GitCommandException>(() => GitHistoryCommands.Reset(_sandbox.Repository, first, "hard", false));

        Assert.Equal(second, _sandbox.Head());
    }

    [Fact]
    public void CherryPick_WhenCommitFromOtherBranch_ThenAppliesItAndUndoRemovesIt()
    {
        _sandbox.Commit("Base", ("a.txt", "a\n"));
        _sandbox.Git("switch", "-q", "-c", "feature");
        var picked = _sandbox.Commit("À reprendre", ("f.txt", "f\n"));
        _sandbox.Git("switch", "-q", "main");
        var head = _sandbox.Head();

        var outcome = GitHistoryCommands.CherryPick(_sandbox.Repository, picked);
        var applied = _sandbox.Exists("f.txt");
        GitUndo.Apply(_sandbox.Repository, outcome.Undo);

        Assert.True(applied);
        Assert.Equal(head, _sandbox.Head());
        Assert.False(_sandbox.Exists("f.txt"));
    }

    [Fact]
    public void Rebase_WhenBranchBehind_ThenReplaysCommitsAndUndoRestoresTip()
    {
        _sandbox.Commit("Base", ("a.txt", "a\n"));
        _sandbox.Git("switch", "-q", "-c", "feature");
        var tip = _sandbox.Commit("Feature", ("f.txt", "f\n"));
        _sandbox.Git("switch", "-q", "main");
        var main = _sandbox.Commit("Main", ("m.txt", "m\n"));
        _sandbox.Git("switch", "-q", "feature");

        var outcome = GitHistoryCommands.Rebase(_sandbox.Repository, "main");
        var rebasedOnMain = _sandbox.Repository.IsAncestor(main, _sandbox.Head());
        GitUndo.Apply(_sandbox.Repository, outcome.Undo);

        Assert.True(rebasedOnMain);
        Assert.Equal(tip, _sandbox.Head());
        Assert.Equal("feature", _sandbox.Branch());
    }

    [Fact]
    public void Rebase_WhenConflict_ThenStopsAndContinueFinishes()
    {
        CreateConflict();
        _sandbox.Git("switch", "-q", "feature");
        var pending = GitHistoryCommands.Rebase(_sandbox.Repository, "main").Undo;
        var operation = _sandbox.Repository.Operation();
        _sandbox.Write("conflit.txt", "résolu\n");
        GitChangeCommands.Resolve(_sandbox.Repository, ["conflit.txt"], false);

        var finished = GitChangeCommands.Continue(_sandbox.Repository, pending);

        Assert.Equal(GitOperationKind.Rebase, operation);
        Assert.Equal("Rebase terminé.", finished.Message);
        Assert.False(finished.Undo?.Pending);
        Assert.Equal("feature", _sandbox.Branch());
    }

    [Fact]
    public void SwitchToCommit_WhenDetached_ThenUndoReturnsToBranch()
    {
        var first = _sandbox.Commit("Un", ("a.txt", "1\n"));
        _sandbox.Commit("Deux", ("a.txt", "2\n"));

        var outcome = GitBranchCommands.Switch(_sandbox.Repository, first, GitBranchCommands.CommitTarget);
        var detached = _sandbox.Repository.Status().Head.Detached;
        GitUndo.Apply(_sandbox.Repository, outcome.Undo);

        Assert.True(detached);
        Assert.Equal("main", _sandbox.Branch());
    }

    [Fact]
    public void Undo_WhenRepositoryChangedSince_ThenRefuses()
    {
        _sandbox.Commit("Base", ("a.txt", "a\n"));
        _sandbox.Write("a.txt", "b\n");
        GitChangeCommands.Stage(_sandbox.Repository, []);
        var outcome = GitChangeCommands.Commit(_sandbox.Repository, "Deux", false);
        _sandbox.Commit("Trois au terminal", ("c.txt", "c\n"));

        var exception = Assert.Throws<GitCommandException>(() => GitUndo.Apply(_sandbox.Repository, outcome.Undo));

        Assert.Equal("Annulation impossible : le dépôt a changé depuis cette opération.", exception.Message);
    }

    public void Dispose() => _sandbox.Dispose();
}
