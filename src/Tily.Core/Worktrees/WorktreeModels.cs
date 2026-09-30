using System.Text.Json.Serialization;

namespace Tily.Core.Worktrees;

[JsonConverter(typeof(JsonStringEnumConverter<WorktreeBranchMode>))]
public enum WorktreeBranchMode
{
    [JsonStringEnumMemberName("new")] New,
    [JsonStringEnumMemberName("local")] Local,
    [JsonStringEnumMemberName("remote")] Remote
}

[JsonConverter(typeof(JsonStringEnumConverter<WorktreeStepStatus>))]
public enum WorktreeStepStatus
{
    [JsonStringEnumMemberName("ok")] Ok,
    [JsonStringEnumMemberName("warning")] Warning,
    [JsonStringEnumMemberName("failed")] Failed
}

public sealed record WorktreeModel(string Path, string? Branch, string? Head, bool IsMain, bool IsDetached, bool Locked, bool Prunable);

public sealed record WorktreeRequestModel(string Repository, string Branch, WorktreeBranchMode Mode, string? Base);

public sealed record WorktreeRepositoryModel(string MainRoot, string Project, IReadOnlyList<string> LocalBranches, IReadOnlyList<string> RemoteBranches, IReadOnlyList<string> Remotes);

public sealed record WorktreePlanModel(
    string Repository,
    string Project,
    string? Path,
    string? LocalBranch,
    string? DefaultBase,
    IReadOnlyList<string> LocalBranches,
    IReadOnlyList<string> RemoteBranches,
    string? Error,
    string? ConfiguredBase = null);

public sealed record WorktreeStepModel(string Step, WorktreeStepStatus Status, string Message);

public sealed record PortChangeModel(int Old, int New);

public sealed record WorktreeCreationModel(string Path, string Name, string Branch, string Project, string MainRoot, IReadOnlyList<WorktreeStepModel> Steps);

public sealed record WorktreeRemovalModel(string Path, string? Branch, IReadOnlyList<WorktreeStepModel> Steps);
