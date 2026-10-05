using Tily.Core.Mcp;
using Xunit;

namespace Tily.Core.Tests.Mcp;

public sealed class McpFolderTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "tily-tests-" + Guid.NewGuid().ToString("N"), "Projet été");

    public McpFolderTests() => Directory.CreateDirectory(Path.Combine(_directory, "api"));

    [Fact]
    public void Resolve_WhenRelative_ThenFullPathFromCurrentDirectory()
    {
        var folder = McpFolder.Resolve("api", _directory);

        Assert.Equal(Path.Combine(_directory, "api"), folder);
    }

    [Fact]
    public void Resolve_WhenTrailingSeparator_ThenTrimmed()
    {
        var folder = McpFolder.Resolve(Path.Combine(_directory, "api") + Path.DirectorySeparatorChar, "/");

        Assert.Equal(Path.Combine(_directory, "api"), folder);
    }

    [Fact]
    public void Resolve_WhenDriveRoot_ThenKeepsTheSeparator()
    {
        var folder = McpFolder.Resolve("/", _directory);

        Assert.Equal("/", folder);
    }

    [Fact]
    public void Resolve_WhenMissing_ThenFailsInFrench()
    {
        var error = Assert.Throws<InvalidOperationException>(() => McpFolder.Resolve("absent", _directory));

        Assert.Equal($"Dossier introuvable : {Path.Combine(_directory, "absent")}.", error.Message);
    }

    [Fact]
    public void Resolve_WhenBlank_ThenNothing()
    {
        var folder = McpFolder.Resolve("  ", _directory);

        Assert.Null(folder);
    }

    public void Dispose() => Directory.Delete(Path.GetDirectoryName(_directory)!, true);
}
