using Tily.Core.Files;
using Xunit;

namespace Tily.Core.Tests.Files;

public sealed class PreviewAddressTests
{
    [Fact]
    public void BaseUrlOf_WhenPathHasSpacesAndAccents_ThenEscapesEachSegment()
    {
        var url = PreviewAddress.BaseUrlOf(@"C:\Mes projets\été\README.md");

        Assert.Equal("https://tily.files/C%3A/Mes%20projets/%C3%A9t%C3%A9/", url);
    }

    [Theory]
    [InlineData(@"C:\Mes projets\été\README.md", "images/logo.png", @"C:\Mes projets\été\images\logo.png")]
    [InlineData(@"C:\Mes projets\été\README.md", "../capture.png", @"C:\Mes projets\capture.png")]
    [InlineData(@"\\serveur\partage\docs\README.md", "a.png", @"\\serveur\partage\docs\a.png")]
    public void PathOf_WhenRelativeUrlResolvedAgainstBaseUrl_ThenReturnsFilePath(string file, string relative, string expected)
    {
        var url = new Uri(new Uri(PreviewAddress.BaseUrlOf(file)), relative).AbsoluteUri;

        var path = PreviewAddress.PathOf(url);

        Assert.Equal(expected, path);
    }

    [Theory]
    [InlineData("https://tily.app/index.html")]
    [InlineData("http://tily.files/C%3A/a.png")]
    [InlineData("https://tily.files/relatif.png")]
    [InlineData("https://tily.files/C%3A/%2E%2E%5Ca.png")]
    public void PathOf_WhenUrlIsNotAPreviewFile_ThenReturnsNull(string url)
    {
        var path = PreviewAddress.PathOf(url);

        Assert.Null(path);
    }
}
