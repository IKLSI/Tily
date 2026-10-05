using System.Diagnostics;

namespace Tily.Core.Worktrees;

public sealed record WorktreePurgeProgressModel(string Name, int Files, TimeSpan Elapsed);

public sealed record WorktreePurgeModel(IReadOnlyList<string> Names, int Files, TimeSpan Elapsed, IReadOnlyList<string> Remaining);

public static class WorktreeTrash
{
    public const string FolderName = ".tily-corbeille";
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromSeconds(1);

    public static string RootFor(string target) => Path.Combine(Path.GetDirectoryName(target.TrimEnd(Path.DirectorySeparatorChar)) ?? target, FolderName);

    public static bool TryMove(string target)
    {
        var root = RootFor(target);
        var slot = Path.Combine(root, Guid.NewGuid().ToString("N")[..8]);
        var trashed = Path.Combine(slot, Path.GetFileName(target.TrimEnd(Path.DirectorySeparatorChar)));
        try
        {
            Directory.CreateDirectory(slot);
            File.SetAttributes(root, File.GetAttributes(root) | FileAttributes.Hidden);
            Directory.Move(target, trashed);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            DeleteIfEmpty(slot);
            DeleteIfEmpty(root);
            return false;
        }
    }

    public static WorktreePurgeModel Purge(string root, Action<WorktreePurgeProgressModel> progress)
    {
        var clock = Stopwatch.StartNew();
        var reported = TimeSpan.Zero;
        var attempted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var names = new List<string>();
        var remaining = new List<string>();
        var files = 0;
        while (NextSlot(root, attempted) is { } slot)
        {
            attempted.Add(slot);
            foreach (var item in Children(slot))
            {
                var name = Path.GetFileName(item);
                names.Add(name);
                LockedFolder.DeleteAll(item, () =>
                {
                    files++;
                    if (clock.Elapsed - reported >= ProgressInterval)
                    {
                        reported = clock.Elapsed;
                        progress(new WorktreePurgeProgressModel(name, files, clock.Elapsed));
                    }
                });
                if (Directory.Exists(item))
                {
                    remaining.Add(item);
                }
            }

            DeleteIfEmpty(slot);
        }

        DeleteIfEmpty(root);
        return new WorktreePurgeModel(names, files, clock.Elapsed, remaining);
    }

    private static string? NextSlot(string root, HashSet<string> attempted) => Children(root).FirstOrDefault(slot => !attempted.Contains(slot));

    private static IReadOnlyList<string> Children(string folder)
    {
        try
        {
            return Directory.Exists(folder) ? Directory.GetDirectories(folder) : [];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static void DeleteIfEmpty(string folder)
    {
        try
        {
            if (Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any())
            {
                Directory.Delete(folder);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
