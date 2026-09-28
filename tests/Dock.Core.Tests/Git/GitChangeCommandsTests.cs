using Dock.Core.Git;
using Xunit;

namespace Dock.Core.Tests.Git;

public sealed class GitChangeCommandsTests : IDisposable
{
    private readonly GitSandbox _sandbox = new();

    [Fact]
    public void StageThenUnstage_WhenFileModified_ThenMovesBetweenIndexAndWorkTree()
    {
        _sandbox.Commit("Base", ("a.txt", "a\n"));
        _sandbox.Write("a.txt", "b\n");

        GitChangeCommands.Stage(_sandbox.Repository, ["a.txt"]);
        var staged = _sandbox.Repository.Status();
        GitChangeCommands.Unstage(_sandbox.Repository, ["a.txt"]);
        var unstaged = _sandbox.Repository.Status();

        Assert.Equal(["a.txt"], staged.Staged.Select(change => change.Path));
        Assert.Empty(unstaged.Staged);
        Assert.Equal(["a.txt"], unstaged.Unstaged.Select(change => change.Path));
    }

    [Fact]
    public void Unstage_WhenNoCommitYet_ThenRemovesFromIndex()
    {
        _sandbox.Write("a.txt", "a\n");
        GitChangeCommands.Stage(_sandbox.Repository, []);

        GitChangeCommands.Unstage(_sandbox.Repository, []);

        Assert.Equal([GitChangeKind.Untracked], _sandbox.Repository.Status().Unstaged.Select(change => change.Kind));
    }

    [Fact]
    public void Commit_WhenNothingStaged_ThenRefusesInFrench()
    {
        _sandbox.Commit("Base", ("a.txt", "a\n"));

        var exception = Assert.Throws<GitCommandException>(() => GitChangeCommands.Commit(_sandbox.Repository, "Message", false));

        Assert.Equal("Aucune modification staged : il faut au moins un fichier staged pour committer.", exception.Message);
    }

    [Fact]
    public void UndoCommit_WhenNotPushed_ThenRestoresHeadAndKeepsChangesStaged()
    {
        var before = _sandbox.Commit("Base", ("a.txt", "a\n"));
        _sandbox.Write("a.txt", "b\n");
        GitChangeCommands.Stage(_sandbox.Repository, ["a.txt"]);
        var outcome = GitChangeCommands.Commit(_sandbox.Repository, "Changer a\n\nDétail", false);

        var undone = GitUndo.Apply(_sandbox.Repository, outcome.Undo);

        Assert.Equal("Annulé : Commit « Changer a ».", undone.Message);
        Assert.Equal(before, _sandbox.Head());
        Assert.Equal(["a.txt"], _sandbox.Repository.Status().Staged.Select(change => change.Path));
    }

    [Fact]
    public void UndoCommit_WhenFirstCommit_ThenBranchBecomesUnborn()
    {
        _sandbox.Write("a.txt", "a\n");
        GitChangeCommands.Stage(_sandbox.Repository, []);
        var outcome = GitChangeCommands.Commit(_sandbox.Repository, "Premier", false);

        GitUndo.Apply(_sandbox.Repository, outcome.Undo);

        Assert.True(_sandbox.Repository.Status().Head.Unborn);
        Assert.Equal(["a.txt"], _sandbox.Repository.Status().Staged.Select(change => change.Path));
    }

    [Fact]
    public void UndoCommit_WhenPushedSince_ThenRefuses()
    {
        _sandbox.CreateRemote();
        _sandbox.Commit("Base", ("a.txt", "a\n"));
        _sandbox.Git("push", "-q", "-u", "origin", "main");
        _sandbox.Write("a.txt", "b\n");
        GitChangeCommands.Stage(_sandbox.Repository, []);
        var outcome = GitChangeCommands.Commit(_sandbox.Repository, "Deux", false);
        _sandbox.Git("push", "-q");

        var info = GitUndo.Describe(_sandbox.Repository, outcome.Undo, _sandbox.Repository.Status().Head, null);

        Assert.Equal(new GitUndoInfoModel("Commit « Deux »", false, "un push a déjà publié les commits"), info);
    }

    [Fact]
    public void UndoAmend_WhenAmended_ThenRestoresPreviousCommit()
    {
        var original = _sandbox.Commit("Original", ("a.txt", "a\n"));
        _sandbox.Write("b.txt", "b\n");
        GitChangeCommands.Stage(_sandbox.Repository, []);
        var outcome = GitChangeCommands.Commit(_sandbox.Repository, "Modifié", true);

        GitUndo.Apply(_sandbox.Repository, outcome.Undo);

        Assert.Equal(original, _sandbox.Head());
        Assert.Equal(["b.txt"], _sandbox.Repository.Status().Staged.Select(change => change.Path));
    }

    [Fact]
    public void Discard_WhenNotConfirmed_ThenRefuses()
    {
        _sandbox.Commit("Base", ("a.txt", "a\n"));
        _sandbox.Write("a.txt", "b\n");

        Assert.Throws<GitCommandException>(() => GitChangeCommands.Discard(_sandbox.Repository, ["a.txt"], false));

        Assert.Equal("b\n", _sandbox.Read("a.txt"));
    }

    [Fact]
    public void UndoDiscard_WhenTrackedAndUntracked_ThenRestoresBothFiles()
    {
        _sandbox.Commit("Base", ("a.txt", "a\n"));
        _sandbox.Write("a.txt", "b\r\n");
        _sandbox.Write("dossier/neuf.txt", "neuf\n");
        var outcome = GitChangeCommands.Discard(_sandbox.Repository, [], true);
        var discarded = (_sandbox.Read("a.txt"), _sandbox.Exists("dossier/neuf.txt"));

        GitUndo.Apply(_sandbox.Repository, outcome.Undo);

        Assert.Equal(("a\n", false), discarded);
        Assert.Equal("b\r\n", _sandbox.Read("a.txt"));
        Assert.Equal("neuf\n", _sandbox.Read("dossier/neuf.txt"));
    }

    [Fact]
    public void UndoDiscard_WhenFileEditedSince_ThenRefusesWithoutWriting()
    {
        _sandbox.Commit("Base", ("a.txt", "a\n"));
        _sandbox.Write("a.txt", "b\n");
        var outcome = GitChangeCommands.Discard(_sandbox.Repository, ["a.txt"], true);
        _sandbox.Write("a.txt", "c\n");

        var exception = Assert.Throws<GitCommandException>(() => GitUndo.Apply(_sandbox.Repository, outcome.Undo));

        Assert.Equal("Annulation impossible : « a.txt » a été modifié depuis l’abandon.", exception.Message);
        Assert.Equal("c\n", _sandbox.Read("a.txt"));
    }

    public void Dispose() => _sandbox.Dispose();
}
