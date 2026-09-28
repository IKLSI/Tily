namespace Dock.Core.Shell;

public static class DroppedPaths
{
    private const string CmdShellId = "cmd";
    private const string GitBashShellId = "gitbash";
    private const string Separator = " ";

    private static readonly char[] PowerShellSpecials = [' ', '\'', '"', '`', '$', '&', '(', ')', '{', '}', '[', ']', ';', ',', '@', '#', '|', '<', '>'];
    private static readonly char[] CmdSpecials = [' ', '&', '(', ')', '[', ']', '{', '}', '^', '=', ';', '!', '\'', '+', ',', '`', '~', '%', '|', '<', '>'];
    private static readonly char[] BashSpecials = [' ', '\\', '\'', '"', '`', '$', '&', '(', ')', '{', '}', '[', ']', ';', '|', '<', '>', '*', '?', '!', '#', '~'];

    public static string Format(IEnumerable<string> paths, string shellId) =>
        string.Concat(paths.Where(path => !string.IsNullOrWhiteSpace(path)).Select(path => Quote(path, shellId) + Separator));

    public static string Quote(string path, string shellId) => shellId switch
    {
        CmdShellId => path.IndexOfAny(CmdSpecials) < 0 ? path : $"\"{path}\"",
        GitBashShellId => path.IndexOfAny(BashSpecials) < 0 ? path : $"'{path.Replace("'", "'\\''")}'",
        _ => path.IndexOfAny(PowerShellSpecials) < 0 ? path : $"'{path.Replace("'", "''")}'",
    };
}
