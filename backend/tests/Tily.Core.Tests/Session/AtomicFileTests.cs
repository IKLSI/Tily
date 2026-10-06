using Tily.Core.Session;
using Xunit;

namespace Tily.Core.Tests.Session;

public sealed class AtomicFileTests : IDisposable
{
    private readonly string _directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "tily-tests-" + Guid.NewGuid().ToString("N"))).FullName;

    [Fact]
    public void Write_WhenFileExists_ThenReplacesContentWithoutLeavingTemporaryFile()
    {
        var filePath = Path.Combine(_directory, "réglages.json");
        File.WriteAllText(filePath, "ancien");

        AtomicFile.Write(filePath, "nouveau");

        Assert.Equal("nouveau", File.ReadAllText(filePath));
        Assert.Equal([filePath], Directory.GetFiles(_directory));
    }

    [Fact]
    public void Write_WhenFileIsSymbolicLink_ThenKeepsLinkAndWritesTarget()
    {
        var target = Path.Combine(_directory, "cible.json");
        var link = Path.Combine(_directory, "lien.json");
        File.WriteAllText(target, "ancien");
        File.CreateSymbolicLink(link, target);

        AtomicFile.Write(link, "nouveau");

        Assert.NotNull(new FileInfo(link).LinkTarget);
        Assert.Equal("nouveau", File.ReadAllText(target));
    }

    [Fact]
    public void Write_WhenFileHasRestrictedMode_ThenKeepsMode()
    {
        var filePath = Path.Combine(_directory, "secret.json");
        File.WriteAllText(filePath, "ancien");
        File.SetUnixFileMode(filePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);

        AtomicFile.Write(filePath, "nouveau");

        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(filePath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
