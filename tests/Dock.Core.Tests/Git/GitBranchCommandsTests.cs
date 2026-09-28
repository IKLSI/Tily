using Dock.Core.Git;
using Xunit;

namespace Dock.Core.Tests.Git;

public sealed class GitBranchCommandsTests : IDisposable
{
    private readonly GitSandbox _sandbox = new();

    public GitBranchCommandsTests() => _sandbox.Commit("Base", ("a.txt", "a\n"));

    [Fact]
    public void Create_WhenInvalidName_ThenRefusesInFrench()
    {
        var exception = Assert.Throws<GitCommandException>(() => GitBranchCommands.Create(_sandbox.Repository, "mauvais..nom", null, false));

        Assert.Equal("Nom de branche invalide : « mauvais..nom ».", exception.Message);
    }

    [Fact]
    public void UndoCreate_WhenCreatedAndCheckedOut_ThenSwitchesBackAndDeletes()
    {
        var outcome = GitBranchCommands.Create(_sandbox.Repository, "feature/vue", null, true);
        var created = _sandbox.Branch();

        GitUndo.Apply(_sandbox.Repository, outcome.Undo);

        Assert.Equal("feature/vue", created);
        Assert.Equal("main", _sandbox.Branch());
        Assert.Null(_sandbox.Repository.RefValue("refs/heads/feature/vue"));
    }

    [Fact]
    public void Delete_WhenNotMerged_ThenRequiresConfirmationAndUndoRestores()
    {
        _sandbox.Git("switch", "-q", "-c", "isolée");
        var tip = _sandbox.Commit("Seule ici", ("b.txt", "b\n"));
        _sandbox.Git("switch", "-q", "main");

        var refused = Assert.Throws<GitCommandException>(() => GitBranchCommands.Delete(_sandbox.Repository, "isolée", false, false));
        var outcome = GitBranchCommands.Delete(_sandbox.Repository, "isolée", true, true);
        GitUndo.Apply(_sandbox.Repository, outcome.Undo);

        Assert.Equal(GitFailureCode.NotMerged, refused.Code);
        Assert.Equal(tip, _sandbox.Repository.RefValue("refs/heads/isolée"));
    }

    [Fact]
    public void UndoRename_WhenRenamed_ThenRestoresName()
    {
        _sandbox.Git("branch", "ancien");
        var outcome = GitBranchCommands.Rename(_sandbox.Repository, "ancien", "nouveau");

        GitUndo.Apply(_sandbox.Repository, outcome.Undo);

        Assert.NotNull(_sandbox.Repository.RefValue("refs/heads/ancien"));
        Assert.Null(_sandbox.Repository.RefValue("refs/heads/nouveau"));
    }

    [Fact]
    public void SwitchRemote_WhenNoLocalBranch_ThenCreatesTrackingBranchAndUndoRemovesIt()
    {
        var remote = _sandbox.CreateRemote();
        _sandbox.Git("push", "-q", "-u", "origin", "main");
        var clone = _sandbox.Clone(remote, "clone");
        _sandbox.GitIn(clone, "switch", "-q", "-c", "distante");
        _sandbox.GitIn(clone, "commit", "-q", "--allow-empty", "-m", "Distante");
        _sandbox.GitIn(clone, "push", "-q", "origin", "distante");
        _sandbox.Git("fetch", "-q");

        var outcome = GitBranchCommands.Switch(_sandbox.Repository, "origin/distante", GitBranchCommands.RemoteTarget);
        var upstream = _sandbox.Repository.Status().Head.Upstream;
        GitUndo.Apply(_sandbox.Repository, outcome.Undo);

        Assert.Equal("origin/distante", upstream);
        Assert.Equal("main", _sandbox.Branch());
        Assert.Null(_sandbox.Repository.RefValue("refs/heads/distante"));
    }

    [Fact]
    public void UndoStash_WhenStashed_ThenPopsChangesBack()
    {
        _sandbox.Write("a.txt", "modifié\n");
        _sandbox.Write("neuf.txt", "n\n");
        var outcome = GitStashTagCommands.Stash(_sandbox.Repository, "Travail en cours");
        var stashed = GitRefsReader.ReadStashes(_sandbox.Repository);

        GitUndo.Apply(_sandbox.Repository, outcome.Undo);

        Assert.Equal("On main: Travail en cours", Assert.Single(stashed).Message);
        Assert.Empty(GitRefsReader.ReadStashes(_sandbox.Repository));
        Assert.Equal("modifié\n", _sandbox.Read("a.txt"));
        Assert.True(_sandbox.Exists("neuf.txt"));
    }

    [Fact]
    public void UndoDropStash_WhenDropped_ThenStoresItAgain()
    {
        _sandbox.Write("a.txt", "modifié\n");
        GitStashTagCommands.Stash(_sandbox.Repository, "À garder");
        var stash = Assert.Single(GitRefsReader.ReadStashes(_sandbox.Repository));

        var outcome = GitStashTagCommands.DropStash(_sandbox.Repository, 0, stash.Sha, true);
        GitUndo.Apply(_sandbox.Repository, outcome.Undo);

        Assert.Equal(stash.Sha, Assert.Single(GitRefsReader.ReadStashes(_sandbox.Repository)).Sha);
    }

    [Fact]
    public void ApplyStash_WhenListChanged_ThenRefuses()
    {
        _sandbox.Write("a.txt", "modifié\n");
        GitStashTagCommands.Stash(_sandbox.Repository, null);

        var exception = Assert.Throws<GitCommandException>(() => GitStashTagCommands.ApplyStash(_sandbox.Repository, 0, "0000000000000000000000000000000000000000", false));

        Assert.Equal("La liste des stashes a changé : actualisez puis réessayez.", exception.Message);
    }

    [Fact]
    public void UndoTags_WhenCreatedThenDeleted_ThenRestoresEachStep()
    {
        var head = _sandbox.Head();
        var created = GitStashTagCommands.CreateTag(_sandbox.Repository, "v1.0", null);
        var deleted = GitStashTagCommands.DeleteTag(_sandbox.Repository, "v1.0");

        GitUndo.Apply(_sandbox.Repository, deleted.Undo);
        var restored = _sandbox.Repository.RefValue("refs/tags/v1.0");
        GitUndo.Apply(_sandbox.Repository, created.Undo);

        Assert.Equal(head, restored);
        Assert.Null(_sandbox.Repository.RefValue("refs/tags/v1.0"));
    }

    public void Dispose() => _sandbox.Dispose();
}
