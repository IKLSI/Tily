using Dock.Core.Projects;
using Xunit;

namespace Dock.Core.Tests.Projects;

public sealed class ProjectCatalogTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "dock-projects-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void List_WhenRootHasFolders_ThenReturnsFirstLevelSortedWithoutWorktrees()
    {
        Directory.CreateDirectory(Path.Combine(_root, "zeta"));
        Directory.CreateDirectory(Path.Combine(_root, "Alpha", "nested"));
        Directory.CreateDirectory(Path.Combine(_root, "worktrees"));
        File.WriteAllText(Path.Combine(_root, "fichier.txt"), string.Empty);

        var list = ProjectCatalog.List(_root);

        Assert.Null(list.Error);
        Assert.Equal(["Alpha", "zeta"], list.Projects.Select(project => project.Name));
        Assert.Equal(Path.Combine(_root, "Alpha"), list.Projects[0].Path);
    }

    [Fact]
    public void List_WhenWorktreesFolderHasFolders_ThenAppendsThemAsWorktreesAfterProjects()
    {
        Directory.CreateDirectory(Path.Combine(_root, "projet"));
        Directory.CreateDirectory(Path.Combine(_root, "worktrees", "projet-zeta"));
        Directory.CreateDirectory(Path.Combine(_root, "worktrees", "Projet-alpha"));
        File.WriteAllText(Path.Combine(_root, "worktrees", "notes.txt"), string.Empty);

        var list = ProjectCatalog.List(_root);

        Assert.Equal([("projet", false), ("Projet-alpha", true), ("projet-zeta", true)], list.Projects.Select(project => (project.Name, project.Worktree)));
    }

    [Fact]
    public void List_WhenWorktreeListed_ThenPathPointsIntoWorktreesFolder()
    {
        Directory.CreateDirectory(Path.Combine(_root, "worktrees", "projet-branche"));

        var list = ProjectCatalog.List(_root);

        Assert.Equal(Path.Combine(_root, "worktrees", "projet-branche"), Assert.Single(list.Projects).Path);
    }

    [Fact]
    public void List_WhenWorktreeFolderConfigured_ThenListsItAndHidesItFromProjects()
    {
        Directory.CreateDirectory(Path.Combine(_root, "projet"));
        Directory.CreateDirectory(Path.Combine(_root, "arbres", "projet-vue"));

        var list = ProjectCatalog.List(_root, Path.Combine(_root, "arbres"));

        Assert.Equal([("projet", false), ("projet-vue", true)], list.Projects.Select(project => (project.Name, project.Worktree)));
    }

    [Fact]
    public void List_WhenRootMissing_ThenReturnsEmptyWithFrenchError()
    {
        var list = ProjectCatalog.List(_root);

        Assert.Empty(list.Projects);
        Assert.Equal($"Le dossier des projets est introuvable : {_root}", list.Error);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }
}
