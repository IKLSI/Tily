using Tily.Core.Mcp;
using Xunit;

namespace Tily.Core.Tests.Mcp;

public sealed class McpEndpointTests
{
    [Fact]
    public void PipeName_WhenSameFolderWrittenDifferently_ThenSameName()
    {
        var name = McpEndpoint.PipeName(@"C:\Users\Moi\AppData\Local\Tily");

        var variant = McpEndpoint.PipeName(@"c:\users\moi\appdata\local\tily\");

        Assert.Equal(name, variant);
    }

    [Fact]
    public void PipeName_WhenFoldersDiffer_ThenNamesDiffer()
    {
        var installed = McpEndpoint.PipeName(@"C:\Users\Moi\AppData\Local\Tily");

        var isolated = McpEndpoint.PipeName(@"C:\Temp\tily-dev");

        Assert.NotEqual(installed, isolated);
    }

    [Fact]
    public void PipeName_WhenComputed_ThenPrefixedAndBounded()
    {
        var name = McpEndpoint.PipeName(@"C:\Temp\tily-dev");

        Assert.Matches("^tily-mcp-[0-9a-f]{24}$", name);
    }

    [Fact]
    public void DataDirectory_WhenNotOverridden_ThenTilyUnderLocalAppData()
    {
        var directory = McpEndpoint.DataDirectory(" ", @"C:\Users\Moi\AppData\Local");

        Assert.Equal(@"C:\Users\Moi\AppData\Local\Tily", directory);
    }

    [Fact]
    public void DataDirectory_WhenOverridden_ThenFullPathOfOverride()
    {
        var directory = McpEndpoint.DataDirectory(@"C:\Temp\..\Temp\tily-dev", @"C:\Users\Moi\AppData\Local");

        Assert.Equal(@"C:\Temp\tily-dev", directory);
    }
}
