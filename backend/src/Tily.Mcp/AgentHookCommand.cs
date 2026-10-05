using System.Text;
using Tily.Core.Agents;
using Tily.Core.Mcp;

namespace Tily.Mcp;

internal static class AgentHookCommand
{
    public static int Run()
    {
        var paneId = McpEndpoint.PaneFromEnvironment();
        if (paneId is null)
        {
            return 0;
        }

        try
        {
            using var input = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false));
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var output = new AgentHookHandler(paneId, McpEndpoint.DataDirectoryFromEnvironment(), home).Handle(input.ReadToEnd());
            if (output is not null)
            {
                using var stdout = Console.OpenStandardOutput();
                stdout.Write(new UTF8Encoding(false).GetBytes(output));
                stdout.Flush();
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }

        return 0;
    }
}
