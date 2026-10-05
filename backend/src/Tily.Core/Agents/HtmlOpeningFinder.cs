using System.Text.RegularExpressions;

namespace Tily.Core.Agents;

public sealed record HtmlOpeningModel(string Path, string? Target, bool Exists, bool Alone);

public static class HtmlOpeningFinder
{
    private const string HomePrefix = "~";
    private const string ChangeDirectory = "cd";
    private const string ShellPath = """(?:"(?<path>[^"]+)"|'(?<path>[^']+)'|(?<path>[^\s"'&|;<>`$-][^\s"'&|;<>`$]*))""";
    private const string Application = """(?:\s+-a\s+(?:"[^"]+"|'[^']+'|[^\s"'&|;<>`$-][^\s"'&|;<>`$]*))?""";

    private static readonly Regex Opening = new($"^open{Application}\\s+{ShellPath}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex HtmlExtension = new(@"\.html?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ChangedDirectory = new($"^cd\\s+{ShellPath}$", RegexOptions.CultureInvariant);
    private static readonly Regex ChangeDirectoryCommand = new(@"^cd(?:\s|$)", RegexOptions.CultureInvariant);

    public static HtmlOpeningModel? Find(string command, string? workingDirectory, string home)
    {
        var segments = ShellCommandSplitter.Split(command).Select(segment => segment.Trim()).Where(segment => segment.Length > 0).ToList();
        var directory = workingDirectory;
        foreach (var segment in segments)
        {
            var match = Opening.Match(segment);
            if (match.Success && HtmlExtension.IsMatch(match.Groups["path"].Value))
            {
                var path = match.Groups["path"].Value;
                var target = Resolve(path, directory, home);
                return new HtmlOpeningModel(path, target, target is not null && File.Exists(target), segments.Count == 1);
            }

            if (ChangeDirectoryCommand.IsMatch(segment))
            {
                directory = ChangedDirectoryOf(segment, directory, home);
            }
        }

        return null;
    }

    private static string? ChangedDirectoryOf(string segment, string? directory, string home)
    {
        if (segment == ChangeDirectory)
        {
            return home;
        }

        var match = ChangedDirectory.Match(segment);
        return match.Success ? Resolve(match.Groups["path"].Value, directory, home) : null;
    }

    private static string? Resolve(string path, string? directory, string home)
    {
        try
        {
            var expanded = path == HomePrefix
                ? home
                : path.StartsWith(HomePrefix + "/", StringComparison.Ordinal) ? home + path[1..] : path;
            if (!Path.IsPathRooted(expanded))
            {
                if (string.IsNullOrWhiteSpace(directory))
                {
                    return null;
                }

                expanded = Path.Combine(directory, expanded);
            }

            return Path.GetFullPath(expanded);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }
}
