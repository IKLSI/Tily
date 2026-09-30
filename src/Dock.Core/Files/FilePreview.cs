using System.Text;

namespace Dock.Core.Files;

public sealed record FilePreviewModel(string Path, string Name, PreviewKind Kind, string? Language, string Content, bool Truncated, string BaseUrl, string? Error);

public static class FilePreview
{
    public const int MaxBytes = 2 * 1024 * 1024;
    private const int BinaryProbeBytes = 8000;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static FilePreviewModel Read(string path)
    {
        FileExplorer.RequireFullPath(path);
        var name = System.IO.Path.GetFileName(path);
        var kind = PreviewTypes.KindOf(path);
        if (kind is null)
        {
            return new FilePreviewModel(path, name, PreviewKind.Text, null, string.Empty, false, PreviewAddress.BaseUrlOf(path), $"Aperçu indisponible pour ce type de fichier : {name}. « Ouvrir dans l’éditeur » l’ouvre dans l’éditeur.");
        }

        var empty = new FilePreviewModel(path, name, kind.Value, PreviewTypes.LanguageOf(path), string.Empty, false, PreviewAddress.BaseUrlOf(path), null);
        if (kind == PreviewKind.Image)
        {
            return File.Exists(path)
                ? empty with { Content = ImageUrl(path) }
                : empty with { Error = $"Le fichier n’existe plus : {path}" };
        }

        try
        {
            var (bytes, truncated) = ReadHead(path);
            var content = Decode(bytes, truncated);
            return content is null
                ? empty with { Error = "Fichier binaire : aperçu impossible." }
                : empty with { Content = content, Truncated = truncated };
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return empty with { Error = $"Le fichier n’existe plus : {path}" };
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
        {
            return empty with { Error = $"Fichier illisible : {exception.Message}" };
        }
    }

    private static string ImageUrl(string path) =>
        $"{PreviewAddress.BaseUrlOf(path)}{Uri.EscapeDataString(System.IO.Path.GetFileName(path))}?v={File.GetLastWriteTimeUtc(path).Ticks}";

    private static (byte[] Bytes, bool Truncated) ReadHead(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var buffer = new byte[(int)Math.Min(stream.Length, MaxBytes)];
        stream.ReadExactly(buffer);
        return (buffer, stream.Length > MaxBytes);
    }

    private static string? Decode(byte[] bytes, bool truncated)
    {
        if (StartsWith(bytes, 0xEF, 0xBB, 0xBF))
        {
            return DecodeUtf8(bytes.AsSpan(3), truncated);
        }

        if (StartsWith(bytes, 0xFF, 0xFE))
        {
            return Encoding.Unicode.GetString(bytes, 2, EvenLength(bytes.Length - 2));
        }

        if (StartsWith(bytes, 0xFE, 0xFF))
        {
            return Encoding.BigEndianUnicode.GetString(bytes, 2, EvenLength(bytes.Length - 2));
        }

        if (bytes.AsSpan(0, Math.Min(bytes.Length, BinaryProbeBytes)).Contains((byte)0))
        {
            return null;
        }

        return DecodeUtf8(bytes, truncated);
    }

    private static string DecodeUtf8(ReadOnlySpan<byte> bytes, bool truncated)
    {
        var usable = truncated ? bytes[..CompleteUtf8Length(bytes)] : bytes;
        try
        {
            return StrictUtf8.GetString(usable);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(bytes);
        }
    }

    private static int CompleteUtf8Length(ReadOnlySpan<byte> bytes)
    {
        var start = bytes.Length;
        while (start > 0 && bytes.Length - start < 3 && (bytes[start - 1] & 0xC0) == 0x80)
        {
            start--;
        }

        if (start == 0 || bytes[start - 1] < 0x80)
        {
            return bytes.Length;
        }

        var lead = bytes[start - 1];
        var expected = lead >= 0xF0 ? 4 : lead >= 0xE0 ? 3 : lead >= 0xC0 ? 2 : 1;
        return bytes.Length - (start - 1) >= expected ? bytes.Length : start - 1;
    }

    private static bool StartsWith(byte[] bytes, params byte[] prefix) =>
        bytes.AsSpan().StartsWith(prefix);

    private static int EvenLength(int length) => length - length % 2;
}
