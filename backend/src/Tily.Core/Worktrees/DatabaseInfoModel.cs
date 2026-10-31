namespace Tily.Core.Worktrees;

public enum DatabaseProvider
{
    PostgreSql
}

public sealed record DatabaseInfoModel(
    DatabaseProvider Provider,
    string SourceDb,
    string? Host,
    string? Port,
    string? User,
    string? Password,
    string ConfigFile,
    string? ConnKey)
{
    public string ProviderLabel => "PostgreSQL";

    public static DatabaseInfoModel? Parse(string connection, string configFile, string? connKey)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in connection.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=');
            if (separator >= 1)
            {
                values[part[..separator].Trim()] = part[(separator + 1)..].Trim();
            }
        }

        string? First(params string[] keys) => keys.Select(key => values.GetValueOrDefault(key)).FirstOrDefault(value => value is not null);

        var hasHost = values.ContainsKey("host");
        var hasServer = values.ContainsKey("server") || values.ContainsKey("data source");
        var hasCatalog = values.ContainsKey("initial catalog");
        var hasUsername = values.ContainsKey("username");
        DatabaseProvider? provider = hasHost || (hasServer && hasUsername && !hasCatalog)
            ? DatabaseProvider.PostgreSql
            : null;
        var sourceDb = values.GetValueOrDefault("database");
        if (provider is null || string.IsNullOrWhiteSpace(sourceDb))
        {
            return null;
        }

        return new DatabaseInfoModel(
            provider.Value,
            sourceDb,
            First("host", "server", "data source"),
            First("port"),
            First("username", "user id", "userid", "uid"),
            First("password", "pwd"),
            configFile,
            connKey);
    }
}
