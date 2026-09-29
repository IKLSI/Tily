namespace Dock.Core.Context;

public static class EditorLocation
{
    private static readonly HashSet<string> GotoEditors = new(StringComparer.OrdinalIgnoreCase) { "code", "code-insiders", "codium", "cursor", "windsurf" };

    public static string Resolve(string? folder, string path)
    {
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

    public static IReadOnlyList<string> Arguments(string editorCommand, string path, int line, int column) =>
        line > 0 && GotoEditors.Contains(Path.GetFileNameWithoutExtension(editorCommand.Trim().Trim('"')))
            ? ["-g", $"{path}:{line}:{Math.Max(column, 1)}"]
            : [path];
}
