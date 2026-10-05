using System.ComponentModel;
using System.Diagnostics;

namespace Tily.Core.Files;

public static class RecycleBin
{
    public const string ScriptExecutable = "/usr/bin/osascript";
    public const string FinderDeleteScript = "on run argv\ntell application \"Finder\" to delete (POSIX file (item 1 of argv) as alias)\nend run";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    public static void Send(string path)
    {
        FileExplorer.RequireFullPath(path);
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            throw new InvalidOperationException($"L’élément n’existe plus : {path}");
        }

        var start = new ProcessStartInfo(ScriptExecutable) { UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true, CreateNoWindow = true };
        foreach (var argument in Arguments(path))
        {
            start.ArgumentList.Add(argument);
        }

        try
        {
            using var process = Process.Start(start) ?? throw new InvalidOperationException("osascript n’a pas pu être lancé.");
            var error = process.StandardError.ReadToEndAsync();
            _ = process.StandardOutput.ReadToEndAsync();
            if (!process.WaitForExit(Timeout))
            {
                process.Kill(true);
                throw new InvalidOperationException($"Le Finder n’a pas répondu : « {Path.GetFileName(path)} » n’a pas été placé dans la corbeille.");
            }

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Impossible de placer « {Path.GetFileName(path)} » dans la corbeille : {error.Result.Trim()}");
            }
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException($"Impossible de placer « {Path.GetFileName(path)} » dans la corbeille : {exception.Message}");
        }
    }

    public static IReadOnlyList<string> Arguments(string path) => ["-e", FinderDeleteScript, Path.GetFullPath(path)];
}
