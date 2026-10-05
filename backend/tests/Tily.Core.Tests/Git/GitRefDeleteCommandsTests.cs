using Tily.Core.Git;
using Xunit;

namespace Tily.Core.Tests.Git;

public sealed class GitRefDeleteCommandsTests : IDisposable
{
    private readonly GitSandbox _sandbox = new();
    private readonly string _remote;

    public GitRefDeleteCommandsTests()
    {
        _remote = _sandbox.CreateRemote();
        _sandbox.Commit("Base", ("a.txt", "a\n"));
        _sandbox.Git("push", "-q", "-u", "origin", "main");
    }

    [Fact]
    public void Delete_WhenMixedSelection_ThenDeletesEachRefAndUndoRestoresLocalOnes()
    {
        _sandbox.Git("switch", "-q", "-c", "isolée");
        var tip = _sandbox.Commit("Seule ici", ("b.txt", "b\n"));
        _sandbox.Git("switch", "-q", "main");
        _sandbox.Git("branch", "fusionnée");
        _sandbox.Git("tag", "v1.0");
        _sandbox.Git("push", "-q", "origin", "main:jetable");
        _sandbox.Git("fetch", "-q");
        _sandbox.Write("a.txt", "premier\n");
        GitStashTagCommands.Stash(_sandbox.Repository, "Premier", []);
        _sandbox.Write("a.txt", "second\n");
        GitStashTagCommands.Stash(_sandbox.Repository, "Second", []);
        var stashes = GitRefsReader.ReadStashes(_sandbox.Repository);
        var deletion = new GitRefDeletionModel(["isolée", "fusionnée"], ["origin/jetable"], ["v1.0"], [.. stashes.Select(stash => stash.Sha)]);

        var outcome = GitRefDeleteCommands.Delete(_sandbox.Repository, deletion, true);
        var remoteGone = !_sandbox.Runner.Run(_remote, ["rev-parse", "--verify", "-q", "refs/heads/jetable"]).Succeeded;
        var emptied = GitRefsReader.ReadStashes(_sandbox.Repository).Count == 0 && _sandbox.Repository.RefValue("refs/heads/isolée") is null && _sandbox.Repository.RefValue("refs/tags/v1.0") is null;
        GitUndo.Apply(_sandbox.Repository, outcome.Undo);

        Assert.Equal("Références supprimées : 2 branches, 1 branche distante, 1 tag et 2 stash.", outcome.Message);
        Assert.True(remoteGone && emptied);
        Assert.Equal("Suppression de 2 branches, 1 tag et 2 stash", outcome.Undo?.Label);
        Assert.Equal(tip, _sandbox.Repository.RefValue("refs/heads/isolée"));
        Assert.NotNull(_sandbox.Repository.RefValue("refs/tags/v1.0"));
        Assert.Equal(stashes.Select(stash => stash.Sha), GitRefsReader.ReadStashes(_sandbox.Repository).Select(stash => stash.Sha));
    }

    [Fact]
    public void Delete_WhenCurrentBranchSelected_ThenRefusesBeforeDeletingAnything()
    {
        _sandbox.Git("branch", "autre");

        var exception = Assert.Throws<GitCommandException>(() => GitRefDeleteCommands.Delete(_sandbox.Repository, new GitRefDeletionModel(["autre", "main"], [], [], []), true));

        Assert.Equal("Impossible de supprimer la branche courante « main » : faites d’abord le checkout d’une autre branche.", exception.Message);
        Assert.NotNull(_sandbox.Repository.RefValue("refs/heads/autre"));
    }

    public void Dispose() => _sandbox.Dispose();
}
