using Tily.Core.Git;
using Xunit;

namespace Tily.Core.Tests.Git;

public sealed class GitLineCommandsTests : IDisposable
{
    private static readonly string Twenty = string.Concat(Enumerable.Range(1, 20).Select(number => $"ligne {number}\n"));

    private readonly GitSandbox _sandbox = new();

    [Fact]
    public void StageLines_WhenWholeFirstHunk_ThenStagesOnlyThatHunk()
    {
        _sandbox.Commit("Base", ("a.txt", Twenty));
        _sandbox.Write("a.txt", Twenty.Replace("ligne 2\n", "ligne deux\n").Replace("ligne 19\n", "ligne dix-neuf\n"));

        Apply(GitLineAction.Stage, "a.txt", false, diff => Changes(diff, 0));

        Assert.Equal(Twenty.Replace("ligne 2\n", "ligne deux\n"), _sandbox.Git("show", ":a.txt"));
    }

    [Fact]
    public void StageLines_WhenOneAddedLineOfHunk_ThenIndexGetsOnlyThatLine()
    {
        _sandbox.Commit("Base", ("a.txt", "un\ndeux\n"));
        _sandbox.Write("a.txt", "un\nnouveau\nautre\ndeux\n");

        Apply(GitLineAction.Stage, "a.txt", false, diff => Lines(diff, "autre"));

        Assert.Equal("un\nautre\ndeux\n", _sandbox.Git("show", ":a.txt"));
    }

    [Fact]
    public void StageLines_WhenFileUsesCrlf_ThenKeepsLineEndings()
    {
        _sandbox.Commit("Base", ("a.txt", "un\r\ndeux\r\n"));
        _sandbox.Write("a.txt", "un\r\nA\r\nB\r\ndeux\r\n");

        Apply(GitLineAction.Stage, "a.txt", false, diff => Lines(diff, "B\r", "B"));

        Assert.Equal("un\r\nB\r\ndeux\r\n", _sandbox.Git("show", ":a.txt"));
    }

    [Fact]
    public void StageLines_WhenUntrackedFilePartially_ThenIndexGetsSelectedLinesOnly()
    {
        _sandbox.Commit("Base", ("a.txt", "a\n"));
        _sandbox.Write("dossier/nouveau fichier.txt", "x\r\ny\r\nz");

        Apply(GitLineAction.Stage, "dossier/nouveau fichier.txt", true, diff => Lines(diff, "x", "z"));

        Assert.Equal("x\r\nz", _sandbox.Git("show", ":dossier/nouveau fichier.txt"));
    }

    [Fact]
    public void StageLines_WhenPartOfDeletedFile_ThenIndexKeepsUnselectedLines()
    {
        _sandbox.Commit("Base", ("a.txt", "x\ny\nz\n"));
        File.Delete(Path.Combine(_sandbox.Work, "a.txt"));

        Apply(GitLineAction.Stage, "a.txt", false, diff => Lines(diff, "y"));

        Assert.Equal("x\nz\n", _sandbox.Git("show", ":a.txt"));
    }

    [Fact]
    public void UnstageLines_WhenOneOfTwoStagedLines_ThenOtherStaysStaged()
    {
        _sandbox.Commit("Base", ("a.txt", "un\ndeux\n"));
        _sandbox.Write("a.txt", "un\nA\nB\ndeux\n");
        _sandbox.Git("add", "a.txt");

        Apply(GitLineAction.Unstage, "a.txt", false, diff => Lines(diff, "A"));

        Assert.Equal(("un\nB\ndeux\n", "un\nA\nB\ndeux\n"), (_sandbox.Git("show", ":a.txt"), _sandbox.Read("a.txt")));
    }

    [Fact]
    public void UnstageLines_WhenPartOfNewFile_ThenFileStaysStagedWithRemainingLines()
    {
        _sandbox.Commit("Base", ("a.txt", "a\n"));
        _sandbox.Write("n.txt", "x\ny\n");
        _sandbox.Git("add", "n.txt");

        Apply(GitLineAction.Unstage, "n.txt", false, diff => Lines(diff, "y"));

        Assert.Equal("x\n", _sandbox.Git("show", ":n.txt"));
    }

    [Fact]
    public void DiscardLines_ThenUndo_WhenPartOfHunk_ThenRestoresFile()
    {
        _sandbox.Commit("Base", ("a.txt", "un\ndeux\n"));
        _sandbox.Write("a.txt", "un\nA\nB\ndeux\n");

        var outcome = Apply(GitLineAction.Discard, "a.txt", false, diff => Lines(diff, "A"));
        var discarded = _sandbox.Read("a.txt");
        GitUndo.Apply(_sandbox.Repository, outcome.Undo);

        Assert.Equal(("un\nB\ndeux\n", "un\nA\nB\ndeux\n"), (discarded, _sandbox.Read("a.txt")));
    }

    [Fact]
    public void DiscardLines_WhenUntrackedFilePartially_ThenRemovesLinesFromFile()
    {
        _sandbox.Commit("Base", ("a.txt", "a\n"));
        _sandbox.Write("n.txt", "x\ny\nz\n");

        Apply(GitLineAction.Discard, "n.txt", true, diff => Lines(diff, "y"));

        Assert.Equal("x\nz\n", _sandbox.Read("n.txt"));
    }

    [Fact]
    public void DiscardLines_WhenNotConfirmed_ThenRefuses()
    {
        _sandbox.Commit("Base", ("a.txt", "a\n"));
        _sandbox.Write("a.txt", "b\n");
        var diff = Read("a.txt", false, GitDiffSource.Unstaged);

        Assert.Throws<GitCommandException>(() => GitLineCommands.Apply(_sandbox.Repository, GitLineAction.Discard, new GitLineRequestModel("a.txt", false, diff.Fingerprint, Changes(diff, 0)), false));

        Assert.Equal("b\n", _sandbox.Read("a.txt"));
    }

    [Fact]
    public void StageLines_WhenFileChangedSinceDiff_ThenRefusesInFrench()
    {
        _sandbox.Commit("Base", ("a.txt", "a\n"));
        _sandbox.Write("a.txt", "b\n");
        var diff = Read("a.txt", false, GitDiffSource.Unstaged);
        _sandbox.Write("a.txt", "c\n");

        var exception = Assert.Throws<GitCommandException>(() => GitLineCommands.Apply(_sandbox.Repository, GitLineAction.Stage, new GitLineRequestModel("a.txt", false, diff.Fingerprint, Changes(diff, 0)), false));

        Assert.Equal("Le diff a changé depuis son affichage : vérifiez la sélection puis recommencez.", exception.Message);
    }

    private GitOutcomeModel Apply(GitLineAction action, string path, bool untracked, Func<GitDiffModel, IReadOnlyList<GitHunkSelectionModel>> select)
    {
        var diff = Read(path, untracked, action == GitLineAction.Unstage ? GitDiffSource.Staged : GitDiffSource.Unstaged);
        return GitLineCommands.Apply(_sandbox.Repository, action, new GitLineRequestModel(path, untracked, diff.Fingerprint, select(diff)), true);
    }

    private GitDiffModel Read(string path, bool untracked, GitDiffSource source) =>
        GitDiffReader.Read(_sandbox.Repository, new GitDiffRequestModel(source, path, null, null, untracked));

    private static IReadOnlyList<GitHunkSelectionModel> Changes(GitDiffModel diff, int hunk) =>
        [new GitHunkSelectionModel(hunk, ChangedIndexes(diff.Hunks[hunk], _ => true))];

    private static IReadOnlyList<GitHunkSelectionModel> Lines(GitDiffModel diff, params string[] texts) =>
        diff.Hunks.Select((hunk, index) => new GitHunkSelectionModel(index, ChangedIndexes(hunk, line => texts.Contains(line.Text)))).Where(hunk => hunk.Lines.Count > 0).ToList();

    private static List<int> ChangedIndexes(GitDiffHunkModel hunk, Func<GitDiffLineModel, bool> wanted) =>
        hunk.Lines.Select((line, index) => (line, index)).Where(pair => pair.line.Kind is GitDiffLineKind.Added or GitDiffLineKind.Removed && wanted(pair.line)).Select(pair => pair.index).ToList();

    public void Dispose() => _sandbox.Dispose();
}
