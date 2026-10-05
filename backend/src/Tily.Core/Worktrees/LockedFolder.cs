namespace Tily.Core.Worktrees;

public static class LockedFolder
{
    private const int MaxRemainingFiles = 200;
    private const int MaxRegisteredFiles = 1000;
    private static readonly HashSet<string> SkippedFolders = new(StringComparer.OrdinalIgnoreCase) { "node_modules", ".git" };

    public static IReadOnlyList<string> SampleFiles(string folder)
    {
        var files = new List<string>();
        Sample(new DirectoryInfo(folder), files);
        return files;
    }

    public static IReadOnlyList<string> DeleteAll(string folder, Action? fileDeleted = null)
    {
        var remaining = new List<string>();
        Delete(new DirectoryInfo(folder), remaining, fileDeleted ?? (() => { }));
        return remaining;
    }

    public static IReadOnlyList<string> LockingProcesses(IReadOnlyList<string> files) => [];

    private static void Sample(DirectoryInfo directory, List<string> files)
    {
        try
        {
            foreach (var file in directory.EnumerateFiles().TakeWhile(_ => files.Count < MaxRegisteredFiles))
            {
                files.Add(file.FullName);
            }

            foreach (var child in directory.EnumerateDirectories().Where(child => !SkippedFolders.Contains(child.Name) && !child.Attributes.HasFlag(FileAttributes.ReparsePoint)))
            {
                if (files.Count >= MaxRegisteredFiles)
                {
                    return;
                }

                Sample(child, files);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static void Delete(DirectoryInfo directory, List<string> remaining, Action fileDeleted)
    {
        try
        {
            foreach (var child in directory.EnumerateDirectories())
            {
                if (child.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    TryDelete(() => child.Delete(), child.FullName, remaining);
                }
                else
                {
                    Delete(child, remaining, fileDeleted);
                }
            }

            foreach (var file in directory.EnumerateFiles())
            {
                TryDelete(() =>
                {
                    file.Attributes = FileAttributes.Normal;
                    file.Delete();
                    fileDeleted();
                }, file.FullName, remaining);
            }

            directory.Delete();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static void TryDelete(Action delete, string path, List<string> remaining)
    {
        try
        {
            delete();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            if (remaining.Count < MaxRemainingFiles)
            {
                remaining.Add(path);
            }
        }
    }
}
