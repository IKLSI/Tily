using Dock.Core.Git;
using Xunit;

namespace Dock.Core.Tests.Git;

public sealed class GitStatusParserTests
{
    private static string Porcelain(params string[] entries) => string.Join('\0', entries) + '\0';

    [Fact]
    public void Parse_WhenBranchHeaders_ThenReadsHead()
    {
        var output = Porcelain("# branch.oid 0123456789abcdef0123456789abcdef01234567", "# branch.head feature/git", "# branch.upstream origin/feature/git", "# branch.ab +2 -3");

        var status = GitStatusParser.Parse(output);

        Assert.Equal(new GitHeadModel("feature/git", "0123456789abcdef0123456789abcdef01234567", false, false, "origin/feature/git", 2, 3), status.Head);
    }

    [Fact]
    public void Parse_WhenInitialAndDetached_ThenFlagsHead()
    {
        var status = GitStatusParser.Parse(Porcelain("# branch.oid (initial)", "# branch.head (detached)"));

        Assert.True(status.Head.Unborn);
        Assert.True(status.Head.Detached);
        Assert.Null(status.Head.Sha);
        Assert.Null(status.Head.Branch);
    }

    [Fact]
    public void Parse_WhenMixedChanges_ThenSplitsStagedAndUnstaged()
    {
        var output = Porcelain(
            "1 D. N... 100644 000000 000000 aaaa 0000 a.txt",
            "1 .M N... 100644 100644 100644 bbbb bbbb dir avec espace/é.txt",
            "2 R. N... 100644 100644 100644 cccc cccc R100 e.txt",
            "d.txt",
            "1 AM N... 000000 100644 100644 0000 dddd s.txt",
            "? notes.md");

        var status = GitStatusParser.Parse(output);

        Assert.Equal(
            [new GitFileChangeModel("a.txt", null, GitChangeKind.Deleted), new GitFileChangeModel("e.txt", "d.txt", GitChangeKind.Renamed), new GitFileChangeModel("s.txt", null, GitChangeKind.Added)],
            status.Staged);
        Assert.Equal(
            [new GitFileChangeModel("dir avec espace/é.txt", null, GitChangeKind.Modified), new GitFileChangeModel("s.txt", null, GitChangeKind.Modified), new GitFileChangeModel("notes.md", null, GitChangeKind.Untracked)],
            status.Unstaged);
    }

    [Fact]
    public void Parse_WhenUnmerged_ThenListsConflictKind()
    {
        var status = GitStatusParser.Parse(Porcelain("u AA N... 000000 100644 100644 100644 0000 1111 2222 conflit.txt", "u UD N... 100644 100644 000000 100644 3333 4444 0000 supprimé.txt"));

        Assert.Equal([new GitConflictModel("conflit.txt", GitConflictKind.BothAdded), new GitConflictModel("supprimé.txt", GitConflictKind.DeletedByThem)], status.Conflicts);
        Assert.Empty(status.Staged);
    }

    [Fact]
    public void Parse_WhenMoreFilesThanLimit_ThenKeepsTotal()
    {
        var status = GitStatusParser.Parse(Porcelain("? a", "? b", "? c"), 2);

        Assert.Equal(2, status.Unstaged.Count);
        Assert.Equal(3, status.UnstagedTotal);
    }
}
