using Dock.Core.Files;
using Xunit;

namespace Dock.Core.Tests.Files;

public sealed class PreviewLinksTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "dock-links-" + Guid.NewGuid().ToString("N"));
    private readonly string _readme;

    public PreviewLinksTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, ".git"));
        Directory.CreateDirectory(Path.Combine(_root, "docs", "guide été"));
        _readme = Touch(Path.Combine("docs", "README.md"));
    }

    [Fact]
    public void Resolve_WhenWebLink_ThenOpensInBrowser()
    {
        var link = PreviewLinks.Resolve(_readme, "https://example.com/page");

        Assert.Equal(new PreviewLinkModel(PreviewLinkKind.Web, "https://example.com/page", null), link);
    }

    [Fact]
    public void Resolve_WhenRelativeMarkdownWithEscapedNameAndAnchor_ThenPreviewsTarget()
    {
        var target = Touch(Path.Combine("docs", "guide été", "Installation.md"));

        var link = PreviewLinks.Resolve(_readme, "guide%20%C3%A9t%C3%A9/Installation.md#pr%C3%A9requis");

        Assert.Equal(new PreviewLinkModel(PreviewLinkKind.Preview, target, "prérequis"), link);
    }

    [Fact]
    public void Resolve_WhenRootRelativeLink_ThenResolvesFromRepositoryRoot()
    {
        var target = Touch("CHANGELOG.md");

        var link = PreviewLinks.Resolve(_readme, "/CHANGELOG.md");

        Assert.Equal(target, link.Target);
    }

    [Fact]
    public void Resolve_WhenAnchorOnly_ThenStaysOnCurrentFile()
    {
        var link = PreviewLinks.Resolve(_readme, "#usage");

        Assert.Equal(new PreviewLinkModel(PreviewLinkKind.Preview, _readme, "usage"), link);
    }

    [Fact]
    public void Resolve_WhenSourceFile_ThenOpensInEditor()
    {
        var target = Touch("Program.cs");

        var link = PreviewLinks.Resolve(_readme, "../Program.cs");

        Assert.Equal(new PreviewLinkModel(PreviewLinkKind.Editor, target, null), link);
    }

    [Fact]
    public void Resolve_WhenFolderWithReadme_ThenPreviewsReadme()
    {
        var link = PreviewLinks.Resolve(Touch("README.md"), "docs/");

        Assert.Equal(_readme, link.Target);
    }

    [Fact]
    public void Resolve_WhenTargetMissing_ThenRefusesInFrench()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => PreviewLinks.Resolve(_readme, "absent.md"));

        Assert.Equal($"Fichier introuvable : {Path.Combine(_root, "docs", "absent.md")}", exception.Message);
    }

    [Fact]
    public void Resolve_WhenUnsupportedScheme_ThenRefusesInFrench()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => PreviewLinks.Resolve(_readme, "javascript:alert(1)"));

        Assert.Equal("Lien non pris en charge : javascript:alert(1)", exception.Message);
    }

    private string Touch(string relative)
    {
        var path = Path.Combine(_root, relative);
        File.WriteAllText(path, string.Empty);
        return path;
    }

    public void Dispose() => Directory.Delete(_root, true);
}
