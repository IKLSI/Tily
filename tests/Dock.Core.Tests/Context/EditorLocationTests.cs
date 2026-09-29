using Dock.Core.Context;
using Xunit;

namespace Dock.Core.Tests.Context;

public sealed class EditorLocationTests
{
    [Fact]
    public void Resolve_WhenRelativePath_ThenCombinesWithFolder()
    {
        var path = EditorLocation.Resolve(@"D:\Projets\dépôt", "src/app.ts");

        Assert.Equal(@"D:\Projets\dépôt\src\app.ts", path);
    }

    [Fact]
    public void Resolve_WhenParentPath_ThenNormalizes()
    {
        var path = EditorLocation.Resolve(@"D:\Projets\dépôt\web", @"..\src\Program.cs");

        Assert.Equal(@"D:\Projets\dépôt\src\Program.cs", path);
    }

    [Fact]
    public void Resolve_WhenAbsolutePath_ThenIgnoresFolder()
    {
        var path = EditorLocation.Resolve(@"D:\ailleurs", @"C:\dépôt\a.cs");

        Assert.Equal(@"C:\dépôt\a.cs", path);
    }

    [Fact]
    public void Resolve_WhenRelativePathWithoutFolder_ThenFails()
    {
        var error = Assert.Throws<InvalidOperationException>(() => EditorLocation.Resolve(null, "a.cs"));

        Assert.Equal("Chemin relatif sans dossier courant : a.cs", error.Message);
    }

    [Fact]
    public void Arguments_WhenVisualStudioCodeWithLine_ThenGoesToLineAndColumn()
    {
        var arguments = EditorLocation.Arguments("code.cmd", @"C:\dépôt\a.cs", 12, 5);

        Assert.Equal(["-g", @"C:\dépôt\a.cs:12:5"], arguments);
    }

    [Fact]
    public void Arguments_WhenQuotedCursorPathWithoutColumn_ThenStartsAtFirstColumn()
    {
        var arguments = EditorLocation.Arguments(@"""C:\Program Files\cursor\cursor.exe""",@"C:\dépôt\a.cs", 3, 0);

        Assert.Equal(["-g", @"C:\dépôt\a.cs:3:1"], arguments);
    }

    [Fact]
    public void Arguments_WhenOtherEditor_ThenOpensFileOnly()
    {
        var arguments = EditorLocation.Arguments("notepad++.exe", @"C:\dépôt\a.cs", 12, 5);

        Assert.Equal([@"C:\dépôt\a.cs"], arguments);
    }

    [Fact]
    public void Arguments_WhenNoLine_ThenOpensFileOnly()
    {
        var arguments = EditorLocation.Arguments("code.cmd", @"C:\dépôt\a.cs", 0, 0);

        Assert.Equal([@"C:\dépôt\a.cs"], arguments);
    }
}
