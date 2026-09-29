using System.Text;
using Dock.Core.Files;
using Xunit;

namespace Dock.Core.Tests.Files;

public sealed class FilePreviewTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "dock-preview-" + Guid.NewGuid().ToString("N"), "dossier été");

    public FilePreviewTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void Read_WhenMarkdownFile_ThenReturnsContentAndBaseUrl()
    {
        var path = Write("README.md", Encoding.UTF8.GetBytes("# Titre accentué"));

        var preview = FilePreview.Read(path);

        Assert.Equal(new FilePreviewModel(path, "README.md", PreviewKind.Markdown, null, "# Titre accentué", false, PreviewAddress.BaseUrlOf(path), null), preview);
    }

    [Fact]
    public void Read_WhenUtf16WithBom_ThenDecodesText()
    {
        var path = Write("sortie.log", [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes("ligne é")]);

        var preview = FilePreview.Read(path);

        Assert.Equal("ligne é", preview.Content);
    }

    [Fact]
    public void Read_WhenInvalidUtf8_ThenFallsBackToLatin1()
    {
        var path = Write("ancien.txt", [0x63, 0x61, 0x66, 0xE9]);

        var preview = FilePreview.Read(path);

        Assert.Equal("café", preview.Content);
    }

    [Fact]
    public void Read_WhenContainsNulBytes_ThenReportsBinaryInFrench()
    {
        var path = Write("donnees.txt", [0x41, 0x00, 0x42]);

        var preview = FilePreview.Read(path);

        Assert.Equal("Fichier binaire : aperçu impossible.", preview.Error);
    }

    [Fact]
    public void Read_WhenLargerThanLimit_ThenTruncatesOnCharacterBoundary()
    {
        var path = Write("gros.txt", Encoding.UTF8.GetBytes("a" + new string('é', FilePreview.MaxBytes)));

        var preview = FilePreview.Read(path);

        Assert.True(preview.Truncated);
        Assert.Equal(1 + (FilePreview.MaxBytes - 2) / 2, preview.Content.Length);
    }

    [Fact]
    public void Read_WhenFileMissing_ThenReportsInFrench()
    {
        var path = Path.Combine(_root, "absent.md");

        var preview = FilePreview.Read(path);

        Assert.Equal($"Le fichier n’existe plus : {path}", preview.Error);
    }

    [Fact]
    public void Read_WhenImage_ThenReturnsLocalAddressWithVersion()
    {
        var path = Write("capture d’écran.png", [0x89, 0x50, 0x4E, 0x47]);

        var preview = FilePreview.Read(path);

        Assert.Equal($"{PreviewAddress.BaseUrlOf(path)}capture%20d%E2%80%99%C3%A9cran.png?v={File.GetLastWriteTimeUtc(path).Ticks}", preview.Content);
    }

    [Fact]
    public void Read_WhenImageAddress_ThenHostResolvesSameFile()
    {
        var path = Write("logo.png", [0x89, 0x50, 0x4E, 0x47]);

        var resolved = PreviewAddress.PathOf(FilePreview.Read(path).Content);

        Assert.Equal(path, resolved);
    }

    [Fact]
    public void Read_WhenImageMissing_ThenReportsInFrench()
    {
        var path = Path.Combine(_root, "absente.png");

        var preview = FilePreview.Read(path);

        Assert.Equal($"Le fichier n’existe plus : {path}", preview.Error);
    }

    [Fact]
    public void Read_WhenTypeNotPreviewable_ThenRefusesInFrench()
    {
        var path = Write("Program.cs", Encoding.UTF8.GetBytes("class A {}"));

        var exception = Assert.Throws<InvalidOperationException>(() => FilePreview.Read(path));

        Assert.Equal("Aperçu indisponible pour ce type de fichier : Program.cs", exception.Message);
    }

    private string Write(string name, byte[] bytes)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    public void Dispose() => Directory.Delete(Path.GetDirectoryName(_root)!, true);
}
