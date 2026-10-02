namespace Tily.Core.Worktrees;

public static class RepositoryFinder
{
    public const int MaxDepth = 3;

    private const string GitEntry = ".git";
    private static readonly HashSet<string> SkippedFolders = new(StringComparer.OrdinalIgnoreCase) { "node_modules", "bin", "obj", "dist", "packages" };

    public static IReadOnlyList<string> Find(string folder, int maxDepth = MaxDepth)
    {
        var found = new List<string>();
        if (!Directory.Exists(folder))
        {
            return found;
        }

        List<string> level = [WorktreeLister.NormalizePath(folder)];
        for (var depth = 0; depth <= maxDepth && level.Count > 0; depth++)
        {
            var next = new List<string>();
            foreach (var current in level)
            {
                if (IsRepository(current))
                {
                    found.Add(current);
                }
                else if (depth < maxDepth)
                {
                    next.AddRange(ChildrenOf(current));
                }
            }

            level = next;
        }

        return found;
    }

    public static bool IsRepository(string folder) =>
        Directory.Exists(Path.Combine(folder, GitEntry)) || File.Exists(Path.Combine(folder, GitEntry));

    private static IReadOnlyList<string> ChildrenOf(string folder)
    {
        try
        {
            return new DirectoryInfo(folder)
                .EnumerateDirectories()
                .Where(directory => !directory.Attributes.HasFlag(FileAttributes.Hidden) && !directory.Name.StartsWith('.') && !SkippedFolders.Contains(directory.Name))
                .OrderBy(directory => directory.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(directory => directory.FullName)
                .ToList();
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
        {
            return [];
        }
    }
}
