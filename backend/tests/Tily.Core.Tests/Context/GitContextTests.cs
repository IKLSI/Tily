using Tily.Core.Context;
using Xunit;

namespace Tily.Core.Tests.Context;

public sealed class GitContextTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "tily-git-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Resolve_WhenNoRepository_ThenNotARepository()
    {
        var folder = Path.Combine(_root, "sans depot");
        Directory.CreateDirectory(folder);

        var context = GitContext.Resolve(folder);

        Assert.False(context.IsRepository);
        Assert.Null(context.Branch);
    }

    [Fact]
    public void Resolve_WhenNestedFolderInRepository_ThenReturnsBranch()
    {
        var repository = Path.Combine(_root, "depot avec espaces");
        Directory.CreateDirectory(Path.Combine(repository, ".git"));
        File.WriteAllText(Path.Combine(repository, ".git", "HEAD"), "ref: refs/heads/feature/x\n");
        var nested = Path.Combine(repository, "src", "web");
        Directory.CreateDirectory(nested);

        var context = GitContext.Resolve(nested);

        Assert.True(context.IsRepository);
        Assert.Equal("feature/x", context.Branch);
        Assert.False(context.DetachedHead);
    }

    [Fact]
    public void Resolve_WhenHeadIsCommit_ThenDetached()
    {
        var repository = Path.Combine(_root, "detache");
        Directory.CreateDirectory(Path.Combine(repository, ".git"));
        File.WriteAllText(Path.Combine(repository, ".git", "HEAD"), "0123456789abcdef0123456789abcdef01234567\n");

        var context = GitContext.Resolve(repository);

        Assert.True(context.IsRepository);
        Assert.Null(context.Branch);
        Assert.True(context.DetachedHead);
    }

    [Fact]
    public void Resolve_WhenWorktreeGitFile_ThenFollowsGitDir()
    {
        var main = Path.Combine(_root, "principal");
        var worktreeGitDir = Path.Combine(main, ".git", "worktrees", "wt");
        Directory.CreateDirectory(worktreeGitDir);
        File.WriteAllText(Path.Combine(worktreeGitDir, "HEAD"), "ref: refs/heads/wt-branch\n");
        var worktree = Path.Combine(_root, "worktrees", "wt");
        Directory.CreateDirectory(worktree);
        File.WriteAllText(Path.Combine(worktree, ".git"), $"gitdir: {worktreeGitDir}\n");

        var context = GitContext.Resolve(worktree);

        Assert.Equal("wt-branch", context.Branch);
    }

    [Fact]
    public void Resolve_WhenInsideLinkedWorktree_ThenGivesWorktreeRoot()
    {
        var main = Path.Combine(_root, "principal");
        var worktreeGitDir = Path.Combine(main, ".git", "worktrees", "wt");
        Directory.CreateDirectory(worktreeGitDir);
        File.WriteAllText(Path.Combine(worktreeGitDir, "HEAD"), "ref: refs/heads/wt-branch\n");
        var worktree = Path.Combine(_root, "worktrees", "wt");
        Directory.CreateDirectory(Path.Combine(worktree, "src"));
        File.WriteAllText(Path.Combine(worktree, ".git"), $"gitdir: {worktreeGitDir}\n");

        var context = GitContext.Resolve(Path.Combine(worktree, "src"));

        Assert.Equal(worktree, context.WorktreeRoot);
    }

    [Fact]
    public void Resolve_WhenMainRepository_ThenNoWorktreeRoot()
    {
        var repository = Path.Combine(_root, "principal");
        Directory.CreateDirectory(Path.Combine(repository, ".git"));
        File.WriteAllText(Path.Combine(repository, ".git", "HEAD"), "ref: refs/heads/main\n");

        var context = GitContext.Resolve(repository);

        Assert.Null(context.WorktreeRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }
}
