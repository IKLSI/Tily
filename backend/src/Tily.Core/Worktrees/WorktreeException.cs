namespace Tily.Core.Worktrees;

public sealed class WorktreeException : Exception
{
    public WorktreeException(string message, string step, string output = "", IReadOnlyList<string>? lockedBy = null) : base(message)
    {
        Step = step;
        Output = output;
        LockedBy = lockedBy ?? [];
    }

    public string Step { get; }

    public string Output { get; }

    public IReadOnlyList<string> LockedBy { get; }
}
