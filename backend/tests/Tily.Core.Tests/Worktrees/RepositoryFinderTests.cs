using Tily.Core.Worktrees;
using Xunit;

namespace Tily.Core.Tests.Worktrees;

public sealed class RepositoryFinderTests : IDisposable
{
    private readonly string _project = Path.Combine(Path.GetTempPath(), "tily-dépôts-" + Guid.NewGuid().ToString("N"), "App Starter Kit");

    [Fact]
    public void Find_WhenProjectContainsNestedRepository_ThenReturnsIt()
    {
        var repository = Repository("app-starter-kit");

        var found = RepositoryFinder.Find(_project);

        Assert.Equal([repository], found);
    }

    [Fact]
    public void Find_WhenProjectIsRepository_ThenDoesNotLookInside()
    {
        var project = Repository(string.Empty);
        Repository("interne");

        var found = RepositoryFinder.Find(_project);

        Assert.Equal([project], found);
    }

    [Fact]
    public void Find_WhenRepositoriesAtSeveralDepths_ThenOrdersBreadthFirst()
    {
        var deep = Repository(Path.Combine("a", "profond"));
        var shallow = Repository("z-proche");

        var found = RepositoryFinder.Find(_project);

        Assert.Equal([shallow, deep], found);
    }

    [Fact]
    public void Find_WhenRepositoryBeyondMaxDepth_ThenIgnoresIt()
    {
        Repository(Path.Combine("un", "deux", "trois", "quatre"));

        var found = RepositoryFinder.Find(_project);

        Assert.Empty(found);
    }

    [Fact]
    public void Find_WhenRepositoryInSkippedOrDottedFolder_ThenIgnoresIt()
    {
        Repository(Path.Combine("node_modules", "paquet"));
        Repository(Path.Combine(".cache", "copie"));

        var found = RepositoryFinder.Find(_project);

        Assert.Empty(found);
    }

    [Fact]
    public void Find_WhenWorktreeHasGitFile_ThenReturnsIt()
    {
        var worktree = Path.Combine(_project, "worktree");
        Directory.CreateDirectory(worktree);
        File.WriteAllText(Path.Combine(worktree, ".git"), "gitdir: ailleurs");

        var found = RepositoryFinder.Find(_project);

        Assert.Equal([worktree], found);
    }

    public void Dispose()
    {
        var root = Path.GetDirectoryName(_project)!;
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }

    private string Repository(string relativePath)
    {
        var folder = WorktreeLister.NormalizePath(Path.Combine(_project, relativePath));
        Directory.CreateDirectory(Path.Combine(folder, ".git"));
        return folder;
    }
}
