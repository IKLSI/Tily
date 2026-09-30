namespace Tily.Core.Agents;

public enum ClaudeSessionStatus
{
    Unknown,
    Busy,
    Shell,
    Idle,
    Waiting
}

public sealed record ClaudeSessionModel(int ProcessId, string SessionId, string? Directory, ClaudeSessionStatus Status, string? WaitingFor, DateTime? StatusUpdatedAtUtc);
