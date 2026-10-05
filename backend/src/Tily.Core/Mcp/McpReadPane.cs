namespace Tily.Core.Mcp;

public static class McpReadPane
{
    public const string Tool = "readPane";
    public const int DefaultLines = 100;
    public const int MaxLines = 2000;

    public static int Lines(int requested) => Math.Clamp(requested, 1, MaxLines);
}
