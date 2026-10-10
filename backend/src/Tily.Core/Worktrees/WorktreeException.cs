namespace Tily.Core.Worktrees;

public sealed class WorktreeException : Exception
{
    public WorktreeException(string message, string step, string output = "") : base(message)
    {
        Step = step;
        Output = output;
    }

    public string Step { get; }

    public string Output { get; }
}
