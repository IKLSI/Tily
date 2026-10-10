namespace Tily.Core.Worktrees;

public static class LockedFolder
{
    private const int MaxRemainingFiles = 200;

    public static IReadOnlyList<string> DeleteAll(string folder, Action? fileDeleted = null)
    {
        var remaining = new List<string>();
        Delete(new DirectoryInfo(folder), remaining, fileDeleted ?? (() => { }));
        return remaining;
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
