using Tily.Core.Worktrees;
using Xunit;

namespace Tily.Core.Tests.Worktrees;

public sealed class WorktreeListerTests
{
    private const string Output =
        "worktree C:/Files/Projects/tily terminal\0HEAD 1111111111111111111111111111111111111111\0branch refs/heads/main\0\0" +
        "worktree C:/Files/Projects/worktrees/tily-vue\0HEAD 2222222222222222222222222222222222222222\0branch refs/heads/feat/vue\0locked\0\0" +
        "worktree C:/Files/Projects/worktrees/tily-essai\0HEAD 3333333333333333333333333333333333333333\0detached\0prunable gitdir file points to non-existent location\0\0";

    [Fact]
    public void Parse_WhenPorcelainOutput_ThenFirstWorktreeIsMain()
    {
        var worktrees = WorktreeLister.Parse(Output);

        Assert.Equal([true, false, false], worktrees.Select(worktree => worktree.IsMain));
    }

    [Fact]
    public void Parse_WhenPathHasSpaces_ThenKeepsItWithBackslashes()
    {
        var worktrees = WorktreeLister.Parse(Output);

        Assert.Equal(@"C:\Files\Projects\tily terminal", worktrees[0].Path);
    }

    [Fact]
    public void Parse_WhenBranchIsQualified_ThenStripsRefsHeads()
    {
        var worktrees = WorktreeLister.Parse(Output);

        Assert.Equal(["main", "feat/vue", null], worktrees.Select(worktree => worktree.Branch));
    }

    [Fact]
    public void Parse_WhenMarkersPresent_ThenReportsLockedDetachedAndPrunable()
    {
        var worktrees = WorktreeLister.Parse(Output);

        Assert.Equal([(false, false, false), (true, false, false), (false, true, true)], worktrees.Select(worktree => (worktree.Locked, worktree.IsDetached, worktree.Prunable)));
    }

    [Fact]
    public void Parse_WhenMainIsBare_ThenSkipsItWithoutPromotingNextOne()
    {
        var worktrees = WorktreeLister.Parse("worktree C:/depot.git\0bare\0\0worktree C:/wt\0HEAD 4444444444444444444444444444444444444444\0branch refs/heads/x\0\0");

        Assert.False(Assert.Single(worktrees).IsMain);
    }
}
