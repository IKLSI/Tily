using Tily.Core.Context;
using Xunit;

namespace Tily.Core.Tests.Context;

public sealed class EditorLocationTests
{
    [Fact]
    public void Resolve_WhenRelativePath_ThenCombinesWithFolder()
    {
        var path = EditorLocation.Resolve("/Users/moi/Projets/dépôt", "src/app.ts");

        Assert.Equal("/Users/moi/Projets/dépôt/src/app.ts", path);
    }

    [Fact]
    public void Resolve_WhenParentPath_ThenNormalizes()
    {
        var path = EditorLocation.Resolve("/Users/moi/Projets/dépôt/web", "../src/Program.cs");

        Assert.Equal("/Users/moi/Projets/dépôt/src/Program.cs", path);
    }

    [Fact]
    public void Resolve_WhenAbsolutePath_ThenIgnoresFolder()
    {
        var path = EditorLocation.Resolve("/Users/moi/ailleurs", "/Users/moi/dépôt/a.cs");

        Assert.Equal("/Users/moi/dépôt/a.cs", path);
    }

    [Fact]
    public void Resolve_WhenHomeRelativePath_ThenExpandsHome()
    {
        var path = EditorLocation.Resolve("/Users/moi/ailleurs", "~/dépôt/a.cs");

        Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "dépôt", "a.cs"), path);
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
        var arguments = EditorLocation.Arguments("code", "/Users/moi/dépôt/a.cs", 12, 5);

        Assert.Equal(["-g", "/Users/moi/dépôt/a.cs:12:5"], arguments);
    }

    [Fact]
    public void Arguments_WhenQuotedCursorPathWithoutColumn_ThenStartsAtFirstColumn()
    {
        var arguments = EditorLocation.Arguments(@"""/Applications/Cursor.app/Contents/Resources/app/bin/cursor""", "/Users/moi/dépôt/a.cs", 3, 0);

        Assert.Equal(["-g", "/Users/moi/dépôt/a.cs:3:1"], arguments);
    }

    [Fact]
    public void Arguments_WhenOtherEditor_ThenOpensFileOnly()
    {
        var arguments = EditorLocation.Arguments("subl", "/Users/moi/dépôt/a.cs", 12, 5);

        Assert.Equal(["/Users/moi/dépôt/a.cs"], arguments);
    }

    [Fact]
    public void ResolveExisting_WhenGitDiffSidePrefix_ThenFallsBackToFileWithoutPrefix()
    {
        var path = EditorLocation.ResolveExisting("/Users/moi/dépôt", "b/web/src/a.ts", candidate => candidate == "/Users/moi/dépôt/web/src/a.ts");

        Assert.Equal("/Users/moi/dépôt/web/src/a.ts", path);
    }

    [Fact]
    public void ResolveExisting_WhenPrefixedFolderExists_ThenKeepsIt()
    {
        var path = EditorLocation.ResolveExisting("/Users/moi/dépôt", "a/b.ts", candidate => candidate == "/Users/moi/dépôt/a/b.ts");

        Assert.Equal("/Users/moi/dépôt/a/b.ts", path);
    }

    [Fact]
    public void ResolveExisting_WhenSpacedPathExists_ThenKeepsIt()
    {
        var path = EditorLocation.ResolveExisting("/Users/moi/ailleurs", "/Users/moi/Projet T/src/a.cs", candidate => candidate == "/Users/moi/Projet T/src/a.cs");

        Assert.Equal("/Users/moi/Projet T/src/a.cs", path);
    }

    [Fact]
    public void ResolveExisting_WhenSpacedPathIsSentence_ThenFallsBackToPathAfterSpace()
    {
        var path = EditorLocation.ResolveExisting("/Users/moi/dépôt", "/Users/moi/Projets then src/a.cs", candidate => candidate == "/Users/moi/dépôt/src/a.cs");

        Assert.Equal("/Users/moi/dépôt/src/a.cs", path);
    }

    [Fact]
    public void ExistingExactly_WhenOnlyPathAfterSpaceExists_ThenReturnsNull()
    {
        var path = EditorLocation.ExistingExactly("/Users/moi/dépôt", "/Users/moi/repo/a.ts and src/b.ts", candidate => candidate == "/Users/moi/dépôt/src/b.ts");

        Assert.Null(path);
    }

    [Fact]
    public void ExistingExactly_WhenDottedFolderPathExists_ThenReturnsIt()
    {
        var path = EditorLocation.ExistingExactly(null, "/Users/moi/Node.js Apps/index.ts", candidate => candidate == "/Users/moi/Node.js Apps/index.ts");

        Assert.Equal("/Users/moi/Node.js Apps/index.ts", path);
    }

    [Fact]
    public void ResolveExisting_WhenNothingExists_ThenReturnsNull()
    {
        var path = EditorLocation.ResolveExisting("/Users/moi/dépôt", "a/web/x.ts", _ => false);

        Assert.Null(path);
    }

    [Fact]
    public void Arguments_WhenNoLine_ThenOpensFileOnly()
    {
        var arguments = EditorLocation.Arguments("code", "/Users/moi/dépôt/a.cs", 0, 0);

        Assert.Equal(["/Users/moi/dépôt/a.cs"], arguments);
    }
}
