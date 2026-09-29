using Dock.Core.Context;
using Xunit;

namespace Dock.Core.Tests.Context;

public sealed class LocalActionsTests
{
    [Theory]
    [InlineData("http://localhost:5173/")]
    [InlineData("https://github.com/MaximeRazafinjato/dock-terminal")]
    public void RequireWebLink_WhenHttpOrHttps_ThenReturnsUri(string url)
    {
        var uri = LocalActions.RequireWebLink(url);

        Assert.Equal(new Uri(url), uri);
    }

    [Theory]
    [InlineData("file:///C:/Windows/System32/calc.exe")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ms-settings:display")]
    public void RequireWebLink_WhenOtherScheme_ThenRefusesWithSchemeInMessage(string url)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => LocalActions.RequireWebLink(url));

        Assert.Contains("seuls les liens http et https sont autorisés", exception.Message);
    }

    [Fact]
    public void RevealInExplorer_WhenPathMissing_ThenRefusesInFrench()
    {
        var missing = Path.Combine(Path.GetTempPath(), "dock-absent-" + Guid.NewGuid().ToString("N"), "fichier.txt");

        var exception = Assert.Throws<InvalidOperationException>(() => LocalActions.RevealInExplorer(missing));

        Assert.Equal($"L’élément n’existe plus : {missing}", exception.Message);
    }

    [Fact]
    public void RequireWebLink_WhenRelative_ThenRefuses()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => LocalActions.RequireWebLink("chemin/relatif"));

        Assert.Contains("adresse invalide", exception.Message);
    }
}
