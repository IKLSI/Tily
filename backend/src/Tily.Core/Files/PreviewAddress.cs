namespace Tily.Core.Files;

public static class PreviewAddress
{
    public const string Host = "tily.files";
    private const char Separator = '/';

    public static string BaseUrlOf(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath) ?? filePath;
        var segments = directory.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
        return $"https://{Host}/" + string.Concat(segments.Select(segment => Uri.EscapeDataString(segment) + Separator));
    }

    public static string? PathOf(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || !string.Equals(uri.Host, Host, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var segments = uri.AbsolutePath.Split(Separator, StringSplitOptions.RemoveEmptyEntries).Select(Uri.UnescapeDataString).ToList();
        if (segments.Count < 1 || segments.Any(segment => segment.Contains(Separator) || segment.Contains('\0') || segment is "." or ".."))
        {
            return null;
        }

        var candidate = Separator + string.Join(Separator, segments);
        return Path.IsPathFullyQualified(candidate) ? candidate : null;
    }
}
