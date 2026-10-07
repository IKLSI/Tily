using Tily.Core.Shell;
using Xunit;

namespace Tily.Core.Tests.Shell;

public sealed class DroppedFilesTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "tily-drops-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Save_WhenFileHasName_ThenWritesContentUnderThatName()
    {
        var path = new DroppedFiles(_root).Save("Capture d’écran 2026-10-07 à 14.03.12.png", [1, 2, 3]);

        Assert.StartsWith(_root, path);
        Assert.Equal("Capture d’écran 2026-10-07 à 14.03.12.png", Path.GetFileName(path));
        Assert.Equal([1, 2, 3], File.ReadAllBytes(path));
    }

    [Fact]
    public void Save_WhenSameNameTwice_ThenKeepsBothFiles()
    {
        var files = new DroppedFiles(_root);

        var first = files.Save("image.png", [1]);
        var second = files.Save("image.png", [2]);

        Assert.NotEqual(first, second);
        Assert.Equal([1], File.ReadAllBytes(first));
    }

    [Theory]
    [InlineData("../../evil.png", "evil.png")]
    [InlineData("..", "fichier")]
    [InlineData("", "fichier")]
    [InlineData("a\nb.png", "fichier")]
    public void Save_WhenNameIsUnsafe_ThenStaysInsideItsFolder(string name, string expected)
    {
        var path = new DroppedFiles(_root).Save(name, [1]);

        Assert.StartsWith(_root, path);
        Assert.Equal(expected, Path.GetFileName(path));
    }

    [Fact]
    public void Save_WhenContentTooLarge_ThenRefusesIt()
    {
        Assert.Throws<InvalidOperationException>(() => new DroppedFiles(_root).Save("big.png", new byte[DroppedFiles.MaxBytes + 1]));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
