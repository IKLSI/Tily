namespace Tily.Core.Shell;

public static class DroppedPaths
{
    private const string Separator = " ";

    private static readonly char[] ShellSpecials = ['\\','\'', '"', '`', '$', '&', '(', ')', '{', '}', '[', ']', ';', '|', '<', '>', '*', '?', '!', '#', '~', '^', '=', '%'];

    public static string Format(IEnumerable<string> paths, string shellId) =>
        string.Concat(paths.Where(path => !string.IsNullOrWhiteSpace(path)).Select(path => Quote(path, shellId) + Separator));

    public static string Quote(string path, string shellId) =>
        path.Any(char.IsWhiteSpace) || path.IndexOfAny(ShellSpecials) >= 0 ? $"'{path.Replace("'", "'\\''")}'" : path;
}
