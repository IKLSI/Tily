using Dock.Core.Files;
using Dock.Core.Git;
using Dock.Core.Tests.Git;
using Xunit;

namespace Dock.Core.Tests.Files;

public sealed class ProjectFilesTests : IDisposable
{
    private readonly GitSandbox _sandbox = new();
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dock-fichiers-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void List_WhenRepository_ThenGivesTrackedAndUntrackedWithoutIgnoredNorDeleted()
    {
        _sandbox.Commit("initial", (".gitignore", "ignoré.log\n"), ("src/a.ts", "a"), ("supprimé.txt", "x"));
        _sandbox.Write("dossier avec espace/é.md", "é");
        _sandbox.Write("ignoré.log", "log");
        File.Delete(Path.Combine(_sandbox.Work, "supprimé.txt"));

        var listing = ProjectFiles.List(_sandbox.Runner, Path.Combine(_sandbox.Work, "src"));

        Assert.Equal(Path.GetFullPath(_sandbox.Work), Path.GetFullPath(listing.Root));
        Assert.Equal([".gitignore", @"dossier avec espace\é.md", @"src\a.ts"], listing.Files);
        Assert.False(listing.Truncated);
    }

    [Fact]
    public void List_WhenRepositoryHasChanges_ThenGivesModifiedStagedAndUntrackedButNotDeleted()
    {
        _sandbox.Commit("initial", ("a.txt", "a"), ("b.txt", "b"), ("c.txt", "c"), ("d.txt", "d"));
        _sandbox.Write("a.txt", "modifié");
        _sandbox.Write("b.txt", "indexé");
        _sandbox.Git("add", "b.txt");
        _sandbox.Write("nouveau/n.txt", "n");
        File.Delete(Path.Combine(_sandbox.Work, "d.txt"));

        var listing = ProjectFiles.List(_sandbox.Runner, _sandbox.Work);

        Assert.Equal(["a.txt", "b.txt", @"nouveau\n.txt"], listing.Changed);
        Assert.Equal(["a.txt", "b.txt", "c.txt", @"nouveau\n.txt"], listing.Files);
    }

    [Fact]
    public void List_WhenIndexedFileDeletedAndNestedRepository_ThenListsNeither()
    {
        _sandbox.Commit("initial", ("a.txt", "a"));
        _sandbox.Write("indexé.txt", "i");
        _sandbox.Git("add", "indexé.txt");
        File.Delete(Path.Combine(_sandbox.Work, "indexé.txt"));
        _sandbox.Write(@"imbriqué\b.txt", "b");
        _sandbox.GitIn(Path.Combine(_sandbox.Work, "imbriqué"), "init", "-q");

        var listing = ProjectFiles.List(_sandbox.Runner, _sandbox.Work);

        Assert.Equal(["a.txt"], listing.Files);
        Assert.Empty(listing.Changed);
    }

    [Fact]
    public void List_WhenSingleFolderHasTooManyFiles_ThenStopsInsideItAndSignalsTruncation()
    {
        Directory.CreateDirectory(_folder);
        for (var index = 0; index < 30; index++)
        {
            File.WriteAllText(Path.Combine(_folder, $"f{index:00}.txt"), "x");
        }

        var listing = ProjectFiles.List(new GitRunner(), _folder, 3);

        Assert.Equal(3, listing.Files.Count);
        Assert.True(listing.Truncated);
    }

    [Fact]
    public void List_WhenPlainFolderTakesTooLong_ThenSignalsTruncation()
    {
        Directory.CreateDirectory(Path.Combine(_folder, "a"));
        File.WriteAllText(Path.Combine(_folder, "a", "un.txt"), "x");

        var listing = ProjectFiles.List(new GitRunner(), _folder, diskTimeLimit: TimeSpan.Zero);

        Assert.True(listing.Truncated);
    }

    [Fact]
    public void List_WhenPlainFolder_ThenSkipsHeavyFoldersAndSignalsTruncation()
    {
        foreach (var path in new[] { @"a\un.txt", @"a\deux.txt", @"node_modules\paquet\index.js", @"bin\Debug\app.dll", "trois.txt" })
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(_folder, path))!);
            File.WriteAllText(Path.Combine(_folder, path), "x");
        }

        var complete = ProjectFiles.List(new GitRunner(), _folder);
        var limited = ProjectFiles.List(new GitRunner(), _folder, 2);

        Assert.Equal([@"a\deux.txt", @"a\un.txt", "trois.txt"], complete.Files);
        Assert.Equal(2, limited.Files.Count);
        Assert.True(limited.Truncated);
    }

    [Fact]
    public void List_WhenFolderMissing_ThenExplainsInFrench()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => ProjectFiles.List(new GitRunner(), _folder));

        Assert.Equal($"Dossier introuvable : {_folder}", exception.Message);
    }

    public void Dispose()
    {
        _sandbox.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }
}
