using Tily.Core.Files;
using Xunit;

namespace Tily.Core.Tests.Files;

public sealed class PreviewTypesTests
{
    [Theory]
    [InlineData("/docs/README.md", PreviewKind.Markdown)]
    [InlineData("/docs/guide.MARKDOWN", PreviewKind.Markdown)]
    [InlineData("/docs/notes.txt", PreviewKind.Text)]
    [InlineData("/app/appsettings.json", PreviewKind.Text)]
    [InlineData("/app/.env", PreviewKind.Text)]
    [InlineData("/app/LICENSE", PreviewKind.Text)]
    [InlineData("/app/logo.PNG", PreviewKind.Image)]
    [InlineData("/app/icone.svg", PreviewKind.Image)]
    [InlineData("/docs/plan.html", PreviewKind.Html)]
    [InlineData("/docs/ancien.HTM", PreviewKind.Html)]
    public void KindOf_WhenFileIsReadable_ThenReturnsItsKind(string path, PreviewKind expected)
    {
        var kind = PreviewTypes.KindOf(path);

        Assert.Equal(expected, kind);
    }

    [Theory]
    [InlineData("/app/Program.cs")]
    [InlineData("/app/archive.zip")]
    [InlineData("/app/Makefile")]
    public void KindOf_WhenFileIsNotPreviewable_ThenReturnsNull(string path)
    {
        var kind = PreviewTypes.KindOf(path);

        Assert.Null(kind);
    }

    [Fact]
    public void LanguageOf_WhenYamlFile_ThenReturnsYaml()
    {
        var language = PreviewTypes.LanguageOf("/app/docker-compose.yml");

        Assert.Equal("yaml", language);
    }

    [Fact]
    public void LanguageOf_WhenHtmlFile_ThenReturnsHtml()
    {
        var language = PreviewTypes.LanguageOf("/docs/plan.html");

        Assert.Equal("html", language);
    }

    [Theory]
    [InlineData("/docs/plan.html", "text/html")]
    [InlineData("/docs/style.css", "text/css")]
    [InlineData("/docs/app.js", "text/javascript")]
    [InlineData("/docs/police.woff2", "font/woff2")]
    [InlineData("/docs/logo.png", "image/png")]
    public void PageResourceContentType_WhenPageResource_ThenReturnsItsType(string path, string expected)
    {
        var contentType = PreviewTypes.PageResourceContentType(path);

        Assert.Equal(expected, contentType);
    }

    [Theory]
    [InlineData("/docs/secret.txt")]
    [InlineData("/docs/donnees.json")]
    [InlineData("/docs/outil.exe")]
    public void PageResourceContentType_WhenOtherFile_ThenReturnsNull(string path)
    {
        var contentType = PreviewTypes.PageResourceContentType(path);

        Assert.Null(contentType);
    }

    [Fact]
    public void ImageContentType_WhenSvg_ThenReturnsSvgType()
    {
        var contentType = PreviewTypes.ImageContentType("/docs/schema.SVG");

        Assert.Equal("image/svg+xml", contentType);
    }

    [Fact]
    public void ImageContentType_WhenNotAnImage_ThenReturnsNull()
    {
        var contentType = PreviewTypes.ImageContentType("/docs/secret.txt");

        Assert.Null(contentType);
    }
}
