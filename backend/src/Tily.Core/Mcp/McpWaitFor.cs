using System.Text.Json;

namespace Tily.Core.Mcp;

public static class McpWaitFor
{
    public const string Tool = "waitFor";
    public const string TimeoutArgument = "timeoutSeconds";
    public const int DefaultTimeoutSeconds = 60;
    public const int MaxTimeoutSeconds = 300;
    private static readonly TimeSpan AnswerMargin = TimeSpan.FromSeconds(5);

    public static int TimeoutSeconds(int requested) => Math.Clamp(requested, 1, MaxTimeoutSeconds);

    public static TimeSpan AnswerTimeout(JsonElement? arguments)
    {
        var requested = arguments is { ValueKind: JsonValueKind.Object } values && values.TryGetProperty(TimeoutArgument, out var timeout) && timeout.ValueKind == JsonValueKind.Number && timeout.TryGetInt32(out var seconds)
            ? seconds
            : DefaultTimeoutSeconds;
        return TimeSpan.FromSeconds(TimeoutSeconds(requested)) + AnswerMargin;
    }
}
