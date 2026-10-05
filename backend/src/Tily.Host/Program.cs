using Tily.Core.Agents;
using Tily.Core.Mcp;
using Tily.Host;
using Tily.Host.Bridge;

const string RemoveClaudeHooksArgument = "--remove-claude-hooks";

if (args.Contains(RemoveClaudeHooksArgument, StringComparer.Ordinal))
{
    RemoveClaudeIntegration();
    return 0;
}

var dataDirectory = ResolveDataDirectory();
var channel = new StdioChannel();
using var loop = new HostLoop(exception => Console.Error.WriteLine(exception));
using (var bridge = new HostBridge(loop, dataDirectory, channel.Send))
{
    channel.Listen(line => loop.TryEnqueue(() => bridge.Receive(line)), () => loop.TryEnqueue(loop.Stop));
    bridge.Start();
    loop.Run();
}

return 0;

static string ResolveDataDirectory()
{
    var directory = McpEndpoint.DataDirectoryFromEnvironment();
    if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(McpEndpoint.DataDirectoryVariable)))
    {
        Environment.SetEnvironmentVariable(McpEndpoint.DataDirectoryVariable, directory);
    }

    Directory.CreateDirectory(directory);
    return directory;
}

static void RemoveClaudeIntegration()
{
    try
    {
        new ClaudeHooksInstaller(ClaudeHooksInstaller.DefaultExecutable()).RemoveIfPresent();
    }
    catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException)
    {
    }

    try
    {
        new ClaudeMcpInstaller(Path.Combine(AppContext.BaseDirectory, ClaudeMcpInstaller.ExecutableName)).RemoveIfPresent();
    }
    catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException)
    {
    }
}
