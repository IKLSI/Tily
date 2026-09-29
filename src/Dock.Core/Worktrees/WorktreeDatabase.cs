using Microsoft.Data.SqlClient;

namespace Dock.Core.Worktrees;

public sealed record ReplicatedDatabaseModel(DatabaseInfoModel? Info, string Reason);

public sealed class WorktreeDatabase
{
    private readonly IReadOnlyList<IDatabaseReplicator> _replicators;

    public WorktreeDatabase(IReadOnlyList<IDatabaseReplicator>? replicators = null) =>
        _replicators = replicators ?? [new PostgresDockerReplicator(), new SqlServerReplicator()];

    public WorktreeStepModel Replicate(string path, string branch, string project)
    {
        var info = DatabaseConfigLocator.Locate(path);
        if (info is null)
        {
            return WorktreeSteps.Warning(WorktreeSteps.Database, "Aucune configuration de base détectée : réplication ignorée.");
        }

        var target = DatabaseNames.Target(info, branch);
        var outcome = Run(() => ReplicatorFor(info).Replicate(info, target, new DatabaseContextModel(path, project)), "Réplication de la base ignorée");
        if (!outcome.Succeeded)
        {
            return WorktreeSteps.Warning(WorktreeSteps.Database, outcome.Message);
        }

        ConnectionRewriter.Rewrite(info, target, path);
        return WorktreeSteps.Ok(WorktreeSteps.Database, outcome.Message);
    }

    public static ReplicatedDatabaseModel Replicated(string worktreePath, string mainRoot)
    {
        var info = DatabaseConfigLocator.Locate(worktreePath);
        if (info is null)
        {
            return new ReplicatedDatabaseModel(null, "Aucune configuration de base détectée.");
        }

        var main = DatabaseConfigLocator.Locate(mainRoot);
        if (main is null)
        {
            return new ReplicatedDatabaseModel(null, "Base du dépôt principal indéterminée : suppression de la base ignorée (garde-fou).");
        }

        return string.Equals(main.SourceDb, info.SourceDb, StringComparison.OrdinalIgnoreCase)
            ? new ReplicatedDatabaseModel(null, $"Base identique au dépôt principal ({info.SourceDb}) : suppression ignorée (garde-fou).")
            : new ReplicatedDatabaseModel(info, $"Base répliquée détectée : {info.SourceDb} ({info.ProviderLabel}).");
    }

    public WorktreeStepModel Drop(DatabaseInfoModel info, string mainRoot, string project)
    {
        var outcome = Run(() => ReplicatorFor(info).Drop(info, info.SourceDb, new DatabaseContextModel(mainRoot, project)), "Suppression de la base ignorée");
        return outcome.Succeeded ? WorktreeSteps.Ok(WorktreeSteps.Database, outcome.Message) : WorktreeSteps.Warning(WorktreeSteps.Database, outcome.Message);
    }

    private IDatabaseReplicator ReplicatorFor(DatabaseInfoModel info) =>
        _replicators.FirstOrDefault(replicator => replicator.Provider == info.Provider)
        ?? throw new WorktreeException($"Fournisseur de base non pris en charge : {info.ProviderLabel}.", WorktreeSteps.Database);

    private static DatabaseOutcomeModel Run(Func<DatabaseOutcomeModel> action, string failure)
    {
        try
        {
            return action();
        }
        catch (Exception exception) when (exception is WorktreeException or SqlException or IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            return new DatabaseOutcomeModel(false, $"{failure} : {exception.Message}");
        }
    }
}
