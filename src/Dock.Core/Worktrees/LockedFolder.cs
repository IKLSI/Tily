using System.Text;
using Dock.Core.Native;

namespace Dock.Core.Worktrees;

public static class LockedFolder
{
    private const int MaxRemainingFiles = 200;
    private const int MaxRegisteredFiles = 1000;
    private const string ProbeSuffix = ".dock-verrou-";
    private static readonly HashSet<string> SkippedFolders = new(StringComparer.OrdinalIgnoreCase) { "node_modules", ".git" };

    public static bool CanMove(string folder)
    {
        var probe = folder.TrimEnd(Path.DirectorySeparatorChar) + ProbeSuffix + Guid.NewGuid().ToString("N")[..8];
        try
        {
            Directory.Move(folder, probe);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }

        Directory.Move(probe, folder);
        return true;
    }

    public static IReadOnlyList<string> SampleFiles(string folder)
    {
        var files = new List<string>();
        Sample(new DirectoryInfo(folder), files);
        return files;
    }

    public static IReadOnlyList<string> DeleteAll(string folder)
    {
        var remaining = new List<string>();
        Delete(new DirectoryInfo(folder), remaining);
        return remaining;
    }

    public static IReadOnlyList<string> LockingProcesses(IReadOnlyList<string> files)
    {
        var registered = files.Where(File.Exists).Take(MaxRegisteredFiles).ToArray();
        if (registered.Length == 0)
        {
            return [];
        }

        var key = new StringBuilder(RestartManagerApi.SessionKeyLength);
        if (RestartManagerApi.RmStartSession(out var session, 0, key) != 0)
        {
            return [];
        }

        try
        {
            if (RestartManagerApi.RmRegisterResources(session, (uint)registered.Length, registered, 0, null, 0, null) != 0)
            {
                return [];
            }

            uint count = 0;
            uint reasons = 0;
            var result = RestartManagerApi.RmGetList(session, out var needed, ref count, null, ref reasons);
            if (result != RestartManagerApi.ErrorMoreData || needed == 0)
            {
                return [];
            }

            var processes = new RestartManagerApi.ProcessInfo[needed];
            count = needed;
            return RestartManagerApi.RmGetList(session, out _, ref count, processes, ref reasons) == 0
                ? processes.Take((int)count).Select(process => $"{process.ApplicationName} ({process.Process.ProcessId})").Distinct().ToList()
                : [];
        }
        finally
        {
            RestartManagerApi.RmEndSession(session);
        }
    }

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

    private static void Delete(DirectoryInfo directory, List<string> remaining)
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
                    Delete(child, remaining);
                }
            }

            foreach (var file in directory.EnumerateFiles())
            {
                TryDelete(() =>
                {
                    file.Attributes = FileAttributes.Normal;
                    file.Delete();
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
