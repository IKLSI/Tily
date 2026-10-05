namespace Tily.Core.Mcp;

public static class McpWorktrees
{
    public const string List = "worktrees";
    public const string Create = "createWorktree";
    public const string Remove = "removeWorktree";
    public static readonly TimeSpan OperationAnswerTimeout = TimeSpan.FromMinutes(10);

    public static bool IsLongOperation(string tool) => tool is Create or Remove;
}
