using Dock.Core.Git;
using Xunit;

namespace Dock.Core.Tests.Git;

public sealed class GitDiffParserTests
{
    private const string Diff = """
        diff --git a/src/app.ts b/src/app.ts
        index 1111111..2222222 100644
        --- a/src/app.ts
        +++ b/src/app.ts
        @@ -12,4 +12,5 @@ export function run() {
         const a = 1
        -const b = 2
        +const b = 3
        +const c = 4
         return a
        \ No newline at end of file

        """;

    [Fact]
    public void Parse_WhenUnifiedDiff_ThenNumbersLines()
    {
        var diff = GitDiffParser.Parse("src/app.ts", null, Diff);

        var hunk = Assert.Single(diff.Hunks);
        Assert.Equal("@@ -12,4 +12,5 @@ export function run() {", hunk.Header);
        Assert.Equal(
            [
                new GitDiffLineModel(GitDiffLineKind.Context, 12, 12, "const a = 1"),
                new GitDiffLineModel(GitDiffLineKind.Removed, 13, null, "const b = 2"),
                new GitDiffLineModel(GitDiffLineKind.Added, null, 13, "const b = 3"),
                new GitDiffLineModel(GitDiffLineKind.Added, null, 14, "const c = 4"),
                new GitDiffLineModel(GitDiffLineKind.Context, 14, 15, "return a"),
                new GitDiffLineModel(GitDiffLineKind.Note, null, null, "Pas de retour à la ligne en fin de fichier")
            ],
            hunk.Lines);
    }

    [Fact]
    public void Parse_WhenBinaryFile_ThenFlagsBinaryWithoutHunks()
    {
        var diff = GitDiffParser.Parse("logo.png", null, "diff --git a/logo.png b/logo.png\nnew file mode 100644\nindex 0000000..3333333\nBinary files /dev/null and b/logo.png differ\n");

        Assert.True(diff.Binary);
        Assert.Empty(diff.Hunks);
        Assert.Equal(["Nouveau fichier", "Fichier binaire : contenu non affiché"], diff.Notes);
    }

    [Fact]
    public void Parse_WhenMoreLinesThanLimit_ThenTruncates()
    {
        var diff = GitDiffParser.Parse("a.txt", null, "@@ -1,3 +1,3 @@\n-a\n+b\n c\n", 2);

        Assert.True(diff.Truncated);
        Assert.Equal(2, diff.Hunks[0].Lines.Count);
    }

    [Fact]
    public void Parse_WhenCrlfLines_ThenStripsCarriageReturn()
    {
        var diff = GitDiffParser.Parse("a.txt", null, "@@ -1 +1 @@\r\n-avant\r\n+après\r\n");

        Assert.Equal(["avant", "après"], diff.Hunks[0].Lines.Select(line => line.Text));
    }
}
