using Tily.Core.Mcp;
using Xunit;

namespace Tily.Core.Tests.Mcp;

public sealed class McpWorktreesTests
{
    [Theory]
    [InlineData(McpWorktrees.Create, true)]
    [InlineData(McpWorktrees.Remove, true)]
    [InlineData(McpWorktrees.List, false)]
    [InlineData(McpActions.Run, false)]
    public void IsLongOperation_WhenTool_ThenOnlyWorktreeCreationAndRemoval(string tool, bool expected)
    {
        var isLong = McpWorktrees.IsLongOperation(tool);

        Assert.Equal(expected, isLong);
    }
}
