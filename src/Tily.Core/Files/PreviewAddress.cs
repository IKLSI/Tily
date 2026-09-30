namespace Tily.Core.Files;

public static class PreviewAddress
{
    public const string Host = "tily.files";
    public const string Filter = "https://" + Host + "/*";
    private const string UncSegment = "unc";

    public static string BaseUrlOf(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath) ?? filePath;
        var segments = directory.StartsWith(@"\\", StringComparison.Ordinal)
            ? new[] { UncSegment }.Concat(directory[2..].Split('\\', StringSplitOptions.RemoveEmptyEntries))
            : directory.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        return $"https://{Host}/" + string.Concat(segments.Select(segment => Uri.EscapeDataString(segment) + "/"));
    }

    public static string? PathOf(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || !string.Equals(uri.Host, Host, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.UnescapeDataString).ToList();
        if (segments.Count < 2 || segments.Any(segment => segment.Contains('\\') || segment is "." or ".."))
        {
            return null;
        }

        var candidate = segments[0] == UncSegment ? @"\\" + string.Join('\\', segments.Skip(1)) : string.Join('\\', segments);
        return Path.IsPathFullyQualified(candidate) ? candidate : null;
    }
}
