namespace Tily.Core.Mcp;

public static class McpCommands
{
    public const string Tool = "commands";
    public const int DefaultLimit = 20;
    public const int MaxLimit = 100;

    public static int Limit(int requested) => Math.Clamp(requested, 1, MaxLimit);
}
