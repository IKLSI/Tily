namespace Tily.Core.Context;

public static class EditorLocation
{
    private const string HomePrefix = "~/";
    private static readonly HashSet<string> GotoEditors = new(StringComparer.OrdinalIgnoreCase) { "code", "code-insiders", "codium", "cursor", "windsurf" };

    public static string Resolve(string? folder, string path)
    {
        if (path == HomePrefix[..1] || path.StartsWith(HomePrefix, StringComparison.Ordinal))
        {
            return Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path[1..].TrimStart('/')));
        }

        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        if (string.IsNullOrWhiteSpace(folder))
        {
            throw new InvalidOperationException($"Chemin relatif sans dossier courant : {path}");
        }

        return Path.GetFullPath(Path.Combine(folder, path));
    }

    public static string? ExistingExactly(string? folder, string path, Func<string, bool> exists)
    {
        if (!Path.IsPathRooted(path) && string.IsNullOrWhiteSpace(folder))
        {
            return null;
        }

        var resolved = Resolve(folder, path);
        return exists(resolved) ? resolved : null;
    }

    public static string? ResolveExisting(string? folder, string path, Func<string, bool> exists)
    {
        var resolved = Resolve(folder, path);
        if (exists(resolved))
        {
            return resolved;
        }

        var diffSide = path.Length > 2 && path[0] is 'a' or 'b' && path[1] == '/' && !Path.IsPathRooted(path);
        var withoutSide = diffSide ? Resolve(folder, path[2..]) : null;
        return withoutSide is not null && exists(withoutSide) ? withoutSide : AfterSpaces(folder, path).FirstOrDefault(exists);
    }

    private static IEnumerable<string> AfterSpaces(string? folder, string path) =>
        path.Select((character, index) => (character, index))
            .Where(pair => pair.character == ' ')
            .Select(pair => path[(pair.index + 1)..])
            .Where(suffix => suffix.Length > 0 && suffix[0] != ' ' && (Path.IsPathRooted(suffix) || !string.IsNullOrWhiteSpace(folder)))
            .Select(suffix => Resolve(folder, suffix));

    public static IReadOnlyList<string> Arguments(string editorCommand, string path, int line, int column) =>
        line > 0 && GotoEditors.Contains(Path.GetFileNameWithoutExtension(editorCommand.Trim().Trim('"')))
            ? ["-g", $"{path}:{line}:{Math.Max(column, 1)}"]
            : [path];
}
