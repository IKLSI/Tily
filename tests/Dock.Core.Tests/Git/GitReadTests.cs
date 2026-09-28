using Dock.Core.Git;
using Xunit;

namespace Dock.Core.Tests.Git;

public sealed class GitReadTests : IDisposable
{
    private readonly GitSandbox _sandbox = new();

    [Fact]
    public void Locate_WhenFolderOutsideRepository_ThenNull()
    {
        var folder = Path.Combine(_sandbox.Root, "hors dépôt");
        Directory.CreateDirectory(folder);

        var location = GitRepository.Locate(_sandbox.Runner, folder);

        Assert.Null(location);
    }

    [Fact]
    public void Locate_WhenNestedFolder_ThenReturnsRoot()
    {
        _sandbox.Commit("Premier", ("src/web/app.ts", "a"));

        var location = GitRepository.Locate(_sandbox.Runner, Path.Combine(_sandbox.Work, "src", "web"));

        Assert.Equal(_sandbox.Work, location?.Root);
    }

    [Fact]
    public void Locate_WhenWorktree_ThenSeparatesGitAndCommonDirectories()
    {
        _sandbox.Commit("Premier", ("a.txt", "a"));
        var worktree = Path.Combine(_sandbox.Root, "arbre secondaire");
        _sandbox.Git("worktree", "add", "-q", "-b", "secondaire", worktree);

        var location = GitRepository.Locate(_sandbox.Runner, worktree);
        var state = GitStateReader.Read(new GitRepository(_sandbox.Runner, location!), null, _ => false);

        Assert.EndsWith("arbre secondaire", location!.Root);
        Assert.Equal(Path.Combine(_sandbox.Work, ".git"), location.CommonDirectory);
        Assert.StartsWith(Path.Combine(_sandbox.Work, ".git", "worktrees"), location.GitDirectory);
        Assert.Equal("secondaire", state.Head.Branch);
    }

    [Fact]
    public void ReadState_WhenChangesAndRemote_ThenReportsFilesBranchesAndTracking()
    {
        _sandbox.CreateRemote();
        _sandbox.Commit("Premier", ("a.txt", "a\n"));
        _sandbox.Git("push", "-q", "-u", "origin", "main");
        _sandbox.Commit("Deuxième", ("b.txt", "b\n"));
        _sandbox.Git("branch", "feature");
        _sandbox.Write("a.txt", "modifié\n");
        _sandbox.Write("nouveau.txt", "n\n");
        _sandbox.Write("indexé.txt", "i\n");
        _sandbox.Git("add", "indexé.txt");

        var state = GitStateReader.Read(_sandbox.Repository, null, _ => false);

        Assert.Equal(("main", "origin/main", 1, 0), (state.Head.Branch, state.Head.Upstream, state.Head.Ahead, state.Head.Behind));
        Assert.Equal(["indexé.txt"], state.Staged.Select(change => change.Path));
        Assert.Equal([("a.txt", GitChangeKind.Modified), ("nouveau.txt", GitChangeKind.Untracked)], state.Unstaged.Select(change => (change.Path, change.Kind)));
        Assert.Equal([("feature", false, true), ("main", true, true)], state.Branches.Select(branch => (branch.Name, branch.Current, branch.Merged)));
        Assert.Equal(1, state.Branches.Single(branch => branch.Name == "main").Ahead);
        Assert.Equal(["origin/main"], state.RemoteBranches.Select(branch => branch.Name));
        Assert.Equal("Deuxième", state.LastMessage);
    }

    [Fact]
    public void ReadHistory_WhenBranchMerged_ThenLabelsAndLanes()
    {
        var root = _sandbox.Commit("Base", ("a.txt", "a\n"));
        _sandbox.Git("switch", "-q", "-c", "feature");
        _sandbox.Commit("Feature", ("f.txt", "f\n"));
        _sandbox.Git("switch", "-q", "main");
        _sandbox.Commit("Main", ("m.txt", "m\n"));
        _sandbox.Git("merge", "-q", "--no-edit", "feature");
        _sandbox.Git("tag", "v1", root);
        var state = GitStateReader.Read(_sandbox.Repository, null, _ => false);

        var history = GitHistoryReader.Read(_sandbox.Repository, GitHistoryScope.All, 10, state);

        Assert.Equal(4, history.Commits.Count);
        Assert.False(history.HasMore);
        Assert.Equal(2, history.Commits[0].Parents.Count);
        Assert.Equal([("main", GitRefKind.Branch, true)], history.Commits[0].Refs.Select(label => (label.Name, label.Kind, label.Current)));
        Assert.Contains(history.Commits, commit => commit.Subject == "Feature" && commit.Graph.Lane == 1 && commit.Refs.Any(label => label.Name == "feature"));
        Assert.Equal([("v1", GitRefKind.Tag)], history.Commits[^1].Refs.Select(label => (label.Name, label.Kind)));
    }

    [Fact]
    public void ReadHistory_WhenMoreCommitsThanCount_ThenHasMore()
    {
        _sandbox.Commit("Un", ("a.txt", "1"));
        _sandbox.Commit("Deux", ("a.txt", "2"));
        _sandbox.Commit("Trois", ("a.txt", "3"));
        var state = GitStateReader.Read(_sandbox.Repository, null, _ => false);

        var history = GitHistoryReader.Read(_sandbox.Repository, GitHistoryScope.Current, 2, state);

        Assert.Equal(["Trois", "Deux"], history.Commits.Select(commit => commit.Subject));
        Assert.True(history.HasMore);
    }

    [Fact]
    public void ReadHistory_WhenOtherBranchIsNewer_ThenWorkingTreeLinksToHeadWithDashedLane()
    {
        _sandbox.Commit("Base", ("a.txt", "a\n"));
        _sandbox.Git("switch", "-q", "-c", "feature");
        _sandbox.Commit("Feature", ("f.txt", "f\n"));
        _sandbox.Git("switch", "-q", "main");
        var state = GitStateReader.Read(_sandbox.Repository, null, _ => false);

        var history = GitHistoryReader.Read(_sandbox.Repository, GitHistoryScope.All, 10, state);

        Assert.Equal((0, GitSegmentKind.Out, true), (history.WorkingTree.Lane, history.WorkingTree.Segments.Single().Kind, history.WorkingTree.Segments.Single().Dashed));
        Assert.Contains(new GitGraphSegmentModel(0, 0, history.WorkingTree.Color, GitSegmentKind.Through, true), history.Commits[0].Graph.Segments);
        Assert.Equal((0, true), (history.Commits[1].Graph.Lane, history.Commits[1].Graph.Segments.Any(segment => segment.Kind == GitSegmentKind.In && segment.Dashed)));
    }

    [Fact]
    public void ReadHistory_WhenStash_ThenInsertsItAboveItsBaseCommit()
    {
        _sandbox.Commit("Base", ("a.txt", "a\n"));
        var head = _sandbox.Commit("Tête", ("a.txt", "b\n"));
        _sandbox.Write("a.txt", "remisé\n");
        _sandbox.Git("stash", "push", "-q", "-m", "essai");
        var state = GitStateReader.Read(_sandbox.Repository, null, _ => false);

        var history = GitHistoryReader.Read(_sandbox.Repository, GitHistoryScope.All, 10, state);

        var stash = Assert.Single(state.Stashes);
        Assert.Equal(
            [(stash.Sha, true, head), (head, false, history.Commits[2].Sha)],
            history.Commits.Take(2).Select(commit => (commit.Sha, commit.Stash, commit.Parents[0])));
        Assert.Equal(("On main: essai", head), (history.Commits[0].Subject, stash.Base));
    }

    [Fact]
    public void ReadHistory_WhenLocalAndRemoteBranchesShareCommit_ThenSingleLabelListsRemote()
    {
        _sandbox.CreateRemote();
        var pushed = _sandbox.Commit("Publié", ("a.txt", "a\n"));
        _sandbox.Git("push", "-q", "-u", "origin", "main");
        _sandbox.Git("branch", "outil");
        _sandbox.Git("push", "-q", "origin", "outil");
        _sandbox.Git("branch", "-q", "-D", "outil");
        _sandbox.Commit("Local", ("b.txt", "b\n"));
        _sandbox.Git("push", "-q", "origin", "main:partagée");
        _sandbox.Git("fetch", "-q", "origin");
        _sandbox.Git("branch", "-q", "partagée", "origin/partagée");
        var state = GitStateReader.Read(_sandbox.Repository, null, _ => false);

        var history = GitHistoryReader.Read(_sandbox.Repository, GitHistoryScope.All, 10, state);

        Assert.Equal(
            [("main", GitRefKind.Branch, ""), ("partagée", GitRefKind.Branch, "origin/partagée")],
            history.Commits[0].Refs.Select(label => (label.Name, label.Kind, string.Join(',', label.Remotes))));
        Assert.Equal(pushed, history.Commits[1].Sha);
        Assert.Equal(
            [("origin/main", GitRefKind.Remote), ("origin/outil", GitRefKind.Remote)],
            history.Commits[1].Refs.Select(label => (label.Name, label.Kind)).Order());
    }

    [Fact]
    public void ReadDiff_WhenStagedUnstagedAndUntracked_ThenReturnsEachVersion()
    {
        _sandbox.Commit("Base", ("a.txt", "un\ndeux\n"));
        _sandbox.Write("a.txt", "un\ntrois\n");
        _sandbox.Git("add", "a.txt");
        _sandbox.Write("a.txt", "un\nquatre\n");
        _sandbox.Write("neuf.txt", "x\ny\n");

        var staged = GitDiffReader.Read(_sandbox.Repository, new GitDiffRequestModel(GitDiffSource.Staged, "a.txt", null, null, false));
        var unstaged = GitDiffReader.Read(_sandbox.Repository, new GitDiffRequestModel(GitDiffSource.Unstaged, "a.txt", null, null, false));
        var untracked = GitDiffReader.Read(_sandbox.Repository, new GitDiffRequestModel(GitDiffSource.Unstaged, "neuf.txt", null, null, true));

        Assert.Equal(["deux", "trois"], staged.Hunks[0].Lines.Where(line => line.Kind != GitDiffLineKind.Context).Select(line => line.Text));
        Assert.Equal(["trois", "quatre"], unstaged.Hunks[0].Lines.Where(line => line.Kind != GitDiffLineKind.Context).Select(line => line.Text));
        Assert.Equal([(1, "x"), (2, "y")], untracked.Hunks[0].Lines.Select(line => (line.New ?? 0, line.Text)));
    }

    [Fact]
    public void ReadCommit_WhenRenameAndRootCommit_ThenListsFilesAndDiff()
    {
        var root = _sandbox.Commit("Base", ("ancien.txt", "contenu identique\n"), ("b.txt", "b\n"));
        _sandbox.Git("mv", "ancien.txt", "nouveau.txt");
        _sandbox.Write("b.txt", "b2\n");
        var second = _sandbox.Commit("Renommage");

        var details = GitDiffReader.ReadCommit(_sandbox.Repository, second);
        var rootDetails = GitDiffReader.ReadCommit(_sandbox.Repository, root);
        var rootDiff = GitDiffReader.Read(_sandbox.Repository, new GitDiffRequestModel(GitDiffSource.Commit, "b.txt", null, root, false));

        Assert.Equal("Renommage", details.Message);
        Assert.Equal([("b.txt", null, GitChangeKind.Modified), ("nouveau.txt", "ancien.txt", GitChangeKind.Renamed)], details.Files.OrderBy(file => file.Path).Select(file => (file.Path, file.OldPath, file.Kind)));
        Assert.Equal(2, rootDetails.Files.Count);
        Assert.Equal([GitDiffLineKind.Added], rootDiff.Hunks[0].Lines.Select(line => line.Kind));
    }

    public void Dispose() => _sandbox.Dispose();
}
