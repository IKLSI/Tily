using Dock.Core.Git;
using Xunit;

namespace Dock.Core.Tests.Git;

public sealed class GitPatchBuilderTests
{
    private const string Header = "diff --git a/a.txt b/a.txt\nindex 1111111..2222222 100644\n--- a/a.txt\n+++ b/a.txt\n";

    private const string OneHunk = Header + "@@ -1,3 +1,4 @@ bloc\n un\n-deux\n+DEUX\n+trois\n quatre\n";

    private const string TwoHunks = Header + "@@ -1,2 +1,3 @@\n un\n+a\n deux\n@@ -10,2 +11,3 @@\n dix\n+b\n onze\n";

    [Fact]
    public void Build_WhenForwardPartial_ThenDropsUnselectedAddedAndKeepsUnselectedRemovedAsContext()
    {
        var patch = GitPatchBuilder.Build(OneHunk, [new GitHunkSelectionModel(0, [3])], false);

        Assert.Equal(Header + "@@ -1,3 +1,4 @@ bloc\n un\n deux\n+trois\n quatre\n", patch);
    }

    [Fact]
    public void Build_WhenReversePartial_ThenKeepsUnselectedAddedAsContextAndDropsUnselectedRemoved()
    {
        var patch = GitPatchBuilder.Build(OneHunk, [new GitHunkSelectionModel(0, [1])], true);

        Assert.Equal(Header + "@@ -1,5 +1,4 @@ bloc\n un\n-deux\n DEUX\n trois\n quatre\n", patch);
    }

    [Fact]
    public void Build_WhenEarlierHunkSkipped_ThenLaterHunkStartsAtOldPosition()
    {
        var patch = GitPatchBuilder.Build(TwoHunks, [new GitHunkSelectionModel(1, [1])], false);

        Assert.Equal(Header + "@@ -10,2 +10,3 @@\n dix\n+b\n onze\n", patch);
    }

    [Fact]
    public void Build_WhenBothHunksSelected_ThenLaterHunkIsShiftedByEarlierOne()
    {
        var patch = GitPatchBuilder.Build(TwoHunks, [new GitHunkSelectionModel(0, [1]), new GitHunkSelectionModel(1, [1])], false);

        Assert.Equal(TwoHunks, patch);
    }

    [Fact]
    public void Build_WhenNewFileReversedPartially_ThenBecomesModificationPatch()
    {
        const string diff = "diff --git a/n.txt b/n.txt\nnew file mode 100644\nindex 0000000..3333333\n--- /dev/null\n+++ b/n.txt\n@@ -0,0 +1,2 @@\n+x\n+y\n";

        var patch = GitPatchBuilder.Build(diff, [new GitHunkSelectionModel(0, [1])], true);

        Assert.Equal("diff --git a/n.txt b/n.txt\nindex 0000000..3333333\n--- a/n.txt\n+++ b/n.txt\n@@ -1,1 +1,2 @@\n x\n+y\n", patch);
    }

    [Fact]
    public void Build_WhenLineEndsWithCarriageReturn_ThenKeepsIt()
    {
        const string diff = Header + "@@ -1,1 +1,2 @@\n un\r\n+deux\r\n";

        var patch = GitPatchBuilder.Build(diff, [new GitHunkSelectionModel(0, [1])], false);

        Assert.EndsWith("\n un\r\n+deux\r\n", patch);
    }

    [Fact]
    public void Build_WhenContextLineSelected_ThenRefusesInFrench()
    {
        var exception = Assert.Throws<GitCommandException>(() => GitPatchBuilder.Build(OneHunk, [new GitHunkSelectionModel(0, [0])], false));

        Assert.Equal("La sélection ne correspond plus au diff : rechargez-le puis recommencez.", exception.Message);
    }
}
