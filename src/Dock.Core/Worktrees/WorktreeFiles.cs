using System.Text;

namespace Dock.Core.Worktrees;

public static class WorktreeFiles
{
    private const int DefaultDepth = 6;
    private static readonly HashSet<string> SkippedFolders = new(StringComparer.OrdinalIgnoreCase) { "bin", "obj", "node_modules", ".git", ".vs", "dist", "packages" };
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);
    private static readonly UTF8Encoding Utf8WithBom = new(true);

    public static IReadOnlyList<string> Find(string root, Func<string, bool> matches, int depth = DefaultDepth)
    {
        var found = new List<string>();
        Collect(root, matches, depth, found);
        return found;
    }

    public static string? Read(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static void Write(string path, string content)
    {
        var bytes = File.ReadAllBytes(path);
        var bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        File.WriteAllText(path, content, bom ? Utf8WithBom : Utf8WithoutBom);
    }

    private static void Collect(string directory, Func<string, bool> matches, int depth, List<string> found)
    {
        if (depth < 0)
        {
            return;
        }

        try
        {
            foreach (var file in Directory.EnumerateFiles(directory).Where(file => matches(Path.GetFileName(file))).Order(StringComparer.OrdinalIgnoreCase))
            {
                found.Add(file);
            }

            foreach (var child in Directory.EnumerateDirectories(directory).Where(child => !SkippedFolders.Contains(Path.GetFileName(child))).Order(StringComparer.OrdinalIgnoreCase))
            {
                Collect(child, matches, depth - 1, found);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
