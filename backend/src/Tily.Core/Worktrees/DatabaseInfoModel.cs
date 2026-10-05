namespace Tily.Core.Worktrees;

public enum DatabaseProvider
{
    PostgreSql,
    SqlServer
}

public sealed record DatabaseInfoModel(
    DatabaseProvider Provider,
    string SourceDb,
    string? Host,
    string? Port,
    string? User,
    string? Password,
    bool IntegratedSecurity,
    string ConfigFile,
    string? ConnKey)
{
    public string ProviderLabel => Provider == DatabaseProvider.PostgreSql ? "PostgreSQL" : "SQL Server";

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
            : hasCatalog || hasServer ? DatabaseProvider.SqlServer : null;
        var sourceDb = provider == DatabaseProvider.SqlServer && hasCatalog ? values["initial catalog"] : values.GetValueOrDefault("database");
        if (provider is null || string.IsNullOrWhiteSpace(sourceDb))
        {
            return null;
        }

        var integrated = First("integrated security", "trusted_connection") is { } security
            && (security.Equals("true", StringComparison.OrdinalIgnoreCase) || security.Equals("sspi", StringComparison.OrdinalIgnoreCase) || security.Equals("yes", StringComparison.OrdinalIgnoreCase));
        return new DatabaseInfoModel(
            provider.Value,
            sourceDb,
            First("host", "server", "data source"),
            First("port"),
            First("username", "user id", "userid", "uid"),
            First("password", "pwd"),
            integrated,
            configFile,
            connKey);
    }
}
