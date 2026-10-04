using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Tily.Mcp;

var options = new McpServerOptions
{
    ServerInfo = new Implementation { Name = TilyTools.ServerName, Title = "Tily", Version = TilyTools.Version },
    ServerInstructions = TilyTools.Instructions,
    ToolCollection = TilyTools.Collection()
};

await using var server = McpServer.Create(new StdioServerTransport(TilyTools.ServerName), options);
await server.RunAsync();
