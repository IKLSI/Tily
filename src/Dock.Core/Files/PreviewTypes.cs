using System.Text.Json.Serialization;

namespace Dock.Core.Files;

[JsonConverter(typeof(JsonStringEnumConverter<PreviewKind>))]
public enum PreviewKind
{
    [JsonStringEnumMemberName("markdown")] Markdown,
    [JsonStringEnumMemberName("text")] Text
}

public static class PreviewTypes
{
    private static readonly HashSet<string> MarkdownExtensions = new(StringComparer.OrdinalIgnoreCase) { ".md", ".markdown", ".mdown", ".mkd" };

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

    public static PreviewKind? KindOf(string path)
    {
        var extension = Path.GetExtension(path);
        if (MarkdownExtensions.Contains(extension))
        {
            return PreviewKind.Markdown;
        }

        return TextExtensions.ContainsKey(extension) || TextFileNames.Contains(Path.GetFileName(path)) ? PreviewKind.Text : null;
    }

    public static string? LanguageOf(string path) =>
        TextExtensions.GetValueOrDefault(Path.GetExtension(path));

    public static string? ImageContentType(string path) =>
        ImageContentTypes.GetValueOrDefault(Path.GetExtension(path));
}
