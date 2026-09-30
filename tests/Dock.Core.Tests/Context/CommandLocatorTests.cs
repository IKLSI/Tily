using Dock.Core.Context;
using Xunit;

namespace Dock.Core.Tests.Context;

public sealed class CommandLocatorTests : IDisposable
{
    private const string Extensions = ".COM;.EXE;.BAT;.CMD";

    private readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "dock-commande-" + Guid.NewGuid().ToString("N"))).FullName;

    private static bool NothingRegistered(string executable) => false;

    [Fact]
    public void Exists_WhenNameWithoutExtensionIsCmdInPath_ThenFound()
    {
        File.WriteAllText(Path.Combine(_folder, "editeur.cmd"), "@echo off");

        Assert.True(CommandLocator.Exists("editeur", $"C:\\absent;{_folder}", Extensions, NothingRegistered));
    }

    [Fact]
    public void Exists_WhenNameMissingFromPath_ThenNotFound()
    {
        Assert.False(CommandLocator.Exists("editeur-introuvable", _folder, Extensions, NothingRegistered));
    }

    [Fact]
    public void Exists_WhenNameRegisteredAsApplication_ThenFoundWithExeExtension()
    {
        var asked = new List<string>();

        var found = CommandLocator.Exists("notepad++", _folder, Extensions, executable =>
        {
            asked.Add(executable);
            return true;
        });

        Assert.True(found);
        Assert.Equal(["notepad++.exe"], asked);
    }

    [Fact]
    public void Exists_WhenAbsolutePathQuoted_ThenChecksFile()
    {
        var editor = Path.Combine(_folder, "dossier avec espace", "code.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(editor)!);

        var before = CommandLocator.Exists($"\"{editor}\"", null, Extensions, NothingRegistered);
        File.WriteAllText(editor, "x");

        Assert.False(before);
        Assert.True(CommandLocator.Exists($"\"{editor}\"", null, Extensions, NothingRegistered));
    }

    public void Dispose() => Directory.Delete(_folder, true);
}
