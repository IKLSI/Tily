using Tily.Core.Files;
using Xunit;

namespace Tily.Core.Tests.Files;

public sealed class FileExplorerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "tily-files-" + Guid.NewGuid().ToString("N"));

    public FileExplorerTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void List_WhenFolderHasEntries_ThenReturnsFoldersFirstSortedWithoutCase()
    {
        File.WriteAllText(Path.Combine(_root, "b.txt"), string.Empty);
        File.WriteAllText(Path.Combine(_root, "A.md"), string.Empty);
        File.WriteAllText(Path.Combine(_root, ".env"), string.Empty);
        Directory.CreateDirectory(Path.Combine(_root, "src"));
        Directory.CreateDirectory(Path.Combine(_root, ".git"));

        var listing = FileExplorer.List(_root);

        Assert.Null(listing.Error);
        Assert.Equal([".git", "src", ".env", "A.md", "b.txt"], listing.Entries.Select(entry => entry.Name));
    }

    [Fact]
    public void List_WhenFolderMissing_ThenReturnsEmptyWithFrenchError()
    {
        var missing = Path.Combine(_root, "absent");

        var listing = FileExplorer.List(missing);

        Assert.Empty(listing.Entries);
        Assert.Equal($"Le dossier n’existe plus : {missing}", listing.Error);
    }

    [Fact]
    public void CreateFile_WhenNameFree_ThenCreatesEmptyFile()
    {
        var path = FileExplorer.CreateFile(_root, "notes.txt");

        Assert.True(File.Exists(path));
    }

    [Fact]
    public void CreateFolder_WhenNameTaken_ThenRefusesInFrench()
    {
        File.WriteAllText(Path.Combine(_root, "src"), string.Empty);

        var exception = Assert.Throws<InvalidOperationException>(() => FileExplorer.CreateFolder(_root, "src"));

        Assert.Equal("« src » existe déjà dans ce dossier.", exception.Message);
    }

    [Theory]
    [InlineData("a/b")]
    [InlineData("..")]
    [InlineData("a:b")]
    [InlineData("ligne\nsuivante")]
    [InlineData("   ")]
    public void CreateFile_WhenNameInvalid_ThenRefuses(string name)
    {
        Assert.Throws<InvalidOperationException>(() => FileExplorer.CreateFile(_root, name));
    }

    [Fact]
    public void Rename_WhenFolderCaseOnlyChange_ThenRenames()
    {
        var source = FileExplorer.CreateFolder(_root, "docs");

        var target = FileExplorer.Rename(source, "Docs");

        Assert.Equal(["Docs"], new DirectoryInfo(_root).EnumerateDirectories().Select(directory => directory.Name));
        Assert.Equal(Path.Combine(_root, "Docs"), target);
    }

    [Fact]
    public void Rename_WhenTargetExists_ThenRefusesWithoutMoving()
    {
        var source = FileExplorer.CreateFile(_root, "a.txt");
        FileExplorer.CreateFile(_root, "b.txt");

        Assert.Throws<InvalidOperationException>(() => FileExplorer.Rename(source, "b.txt"));

        Assert.True(File.Exists(source));
    }

    [Fact]
    public void RecycleBinSend_WhenPathMissing_ThenRefusesInFrench()
    {
        var missing = Path.Combine(_root, "absent.txt");

        var exception = Assert.Throws<InvalidOperationException>(() => RecycleBin.Send(missing));

        Assert.Equal($"L’élément n’existe plus : {missing}", exception.Message);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }
}
