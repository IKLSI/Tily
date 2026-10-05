using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Tily.Core.Agents;
using Tily.Core.Mcp;
using Tily.Mcp;

if (args is [ClaudeHooksInstaller.HookArgument, ..])
{
    return AgentHookCommand.Run();
}

var inTily = McpEndpoint.PaneFromEnvironment() is not null;
var options = new McpServerOptions
{
    ServerInfo = new Implementation { Name = TilyTools.ServerName, Title = "Tily", Version = TilyTools.Version },
    ServerInstructions = inTily ? TilyTools.Instructions : null,
    ToolCollection = inTily ? TilyTools.Collection() : []
};

await using var server = McpServer.Create(new StdioServerTransport(TilyTools.ServerName), options);
await server.RunAsync();
return 0;
