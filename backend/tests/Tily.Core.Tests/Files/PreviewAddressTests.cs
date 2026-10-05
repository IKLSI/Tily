using Tily.Core.Files;
using Xunit;

namespace Tily.Core.Tests.Files;

public sealed class PreviewAddressTests
{
    [Fact]
    public void BaseUrlOf_WhenPathHasSpacesAndAccents_ThenEscapesEachSegment()
    {
        var url = PreviewAddress.BaseUrlOf("/Mes projets/été/README.md");

        Assert.Equal("https://tily.files/Mes%20projets/%C3%A9t%C3%A9/", url);
    }

    [Theory]
    [InlineData("/Mes projets/été/README.md", "images/logo.png", "/Mes projets/été/images/logo.png")]
    [InlineData("/Mes projets/été/README.md", "../capture.png", "/Mes projets/capture.png")]
    public void PathOf_WhenRelativeUrlResolvedAgainstBaseUrl_ThenReturnsFilePath(string file, string relative, string expected)
    {
        var url = new Uri(new Uri(PreviewAddress.BaseUrlOf(file)), relative).AbsoluteUri;

        var path = PreviewAddress.PathOf(url);

        Assert.Equal(expected, path);
    }

    [Theory]
    [InlineData("https://tily.example/index.html")]
    [InlineData("http://tily.files/C%3A/a.png")]
    [InlineData("https://tily.files/a%2Fb.png")]
    [InlineData("https://tily.files/")]
    public void PathOf_WhenUrlIsNotAPreviewFile_ThenReturnsNull(string url)
    {
        var path = PreviewAddress.PathOf(url);

        Assert.Null(path);
    }
}
