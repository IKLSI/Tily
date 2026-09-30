namespace Tily.Core.Worktrees;

public sealed record DatabaseContextModel(string Root, string Project);

public sealed record DatabaseOutcomeModel(bool Succeeded, string Message);

public interface IDatabaseReplicator
{
    DatabaseProvider Provider { get; }

    DatabaseOutcomeModel Replicate(DatabaseInfoModel info, string targetDb, DatabaseContextModel context);

    DatabaseOutcomeModel Drop(DatabaseInfoModel info, string targetDb, DatabaseContextModel context);
}
