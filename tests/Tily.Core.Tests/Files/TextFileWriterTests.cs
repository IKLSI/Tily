using System.Text;
using Tily.Core.Files;
using Xunit;

namespace Tily.Core.Tests.Files;

public sealed class TextFileWriterTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "tily-writer-" + Guid.NewGuid().ToString("N"), "dossier été");

    public TextFileWriterTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void Save_WhenUtf8File_ThenWritesContentWithoutBom()
    {
        var path = Write("notes.txt", Encoding.UTF8.GetBytes("avant"));

        var error = TextFileWriter.Save(path, "après\nfin", TextFileWriter.VersionOf(path), false);

        Assert.Null(error);
        Assert.Equal(Encoding.UTF8.GetBytes("après\nfin"), File.ReadAllBytes(path));
    }

    [Fact]
    public void Save_WhenFileUsesCrlf_ThenKeepsCrlf()
    {
        var path = Write("config.json", Encoding.UTF8.GetBytes("{\r\n}"));

        TextFileWriter.Save(path, "{\n  \"a\": 1\n}", TextFileWriter.VersionOf(path), false);

        Assert.Equal("{\r\n  \"a\": 1\r\n}", File.ReadAllText(path));
    }

    [Fact]
    public void Save_WhenFileUsesLf_ThenConvertsCrlfToLf()
    {
        var path = Write("README.md", Encoding.UTF8.GetBytes("# Titre\n"));

        TextFileWriter.Save(path, "# Titre\r\ntexte\r\n", TextFileWriter.VersionOf(path), false);

        Assert.Equal("# Titre\ntexte\n", File.ReadAllText(path));
    }

    [Fact]
    public void Save_WhenUtf8WithBom_ThenKeepsBom()
    {
        var path = Write("script.ini", [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes("a=1")]);

        TextFileWriter.Save(path, "a=é", TextFileWriter.VersionOf(path), false);

        Assert.Equal([.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes("a=é")], File.ReadAllBytes(path));
    }

    [Fact]
    public void Save_WhenUtf16_ThenKeepsEncoding()
    {
        var path = Write("sortie.log", [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes("ligne")]);

        TextFileWriter.Save(path, "ligne é", TextFileWriter.VersionOf(path), false);

        Assert.Equal([.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes("ligne é")], File.ReadAllBytes(path));
    }

    [Fact]
    public void Save_WhenLatin1_ThenKeepsLatin1()
    {
        var path = Write("ancien.txt", [0x63, 0x61, 0x66, 0xE9]);

        TextFileWriter.Save(path, "café crème", TextFileWriter.VersionOf(path), false);

        Assert.Equal(Encoding.Latin1.GetBytes("café crème"), File.ReadAllBytes(path));
    }

    [Fact]
    public void Save_WhenLatin1CannotHoldCharacter_ThenRefusesInFrench()
    {
        var path = Write("ancien.txt", [0x63, 0x61, 0x66, 0xE9]);

        var error = TextFileWriter.Save(path, "café €", TextFileWriter.VersionOf(path), false);

        Assert.Equal("Ce fichier est encodé en Latin-1 : le caractère « € » ne peut pas y être enregistré.", error);
    }

    [Fact]
    public void Save_WhenModifiedSinceOpened_ThenRefusesInFrenchAndKeepsFile()
    {
        var path = Write("notes.txt", Encoding.UTF8.GetBytes("avant"));
        var version = TextFileWriter.VersionOf(path);
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(1));

        var error = TextFileWriter.Save(path, "après", version, false);

        Assert.Equal("Le fichier a été modifié sur le disque depuis son ouverture.", error);
        Assert.Equal("avant", File.ReadAllText(path));
    }

    [Fact]
    public void Save_WhenModifiedSinceOpenedAndForced_ThenOverwrites()
    {
        var path = Write("notes.txt", Encoding.UTF8.GetBytes("avant"));
        var version = TextFileWriter.VersionOf(path);
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(1));

        TextFileWriter.Save(path, "après", version, true);

        Assert.Equal("après", File.ReadAllText(path));
    }

    [Fact]
    public void Save_WhenDeletedSinceOpened_ThenRefusesInFrench()
    {
        var path = Write("notes.txt", Encoding.UTF8.GetBytes("avant"));
        var version = TextFileWriter.VersionOf(path);
        File.Delete(path);

        var error = TextFileWriter.Save(path, "après", version, false);

        Assert.Equal($"Le fichier a été supprimé depuis son ouverture : {path}", error);
    }

    [Fact]
    public void Save_WhenDeletedSinceOpenedAndForced_ThenRecreatesInUtf8()
    {
        var path = Write("notes.txt", Encoding.UTF8.GetBytes("avant"));
        var version = TextFileWriter.VersionOf(path);
        File.Delete(path);

        TextFileWriter.Save(path, "après", version, true);

        Assert.Equal(Encoding.UTF8.GetBytes("après"), File.ReadAllBytes(path));
    }

    [Fact]
    public void Save_WhenImage_ThenRefusesInFrench()
    {
        var path = Write("logo.png", [0x89, 0x50, 0x4E, 0x47]);

        var error = TextFileWriter.Save(path, "texte", TextFileWriter.VersionOf(path), false);

        Assert.Equal("Ce type de fichier ne se modifie pas dans Tily : logo.png.", error);
    }

    [Fact]
    public void Save_WhenBinary_ThenRefusesInFrench()
    {
        var path = Write("donnees.txt", [0x41, 0x00, 0x42]);

        var error = TextFileWriter.Save(path, "AB", TextFileWriter.VersionOf(path), false);

        Assert.Equal("Fichier binaire : modification impossible.", error);
    }

    [Fact]
    public void Save_WhenLargerThanLimit_ThenRefusesInFrench()
    {
        var path = Write("gros.txt", Encoding.UTF8.GetBytes(new string('a', FilePreview.MaxBytes + 1)));

        var error = TextFileWriter.Save(path, "a", TextFileWriter.VersionOf(path), false);

        Assert.Equal("Fichier trop volumineux pour être modifié dans Tily (plus de 2 Mo).", error);
    }

    [Fact]
    public void Read_WhenTextFile_ThenReturnsVersionForSave()
    {
        var path = Write("notes.txt", Encoding.UTF8.GetBytes("texte"));

        var preview = FilePreview.Read(path);

        Assert.Null(TextFileWriter.Save(path, "nouveau", preview.Version, false));
    }

    private string Write(string name, byte[] bytes)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    public void Dispose() => Directory.Delete(Path.GetDirectoryName(_root)!, true);
}
