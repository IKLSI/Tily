using Tily.Core.Context;
using Xunit;

namespace Tily.Core.Tests.Context;

public sealed class CommandLocatorTests : IDisposable
{
    private readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "tily-commande-" + Guid.NewGuid().ToString("N"))).FullName;

    [Fact]
    public void Exists_WhenNameInPath_ThenFound()
    {
        File.WriteAllText(Path.Combine(_folder, "editeur"), "#!/bin/sh");

        Assert.True(CommandLocator.Exists("editeur", $"/absent{Path.PathSeparator}{_folder}"));
    }

    [Fact]
    public void Find_WhenNameInPath_ThenReturnsFullPath()
    {
        var editor = Path.Combine(_folder, "editeur");
        File.WriteAllText(editor, "#!/bin/sh");

        Assert.Equal(editor, CommandLocator.Find("editeur", _folder));
    }

    [Fact]
    public void Exists_WhenNameMissingFromPath_ThenNotFound()
    {
        Assert.False(CommandLocator.Exists("editeur-introuvable-tily", _folder));
    }

    [Fact]
    public void Exists_WhenAbsolutePathQuoted_ThenChecksFile()
    {
        var editor = Path.Combine(_folder, "dossier avec espace", "code");
        Directory.CreateDirectory(Path.GetDirectoryName(editor)!);

        var before = CommandLocator.Exists($"\"{editor}\"", null);
        File.WriteAllText(editor, "x");

        Assert.False(before);
        Assert.True(CommandLocator.Exists($"\"{editor}\"", null));
    }

    public void Dispose() => Directory.Delete(_folder, true);
}
