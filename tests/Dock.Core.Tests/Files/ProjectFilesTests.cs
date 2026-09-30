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
