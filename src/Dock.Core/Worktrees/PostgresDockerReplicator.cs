using System.Text.RegularExpressions;

namespace Dock.Core.Worktrees;

public sealed partial class PostgresDockerReplicator : IDatabaseReplicator
{
    private const string Docker = "docker";
    private const string DefaultUser = "postgres";
    private const string AdminDatabase = "postgres";
    private const string PasswordVariable = "PGPASSWORD";
    private const string EnvironmentFile = ".env";
    private const string ServerFolder = "server";
    private const string ContainerSuffix = "-pg";
    private const string BusySource = "being accessed";
    private static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(2);

    public DatabaseProvider Provider => DatabaseProvider.PostgreSql;

    public DatabaseOutcomeModel Replicate(DatabaseInfoModel info, string targetDb, DatabaseContextModel context)
    {
        var container = RunningContainer(info, context, "réplication ignorée");
        if (container.Failure is { } failure)
        {
            return failure;
        }

        var name = container.Name!;
        if (!Exists(name, info, info.SourceDb))
        {
            return new DatabaseOutcomeModel(false, $"Base source « {info.SourceDb} » absente dans « {name} » : réplication ignorée.");
        }

        if (Exists(name, info, targetDb))
        {
            return new DatabaseOutcomeModel(true, $"Base « {targetDb} » déjà présente : réutilisée.");
        }

        var terminate = $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = {DatabaseNames.Literal(info.SourceDb)} AND pid <> pg_backend_pid();";
        var create = $"CREATE DATABASE {DatabaseNames.PostgreSqlIdentifier(targetDb)} WITH TEMPLATE {DatabaseNames.PostgreSqlIdentifier(info.SourceDb)};";
        Psql(name, info, terminate);
        var created = Psql(name, info, create);
        if (!created.Succeeded && created.Output.Contains(BusySource, StringComparison.OrdinalIgnoreCase))
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));
            Psql(name, info, terminate);
            created = Psql(name, info, create);
        }

        return Exists(name, info, targetDb)
            ? new DatabaseOutcomeModel(true, $"Base répliquée : {info.SourceDb} → {targetDb} (PostgreSQL, conteneur {name}).")
            : new DatabaseOutcomeModel(false, $"Échec de CREATE DATABASE PostgreSQL : {created.Output}");
    }

    public DatabaseOutcomeModel Drop(DatabaseInfoModel info, string targetDb, DatabaseContextModel context)
    {
        var container = RunningContainer(info, context, "suppression de la base ignorée");
        if (container.Failure is { } failure)
        {
            return failure;
        }

        var dropped = Psql(container.Name!, info, $"DROP DATABASE IF EXISTS {DatabaseNames.PostgreSqlIdentifier(targetDb)} WITH (FORCE);");
        return dropped.Succeeded
            ? new DatabaseOutcomeModel(true, $"Base « {targetDb} » supprimée.")
            : new DatabaseOutcomeModel(false, $"Échec de DROP DATABASE PostgreSQL : {dropped.Output}");
    }

    private static (string? Name, DatabaseOutcomeModel? Failure) RunningContainer(DatabaseInfoModel info, DatabaseContextModel context, string consequence)
    {
        var name = ContainerOf(info, context);
        if (name is null)
        {
            return (null, new DatabaseOutcomeModel(false, $"Conteneur PostgreSQL introuvable : {consequence}."));
        }

        if (Names(["ps", "--filter", $"name=^{name}$", "--format", "{{.Names}}"]).Contains(name))
        {
            return (name, null);
        }

        if (!ContainerExists(name))
        {
            return (null, new DatabaseOutcomeModel(false, $"Conteneur « {name} » inexistant : {consequence}."));
        }

        ExternalCommand.Run(Docker, ["start", name]);
        Thread.Sleep(StartDelay);
        return (name, null);
    }

    private static string? ContainerOf(DatabaseInfoModel info, DatabaseContextModel context)
    {
        var variables = EnvironmentOf(context.Root);
        if (variables.GetValueOrDefault("POSTGRES_CONTAINER") is { Length: > 0 } configured && ContainerExists(configured))
        {
            return configured;
        }

        var projectName = variables.GetValueOrDefault("PROJECT_NAME") ?? FirstServerFolder(context.Root);
        if (projectName is not null && ContainerExists($"{projectName.ToLowerInvariant()}{ContainerSuffix}"))
        {
            return $"{projectName.ToLowerInvariant()}{ContainerSuffix}";
        }

        if (info.Port is { Length: > 0 } port && Names(["ps", "-a", "--filter", $"publish={port}", "--format", "{{.Names}}"]).FirstOrDefault() is { } published)
        {
            return published;
        }

        var fallback = $"{context.Project.ToLowerInvariant()}{ContainerSuffix}";
        return ContainerExists(fallback) ? fallback : null;
    }

    private static Dictionary<string, string> EnvironmentOf(string root)
    {
        var file = Path.Combine(root, EnvironmentFile);
        var variables = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!File.Exists(file) || WorktreeFiles.Read(file) is not { } content)
        {
            return variables;
        }

        foreach (Match match in Variable().Matches(content))
        {
            variables[match.Groups[1].Value] = match.Groups[2].Value.Trim('"').Trim('\'');
        }

        return variables;
    }

    private static string? FirstServerFolder(string root)
    {
        var server = Path.Combine(root, ServerFolder);
        return Directory.Exists(server) ? Directory.EnumerateDirectories(server).Select(Path.GetFileName).Order(StringComparer.OrdinalIgnoreCase).FirstOrDefault() : null;
    }

    private static bool ContainerExists(string name) => Names(["ps", "-a", "--filter", $"name=^{name}$", "--format", "{{.Names}}"]).Contains(name);

    private static IReadOnlyList<string> Names(string[] arguments)
    {
        var output = ExternalCommand.Run(Docker, arguments);
        return output.Succeeded ? output.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) : [];
    }

    private static bool Exists(string container, DatabaseInfoModel info, string database) =>
        Psql(container, info, $"SELECT 1 FROM pg_database WHERE datname = {DatabaseNames.Literal(database)};", true).Output.Trim() == "1";

    private static ExternalOutputModel Psql(string container, DatabaseInfoModel info, string sql, bool tuples = false)
    {
        var user = string.IsNullOrWhiteSpace(info.User) ? DefaultUser : info.User;
        List<string> arguments = ["exec", "-i", "-e", PasswordVariable, container, "psql", "-U", user, "-d", AdminDatabase, "-v", "ON_ERROR_STOP=1"];
        if (tuples)
        {
            arguments.Add("-tA");
        }

        return ExternalCommand.Run(Docker, arguments, sql, new Dictionary<string, string> { [PasswordVariable] = info.Password ?? string.Empty });
    }

    [GeneratedRegex(@"^\s*([A-Z_][A-Z0-9_]*)\s*=\s*(.+?)\s*$", RegexOptions.Multiline)]
    private static partial Regex Variable();
}
