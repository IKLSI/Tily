using Dock.Core.Files;
using Xunit;

namespace Dock.Core.Tests.Files;

public sealed class PreviewTypesTests
{
    [Theory]
    [InlineData(@"C:\docs\README.md", PreviewKind.Markdown)]
    [InlineData(@"C:\docs\guide.MARKDOWN", PreviewKind.Markdown)]
    [InlineData(@"C:\docs\notes.txt", PreviewKind.Text)]
    [InlineData(@"C:\app\appsettings.json", PreviewKind.Text)]
    [InlineData(@"C:\app\.env", PreviewKind.Text)]
    [InlineData(@"C:\app\LICENSE", PreviewKind.Text)]
    public void KindOf_WhenFileIsReadable_ThenReturnsItsKind(string path, PreviewKind expected)
    {
        var kind = PreviewTypes.KindOf(path);

        Assert.Equal(expected, kind);
    }

    [Theory]
    [InlineData(@"C:\app\Program.cs")]
    [InlineData(@"C:\app\logo.png")]
    [InlineData(@"C:\app\Makefile")]
    public void KindOf_WhenFileIsNotPreviewable_ThenReturnsNull(string path)
    {
        var kind = PreviewTypes.KindOf(path);

        Assert.Null(kind);
    }

    [Fact]
    public void LanguageOf_WhenYamlFile_ThenReturnsYaml()
    {
        var language = PreviewTypes.LanguageOf(@"C:\app\docker-compose.yml");

        Assert.Equal("yaml", language);
    }

    [Fact]
    public void ImageContentType_WhenSvg_ThenReturnsSvgType()
    {
        var contentType = PreviewTypes.ImageContentType(@"C:\docs\schema.SVG");

        Assert.Equal("image/svg+xml", contentType);
    }

    [Fact]
    public void ImageContentType_WhenNotAnImage_ThenReturnsNull()
    {
        var contentType = PreviewTypes.ImageContentType(@"C:\docs\secret.txt");

        Assert.Null(contentType);
    }
}
