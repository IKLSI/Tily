namespace Tily.Core.Context;

public static class CommandLocator
{
    public static readonly IReadOnlyList<string> FallbackDirectories = ["/opt/homebrew/bin", "/usr/local/bin", "/usr/bin", "/bin"];

    public static bool Exists(string command) => Find(command) is not null;

    public static bool Exists(string command, string? path) => Find(command, path) is not null;

    public static string? Find(string command) => Find(command, Environment.GetEnvironmentVariable("PATH"));

    public static string? Find(string command, string? path)
    {
        var name = command.Trim().Trim('"');
        if (name.Length == 0)
        {
            return null;
        }

        if (Path.IsPathRooted(name))
        {
            return File.Exists(name) ? name : null;
        }

        if (name.Contains(Path.DirectorySeparatorChar))
        {
            return name;
        }

        var directories = (path ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Concat(FallbackDirectories);
        return directories.Select(directory => FileIn(directory, name)).FirstOrDefault(candidate => candidate is not null);
    }

    private static string? FileIn(string directory, string file)
    {
        try
        {
            var candidate = Path.Combine(directory, file);
            return File.Exists(candidate) ? candidate : null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
