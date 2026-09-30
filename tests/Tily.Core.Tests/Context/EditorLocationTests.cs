using Tily.Core.Context;
using Xunit;

namespace Tily.Core.Tests.Context;

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
    public void ResolveExisting_WhenGitDiffSidePrefix_ThenFallsBackToFileWithoutPrefix()
    {
        var path = EditorLocation.ResolveExisting(@"D:\dépôt", "b/web/src/a.ts", candidate => candidate == @"D:\dépôt\web\src\a.ts");

        Assert.Equal(@"D:\dépôt\web\src\a.ts", path);
    }

    [Fact]
    public void ResolveExisting_WhenPrefixedFolderExists_ThenKeepsIt()
    {
        var path = EditorLocation.ResolveExisting(@"D:\dépôt", "a/b.ts", candidate => candidate == @"D:\dépôt\a\b.ts");

        Assert.Equal(@"D:\dépôt\a\b.ts", path);
    }

    [Fact]
    public void ResolveExisting_WhenSpacedPathExists_ThenKeepsIt()
    {
        var path = EditorLocation.ResolveExisting(@"D:\ailleurs", @"D:\Projets\Projet T\src\a.cs", candidate => candidate == @"D:\Projets\Projet T\src\a.cs");

        Assert.Equal(@"D:\Projets\Projet T\src\a.cs", path);
    }

    [Fact]
    public void ResolveExisting_WhenSpacedPathIsSentence_ThenFallsBackToPathAfterSpace()
    {
        var path = EditorLocation.ResolveExisting(@"D:\dépôt", @"D:\Projets then src\a.cs", candidate => candidate == @"D:\dépôt\src\a.cs");

        Assert.Equal(@"D:\dépôt\src\a.cs", path);
    }

    [Fact]
    public void ExistingExactly_WhenOnlyPathAfterSpaceExists_ThenReturnsNull()
    {
        var path = EditorLocation.ExistingExactly(@"D:\dépôt", @"C:\repo\a.ts and src\b.ts", candidate => candidate == @"D:\dépôt\src\b.ts");

        Assert.Null(path);
    }

    [Fact]
    public void ExistingExactly_WhenDottedFolderPathExists_ThenReturnsIt()
    {
        var path = EditorLocation.ExistingExactly(null, @"C:\Tools\Node.js Apps\index.ts", candidate => candidate == @"C:\Tools\Node.js Apps\index.ts");

        Assert.Equal(@"C:\Tools\Node.js Apps\index.ts", path);
    }

    [Fact]
    public void ResolveExisting_WhenNothingExists_ThenReturnsNull()
    {
        var path = EditorLocation.ResolveExisting(@"D:\dépôt", "a/web/x.ts", _ => false);

        Assert.Null(path);
    }

    [Fact]
    public void CommandLine_WhenShellCharacters_ThenQuotesEveryArgument()
    {
        var commandLine = EditorLocation.CommandLine(["-g", @"C:\R&D\a^b%c.ts:3:1"]);

        Assert.Equal(@"""-g"" ""C:\R&D\a^b%c.ts:3:1""", commandLine);
    }

    [Fact]
    public void CommandLine_WhenTrailingBackslash_ThenKeepsClosingQuote()
    {
        var commandLine = EditorLocation.CommandLine([@"C:\"]);

        Assert.Equal(@"""C:\\""", commandLine);
    }

    [Fact]
    public void Arguments_WhenNoLine_ThenOpensFileOnly()
    {
        var arguments = EditorLocation.Arguments("code.cmd", @"C:\dépôt\a.cs", 0, 0);

        Assert.Equal([@"C:\dépôt\a.cs"], arguments);
    }
}
