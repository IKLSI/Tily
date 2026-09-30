using System.Text.RegularExpressions;

namespace Tily.Core.Worktrees;

public static partial class DatabaseNames
{
    private const int SlugMaxLength = 40;
    private const int PostgreSqlMaxLength = 63;
    private const int SqlServerMaxLength = 128;
    private const string EmptySlug = "wt";

    public static string Slug(string value)
    {
        var slug = Underscores().Replace(Forbidden().Replace(value.ToLowerInvariant(), "_"), "_").Trim('_');
        if (slug.Length > SlugMaxLength)
        {
            slug = slug[..SlugMaxLength].Trim('_');
        }

        return slug.Length == 0 ? EmptySlug : slug;
    }

    public static string Target(DatabaseInfoModel info, string branch)
    {
        var target = $"{info.SourceDb}_{Slug(WorktreeTarget.Slug(branch))}";
        var maxLength = info.Provider == DatabaseProvider.PostgreSql ? PostgreSqlMaxLength : SqlServerMaxLength;
        return target.Length > maxLength ? target[..maxLength] : target;
    }

    public static string Require(string name) =>
        name.Length == 0 || name.Any(char.IsControl)
            ? throw new WorktreeException($"Nom de base invalide : « {name} ».", WorktreeSteps.Database)
            : name;

    public static string PostgreSqlIdentifier(string name) => $"\"{Require(name).Replace("\"", "\"\"")}\"";

    public static string SqlServerIdentifier(string name) => $"[{Require(name).Replace("]", "]]")}]";

    public static string Literal(string value) => $"'{Require(value).Replace("'", "''")}'";

    [GeneratedRegex("[^a-z0-9_]")]
    private static partial Regex Forbidden();

    [GeneratedRegex("_+")]
    private static partial Regex Underscores();
}
