using System.ComponentModel;
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
        void Report(string name, int erased)
        {
            if (clock.Elapsed - reported >= ProgressInterval)
            {
                reported = clock.Elapsed;
                progress(new WorktreePurgeProgressModel(name, files + erased, clock.Elapsed));
            }
        }

        while (NextSlot(root, attempted) is { } slot)
        {
            attempted.Add(slot);
            foreach (var item in Children(slot))
            {
                var name = Path.GetFileName(item);
                names.Add(name);
                files += Erase(item, erased => Report(name, erased));
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

    private static int Erase(string folder, Action<int> erased)
    {
        var total = CountFiles(folder);
        RemoveWithCommandPrompt(folder, () => erased(total - CountFiles(folder)));
        if (!Directory.Exists(folder))
        {
            return total;
        }

        var done = total - CountFiles(folder);
        LockedFolder.DeleteAll(folder, () => erased(++done));
        return total - CountFiles(folder);
    }

    private static void RemoveWithCommandPrompt(string folder, Action tick)
    {
        if (folder.Contains('%'))
        {
            return;
        }

        var info = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe"), $"/d /c rd /s /q \"{folder}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };
        try
        {
            using var process = Process.Start(info);
            while (process is not null && !process.WaitForExit(ProgressInterval))
            {
                tick();
            }
        }
        catch (Win32Exception)
        {
        }
    }

    private static int CountFiles(string folder)
    {
        try
        {
            var directory = new DirectoryInfo(folder);
            return directory.Exists
                ? directory.EnumerateFiles().Count() + directory.EnumerateDirectories().Where(child => !child.Attributes.HasFlag(FileAttributes.ReparsePoint)).Sum(child => CountFiles(child.FullName))
                : 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return 0;
        }
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
