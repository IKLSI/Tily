using System.Text.RegularExpressions;

namespace Tily.Core.Worktrees;

public static partial class ConnectionRewriter
{
    private const string EnvironmentFile = ".env";

    public static IReadOnlyList<string> Rewrite(DatabaseInfoModel info, string targetDb, string root)
    {
        var changed = new List<string>();
        if (File.Exists(info.ConfigFile) && WorktreeFiles.Read(info.ConfigFile) is { } content)
        {
            var updated = Regex.Replace(content, DatabasePattern(info.SourceDb), match => match.Groups[1].Value + targetDb, RegexOptions.IgnoreCase);

            if (updated != content)
            {
                WorktreeFiles.Write(info.ConfigFile, updated);
                changed.Add(info.ConfigFile);
            }
        }

        var environment = Path.Combine(root, EnvironmentFile);
        if (File.Exists(environment) && WorktreeFiles.Read(environment) is { } variables && PostgresDatabase().IsMatch(variables))
        {
            var updated = PostgresDatabase().Replace(variables, match => match.Groups[1].Value + targetDb);
            if (updated != variables)
            {
                WorktreeFiles.Write(environment, updated);
                changed.Add(environment);
            }
        }

        return changed;
    }

    private static string DatabasePattern(string sourceDb) =>
        $"""(Database\s*=\s*){Regex.Escape(sourceDb)}(?=[;"'\s\\]|$)""";

    [GeneratedRegex(@"^(\s*POSTGRES_DB\s*=)[^\r\n]*", RegexOptions.Multiline)]
    private static partial Regex PostgresDatabase();
}
