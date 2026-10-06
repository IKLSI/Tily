using System.ComponentModel;
using Tily.Core.Processes;

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

        try
        {
            var result = ProcessRunner.Run(new ProcessRequestModel(ScriptExecutable, Arguments(path), Timeout, InheritsInput: true));
            if (result.TimedOut)
            {
                throw new InvalidOperationException($"Le Finder n’a pas répondu : « {Path.GetFileName(path)} » n’a pas été placé dans la corbeille.");
            }

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"Impossible de placer « {Path.GetFileName(path)} » dans la corbeille : {result.Error.Trim()}");
            }
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException($"Impossible de placer « {Path.GetFileName(path)} » dans la corbeille : {exception.Message}");
        }
    }

    public static IReadOnlyList<string> Arguments(string path) => ["-e", FinderDeleteScript, Path.GetFullPath(path)];
}
