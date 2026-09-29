using Dock.Core.Git;

namespace Dock.Core.Worktrees;

public static class WorktreeLister
{
    private const string BranchPrefix = "refs/heads/";

    public static IReadOnlyList<WorktreeModel> List(GitRepository repository) =>
        Parse(repository.Read("worktree", "list", "--porcelain", "-z"));

    public static IReadOnlyList<WorktreeModel> TryList(GitRepository repository)
    {
        try
        {
            return List(repository);
        }
        catch (GitCommandException)
        {
            return [];
        }
    }

    public static IReadOnlyList<WorktreeModel> Parse(string output)
    {
        var records = output.Split("\0\0", StringSplitOptions.RemoveEmptyEntries);
        return records
            .Select((record, index) => ParseRecord(record.Split('\0', StringSplitOptions.RemoveEmptyEntries), index == 0))
            .OfType<WorktreeModel>()
            .ToList();
    }

    public static string NormalizePath(string path) =>
        Path.GetFullPath(path.Replace('/', Path.DirectorySeparatorChar)).TrimEnd(Path.DirectorySeparatorChar);

    private static WorktreeModel? ParseRecord(IReadOnlyList<string> lines, bool isMain)
    {
        string? path = null;
        string? branch = null;
        string? head = null;
        var detached = false;
        var bare = false;
        var locked = false;
        var prunable = false;
        foreach (var line in lines)
        {
            var separator = line.IndexOf(' ');
            var key = separator < 0 ? line : line[..separator];
            var value = separator < 0 ? string.Empty : line[(separator + 1)..];
            switch (key)
            {
                case "worktree":
                    path = NormalizePath(value);
                    break;
                case "HEAD":
                    head = value;
                    break;
                case "branch":
                    branch = value.StartsWith(BranchPrefix, StringComparison.Ordinal) ? value[BranchPrefix.Length..] : value;
                    break;
                case "detached":
                    detached = true;
                    break;
                case "bare":
                    bare = true;
                    break;
                case "locked":
                    locked = true;
                    break;
                case "prunable":
                    prunable = true;
                    break;
            }
        }

        return path is null || bare ? null : new WorktreeModel(path, branch, head, isMain, detached, locked, prunable);
    }
}
