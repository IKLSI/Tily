using System.Collections.Concurrent;

namespace Dock.Core.Git;

public enum GitUndoKind
{
    Commit,
    Amend,
    Merge,
    Pull,
    Rebase,
    CherryPick,
    ResetSoft,
    ResetMixed,
    ResetHard,
    Switch,
    BranchCreate,
    BranchDelete,
    BranchRename,
    TagCreate,
    TagDelete,
    StashPush,
    StashDrop,
    Discard
}

public sealed record GitDiscardedFileModel(string Path, string? Blob, string? After);

public sealed record GitUndoRecordModel
{
    public required GitUndoKind Kind { get; init; }
    public required string Label { get; init; }
    public string? HeadBefore { get; init; }
    public string? HeadAfter { get; init; }
    public string? BranchBefore { get; init; }
    public string? BranchAfter { get; init; }
    public bool Pending { get; init; }
    public bool Published { get; init; }
    public string? IndexTree { get; init; }
    public string? Backup { get; init; }
    public string? RefName { get; init; }
    public string? RefTarget { get; init; }
    public string? NewName { get; init; }
    public string? Upstream { get; init; }
    public string? StashMessage { get; init; }
    public IReadOnlyList<GitDiscardedFileModel> Files { get; init; } = [];
}

public sealed class GitUndoJournal
{
    private readonly ConcurrentDictionary<string, GitUndoRecordModel> _records = new(StringComparer.OrdinalIgnoreCase);

    public GitUndoRecordModel? Get(string root) => _records.TryGetValue(root, out var record) ? record : null;

    public void Apply(string root, GitOutcomeModel outcome)
    {
        if (outcome.Undo is not null)
        {
            _records[root] = outcome.Undo;
        }
        else if (outcome.ClearUndo)
        {
            _records.TryRemove(root, out _);
        }
    }
}
