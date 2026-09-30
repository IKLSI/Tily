using Microsoft.Win32;

namespace Dock.Core.Context;

public static class CommandLocator
{
    private const string AppPathsKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\";
    private const string ExecutableExtension = ".exe";
    private const string DefaultExtensions = ".COM;.EXE;.BAT;.CMD";

    public static bool Exists(string command) => Exists(command, Environment.GetEnvironmentVariable("PATH"), Environment.GetEnvironmentVariable("PATHEXT"), RegisteredApplication);

    public static bool Exists(string command, string? path, string? extensions, Func<string, bool> registered)
    {
        var name = command.Trim().Trim('"');
        if (name.Length == 0)
        {
            return false;
        }

        if (Path.IsPathRooted(name))
        {
            return File.Exists(name);
        }

        if (name.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0)
        {
            return true;
        }

        var candidates = Path.HasExtension(name)
            ? [name]
            : (extensions ?? DefaultExtensions).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(extension => name + extension).ToArray();
        var directories = (path ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(directory => directory.Trim('"'));
        return directories.Any(directory => candidates.Any(candidate => FileExistsIn(directory, candidate)))
            || registered(Path.HasExtension(name) ? name : name + ExecutableExtension);
    }

    private static bool FileExistsIn(string directory, string file)
    {
        try
        {
            return File.Exists(Path.Combine(directory, file));
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool RegisteredApplication(string executable)
    {
        using var user = Registry.CurrentUser.OpenSubKey(AppPathsKey + executable);
        using var machine = user is null ? Registry.LocalMachine.OpenSubKey(AppPathsKey + executable) : null;
        return user is not null || machine is not null;
    }
}
