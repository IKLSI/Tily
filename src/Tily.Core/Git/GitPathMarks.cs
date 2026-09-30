namespace Tily.Core.Git;

public sealed record GitPathMarkModel(string Path, GitChangeKind Kind, bool Conflicted);

public static class GitPathMarks
{
    public static IReadOnlyList<GitPathMarkModel> From(string root, GitStatusModel status)
    {
        var marks = new Dictionary<string, GitPathMarkModel>(StringComparer.OrdinalIgnoreCase);
        foreach (var change in status.Staged.Concat(status.Unstaged))
        {
            var path = FullPath(root, change.Path);
            if (!marks.TryGetValue(path, out var existing) || Precedence(change.Kind) > Precedence(existing.Kind))
            {
                marks[path] = new GitPathMarkModel(path, change.Kind, false);
            }
        }

        foreach (var conflict in status.Conflicts)
        {
            var path = FullPath(root, conflict.Path);
            marks[path] = new GitPathMarkModel(path, GitChangeKind.Modified, true);
        }

        return marks.Values.ToList();
    }

    public static string DisplayRoot(string folder, string prefix, string fallback)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
        foreach (var segment in prefix.Split('/', StringSplitOptions.RemoveEmptyEntries).Reverse())
        {
            var parent = Path.GetDirectoryName(root);
            if (parent is null || !string.Equals(Path.GetFileName(root), segment, StringComparison.OrdinalIgnoreCase))
            {
                return fallback;
            }

            root = parent;
        }

        return root;
    }

    public static string DisplayRootFrom(GitRunner runner, string folder, string root)
    {
        try
        {
            var output = runner.Run(folder, ["rev-parse", "--show-prefix"]);
            return output.Succeeded ? DisplayRoot(folder, output.Output.Trim(), root) : root;
        }
        catch (Exception exception) when (exception is GitCommandException or System.ComponentModel.Win32Exception or IOException or InvalidOperationException)
        {
            return root;
        }
    }

    private static int Precedence(GitChangeKind kind) => kind switch
    {
        GitChangeKind.Untracked or GitChangeKind.Deleted => 2,
        GitChangeKind.Added or GitChangeKind.Renamed or GitChangeKind.Copied => 1,
        _ => 0
    };

    private static string FullPath(string root, string relativePath) =>
        Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
}
