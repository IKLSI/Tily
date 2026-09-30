using Tily.Core.Git;
using Xunit;

namespace Tily.Core.Tests.Git;

public sealed class GitNamesTests
{
    [Theory]
    [InlineData("main")]
    [InlineData("origin/feature/git")]
    [InlineData("HEAD~2")]
    [InlineData("0123456789abcdef")]
    public void RequireRevision_WhenRevisionValid_ThenReturnsIt(string revision)
    {
        var accepted = GitNames.RequireRevision(revision);

        Assert.Equal(revision, accepted);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("--upload-pack=calc")]
    [InlineData("-n")]
    [InlineData("main extra")]
    [InlineData("main\n")]
    public void RequireRevision_WhenRevisionEmptyOptionLikeOrSpaced_ThenRefuses(string? revision)
    {
        var exception = Assert.Throws<GitCommandException>(() => GitNames.RequireRevision(revision));

        Assert.StartsWith("Référence Git invalide", exception.Message);
    }

    [Theory]
    [InlineData("src/app.ts")]
    [InlineData(@"dossier avec espace\fichier.txt")]
    [InlineData("notes..bak.txt")]
    public void RequireRelativePath_WhenPathInsideRepository_ThenReturnsIt(string path)
    {
        var accepted = GitNames.RequireRelativePath(path);

        Assert.Equal(path, accepted);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(@"C:\Windows\win.ini")]
    [InlineData("/etc/passwd")]
    [InlineData("../secret.txt")]
    [InlineData(@"src\..\..\secret.txt")]
    public void RequireRelativePath_WhenPathRootedOrEscapingRepository_ThenRefuses(string? path)
    {
        var exception = Assert.Throws<GitCommandException>(() => GitNames.RequireRelativePath(path));

        Assert.StartsWith("Chemin de fichier invalide", exception.Message);
    }
}
