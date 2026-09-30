namespace Tily.Core.Shell;

public static class DroppedPaths
{
    private const string CmdShellId = "cmd";
    private const string GitBashShellId = "gitbash";
    private const string Separator = " ";

    private static readonly char[] PowerShellQuotes = ['\'', '‘', '’', '‚', '‛'];
    private static readonly char[] PowerShellSpecials = [.. PowerShellQuotes, '"', '“', '”', '„', '`', '$', '&', '(', ')', '{', '}', '[', ']', ';', ',', '@', '#', '|', '<', '>'];
    private static readonly char[] CmdSpecials = ['&', '(', ')', '[', ']', '{', '}', '^', '=', ';', '!', '\'', '+', ',', '`', '~', '%', '|', '<', '>'];
    private static readonly char[] BashSpecials = ['\\', '\'', '"', '`', '$', '&', '(', ')', '{', '}', '[', ']', ';', '|', '<', '>', '*', '?', '!', '#', '~'];

    public static string Format(IEnumerable<string> paths, string shellId) =>
        string.Concat(paths.Where(path => !string.IsNullOrWhiteSpace(path)).Select(path => Quote(path, shellId) + Separator));

    public static string Quote(string path, string shellId) => shellId switch
    {
        CmdShellId => NeedsQuotes(path, CmdSpecials) ? $"\"{path}\"" : path,
        GitBashShellId => NeedsQuotes(path, BashSpecials) ? $"'{path.Replace("'", "'\\''")}'" : path,
        _ => NeedsQuotes(path, PowerShellSpecials) ? $"'{DoublePowerShellQuotes(path)}'" : path,
    };

    private static bool NeedsQuotes(string path, char[] specials) => path.Any(char.IsWhiteSpace) || path.IndexOfAny(specials) >= 0;

    private static string DoublePowerShellQuotes(string path) =>
        string.Concat(path.Select(character => PowerShellQuotes.Contains(character) ? $"{character}{character}" : character.ToString()));
}
