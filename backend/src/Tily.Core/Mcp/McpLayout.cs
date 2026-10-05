using System.Text.Json;
using System.Text.Json.Nodes;
using Tily.Core.Context;

namespace Tily.Core.Mcp;

public static class McpLayout
{
    public const string Tool = "layout";

    public static JsonElement WithBranches(JsonElement layout, Func<string, GitContextModel> resolve)
    {
        var root = JsonNode.Parse(layout.GetRawText());
        var panes = (root?["workspaces"] as JsonArray ?? [])
            .SelectMany(workspace => workspace?["tabs"] as JsonArray ?? [])
            .SelectMany(tab => tab?["panes"] as JsonArray ?? [])
            .OfType<JsonObject>();
        var contexts = new Dictionary<string, GitContextModel>(StringComparer.OrdinalIgnoreCase);
        foreach (var pane in panes)
        {
            if (pane["path"] is not JsonValue value || !value.TryGetValue<string>(out var path) || path.Length == 0)
            {
                continue;
            }

            if (!contexts.TryGetValue(path, out var context))
            {
                context = resolve(path);
                contexts[path] = context;
            }

            pane["branch"] = context.Branch;
            if (context.DetachedHead)
            {
                pane["detachedHead"] = true;
            }
        }

        return JsonSerializer.SerializeToElement(root, McpPipe.JsonOptions);
    }
}
