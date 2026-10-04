using System.Text.Json;
using ModelContextProtocol.Protocol;
using Tily.Core.Mcp;

namespace Tily.Mcp;

internal static class TilyConnection
{
    public static async Task<CallToolResult> CallAsync(string tool, object? arguments, CancellationToken token)
    {
        var pane = Environment.GetEnvironmentVariable(McpEndpoint.PaneVariable);
        if (string.IsNullOrWhiteSpace(pane))
        {
            return Failure(McpPipe.NotInTily);
        }

        var pipe = McpEndpoint.PipeName(McpEndpoint.DataDirectoryFromEnvironment());
        var element = arguments is null ? (JsonElement?)null : JsonSerializer.SerializeToElement(arguments, McpPipe.JsonOptions);
        var response = await McpPipeClient.SendAsync(pipe, new McpPipeRequestModel(tool, pane, element), McpPipeClient.ConnectTimeout, token);
        if (response.Error is { } error)
        {
            return Failure(error);
        }

        var text = response.Result is { } result ? JsonSerializer.Serialize(result, McpPipe.JsonOptions) : McpPipe.UnreadableAnswer;
        return new CallToolResult { Content = [new TextContentBlock { Text = text }], IsError = response.Result is null };
    }

    private static CallToolResult Failure(string message) =>
        new() { Content = [new TextContentBlock { Text = message }], IsError = true };
}
