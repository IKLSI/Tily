namespace Dock.Core.Files;

public enum PreviewLinkKind
{
    Web,
    Preview,
    Editor
}

public sealed record PreviewLinkModel(PreviewLinkKind Kind, string Target, string? Anchor);

public static class PreviewLinks
{
    private static readonly string[] FolderReadmes = ["README.md", "README.markdown", "README.txt", "README"];

    public static PreviewLinkModel Resolve(string currentFile, string href)
    {
        FileExplorer.RequireFullPath(currentFile);
        var trimmed = href.Trim();
        if (trimmed.Length == 0)
        {
            throw new InvalidOperationException("Lien vide.");
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute) && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            return new PreviewLinkModel(PreviewLinkKind.Web, absolute.AbsoluteUri, null);
        }

        var hash = trimmed.IndexOf('#');
        var location = hash < 0 ? trimmed : trimmed[..hash];
        var anchor = hash < 0 || hash == trimmed.Length - 1 ? null : Uri.UnescapeDataString(trimmed[(hash + 1)..]);
        var target = location.Length == 0 ? currentFile : TargetOf(currentFile, location, trimmed);
        return FileLink(Directory.Exists(target) ? ReadmeOf(target) : target, anchor);
    }

    private static string TargetOf(string currentFile, string location, string href)
    {
        if (Uri.TryCreate(location, UriKind.Absolute, out var uri))
        {
            return uri.IsFile ? uri.LocalPath : throw new InvalidOperationException($"Lien non pris en charge : {href}");
        }

        var query = location.IndexOf('?');
        var relative = Uri.UnescapeDataString(query < 0 ? location : location[..query]).Replace('/', '\\');
        var directory = Path.GetDirectoryName(currentFile) ?? currentFile;
        var origin = relative.StartsWith('\\') ? RepositoryRoot(directory) : directory;
        return Path.GetFullPath(Path.Combine(origin, relative.TrimStart('\\')));
    }

    private static PreviewLinkModel FileLink(string target, string? anchor)
    {
        if (!File.Exists(target))
        {
            throw new InvalidOperationException($"Fichier introuvable : {target}");
        }

        return new PreviewLinkModel(PreviewTypes.KindOf(target) is null ? PreviewLinkKind.Editor : PreviewLinkKind.Preview, target, anchor);
    }

    private static string ReadmeOf(string directory) =>
        FolderReadmes.Select(name => Path.Combine(directory, name)).FirstOrDefault(File.Exists)
        ?? throw new InvalidOperationException($"« {Path.GetFileName(directory)} » est un dossier sans README : aperçu impossible.");

    private static string RepositoryRoot(string directory)
    {
        for (var current = new DirectoryInfo(directory); current is not null; current = current.Parent)
        {
            var marker = Path.Combine(current.FullName, ".git");
            if (Directory.Exists(marker) || File.Exists(marker))
            {
                return current.FullName;
            }
        }

        return Path.GetPathRoot(directory) ?? directory;
    }
}
