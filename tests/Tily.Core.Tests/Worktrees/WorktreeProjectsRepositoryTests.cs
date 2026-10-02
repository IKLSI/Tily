using Tily.Core.Worktrees;
using Xunit;

namespace Tily.Core.Tests.Worktrees;

public sealed class WorktreeProjectsRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "tily-worktree-projects-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void RepositoryOf_WhenNothingSaved_ThenReturnsNull()
    {
        var repository = new WorktreeProjectsRepository(_directory);

        var stored = repository.RepositoryOf(@"C:\Projets\App Starter Kit");

        Assert.Null(stored);
    }

    [Fact]
    public void SaveRepository_WhenReadAgain_ThenReturnsItWhateverTheCase()
    {
        new WorktreeProjectsRepository(_directory).SaveRepository(@"C:\Projets\App Starter Kit\", @"C:\Projets\App Starter Kit\app-starter-kit");

        var stored = new WorktreeProjectsRepository(_directory).RepositoryOf(@"c:\projets\app starter kit");

        Assert.Equal(@"C:\Projets\App Starter Kit\app-starter-kit", stored);
    }

    [Fact]
    public void RepositoryOf_WhenFileCorrupted_ThenReturnsNull()
    {
        var repository = new WorktreeProjectsRepository(_directory);
        File.WriteAllText(repository.FilePath, "{ illisible");

        var stored = repository.RepositoryOf(@"C:\Projets\App Starter Kit");

        Assert.Null(stored);
    }

    [Fact]
    public void SaveRepository_WhenProjectRelative_ThenRefusesInFrench()
    {
        var repository = new WorktreeProjectsRepository(_directory);

        var exception = Assert.Throws<InvalidOperationException>(() => repository.SaveRepository("relatif", @"C:\Projets\dépôt"));

        Assert.StartsWith("Le dossier du projet et celui du dépôt doivent être des chemins absolus", exception.Message);
    }

    [Fact]
    public void SaveRepository_WhenFolderSaved_ThenKeepsFolder()
    {
        var repository = new WorktreeProjectsRepository(_directory);
        repository.SaveFolders([new WorktreeProjectFolderModel(@"C:\Projets\App Starter Kit", @"F:\arbres")]);

        repository.SaveRepository(@"C:\Projets\App Starter Kit", @"C:\Projets\App Starter Kit\app");

        Assert.Equal(new WorktreeProjectFolderModel(@"C:\Projets\App Starter Kit", @"F:\arbres"), Assert.Single(repository.Folders()));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
