using Tily.Core.Mcp;
using Xunit;

namespace Tily.Core.Tests.Mcp;

public sealed class McpEndpointTests
{
    [Fact]
    public void PipeName_WhenSameFolderWrittenDifferently_ThenSameName()
    {
        var name = McpEndpoint.PipeName("/Users/Moi/Library/Application Support/Tily");

        var variant = McpEndpoint.PipeName("/users/moi/library/application support/tily/");

        Assert.Equal(name, variant);
    }

    [Fact]
    public void PipeName_WhenFoldersDiffer_ThenNamesDiffer()
    {
        var installed = McpEndpoint.PipeName("/Users/Moi/Library/Application Support/Tily");

        var isolated = McpEndpoint.PipeName("/Temp/tily-dev");

        Assert.NotEqual(installed, isolated);
    }

    [Fact]
    public void PipeName_WhenComputed_ThenPrefixedAndBounded()
    {
        var name = McpEndpoint.PipeName("/Temp/tily-dev");

        Assert.Matches("^tily-mcp-[0-9a-f]{24}$", name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Pane_WhenVariableMissingOrBlank_ThenOutsideTily(string? value)
    {
        var pane = McpEndpoint.Pane(value);

        Assert.Null(pane);
    }

    [Fact]
    public void Pane_WhenVariableSet_ThenTrimmedPaneId()
    {
        var pane = McpEndpoint.Pane(" 95724b0bb3834147a87a2b03e9aefbd0 ");

        Assert.Equal("95724b0bb3834147a87a2b03e9aefbd0", pane);
    }

    [Fact]
    public void DataDirectory_WhenNotOverridden_ThenTilyUnderApplicationSupport()
    {
        var directory = McpEndpoint.DataDirectory(" ", "/Users/Moi/Library/Application Support");

        Assert.Equal("/Users/Moi/Library/Application Support/Tily", directory);
    }

    [Fact]
    public void DataDirectory_WhenOverridden_ThenFullPathOfOverride()
    {
        var directory = McpEndpoint.DataDirectory("/Temp/../Temp/tily-dev", "/Users/Moi/Library/Application Support");

        Assert.Equal("/Temp/tily-dev", directory);
    }
}
