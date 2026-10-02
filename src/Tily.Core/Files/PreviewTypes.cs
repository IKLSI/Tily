using System.Text.Json.Serialization;

namespace Tily.Core.Files;

[JsonConverter(typeof(JsonStringEnumConverter<PreviewKind>))]
public enum PreviewKind
{
    [JsonStringEnumMemberName("markdown")] Markdown,
    [JsonStringEnumMemberName("text")] Text,
    [JsonStringEnumMemberName("image")] Image,
    [JsonStringEnumMemberName("html")] Html
}

public static class PreviewTypes
{
    private const string HtmlLanguage = "html";

    private static readonly HashSet<string> MarkdownExtensions = new(StringComparer.OrdinalIgnoreCase) { ".md", ".markdown", ".mdown", ".mkd" };

    private static readonly HashSet<string> HtmlExtensions = new(StringComparer.OrdinalIgnoreCase) { ".html", ".htm" };

    private static readonly Dictionary<string, string?> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".txt"] = null,
        [".text"] = null,
        [".log"] = null,
        [".csv"] = null,
        [".tsv"] = null,
        [".json"] = "json",
        [".jsonc"] = "json",
        [".yml"] = "yaml",
        [".yaml"] = "yaml",
        [".toml"] = "ini",
        [".ini"] = "ini",
        [".cfg"] = "ini",
        [".conf"] = "ini",
        [".properties"] = "ini",
        [".editorconfig"] = "ini",
        [".env"] = "ini",
        [".xml"] = "xml",
        [".gitignore"] = null,
        [".gitattributes"] = null,
        [".npmrc"] = "ini"
    };

    private static readonly HashSet<string> TextFileNames = new(StringComparer.OrdinalIgnoreCase) { "LICENSE", "LICENCE", "README", "CHANGELOG", "AUTHORS", "NOTICE", "COPYING" };

    private static readonly Dictionary<string, string> ImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".svg"] = "image/svg+xml",
        [".bmp"] = "image/bmp",
        [".ico"] = "image/x-icon",
        [".avif"] = "image/avif"
    };

    private static readonly Dictionary<string, string> PageResourceContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".html"] = "text/html",
        [".htm"] = "text/html",
        [".css"] = "text/css",
        [".js"] = "text/javascript",
        [".mjs"] = "text/javascript",
        [".woff"] = "font/woff",
        [".woff2"] = "font/woff2",
        [".ttf"] = "font/ttf",
        [".otf"] = "font/otf"
    };

    public static PreviewKind? KindOf(string path)
    {
        var extension = Path.GetExtension(path);
        if (MarkdownExtensions.Contains(extension))
        {
            return PreviewKind.Markdown;
        }

        if (HtmlExtensions.Contains(extension))
        {
            return PreviewKind.Html;
        }

        if (TextExtensions.ContainsKey(extension) || TextFileNames.Contains(Path.GetFileName(path)))
        {
            return PreviewKind.Text;
        }

        return ImageContentTypes.ContainsKey(extension) ? PreviewKind.Image : null;
    }

    public static string? LanguageOf(string path)
    {
        var extension = Path.GetExtension(path);
        return HtmlExtensions.Contains(extension) ? HtmlLanguage : TextExtensions.GetValueOrDefault(extension);
    }

    public static string? ImageContentType(string path) =>
        ImageContentTypes.GetValueOrDefault(Path.GetExtension(path));

    public static string? PageResourceContentType(string path) =>
        ImageContentType(path) ?? PageResourceContentTypes.GetValueOrDefault(Path.GetExtension(path));
}
