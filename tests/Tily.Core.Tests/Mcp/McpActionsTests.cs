using Tily.Core.Mcp;
using Xunit;

namespace Tily.Core.Tests.Mcp;

public sealed class McpActionsTests
{
    [Fact]
    public void Command_WhenSingleLine_ThenTrimmed()
    {
        var command = McpActions.Command("  pnpm dev  ");

        Assert.Equal("pnpm dev", command);
    }

    [Theory]
    [InlineData("pnpm install\npnpm dev")]
    [InlineData("pnpm install\rpnpm dev")]
    public void Command_WhenSeveralLines_ThenFailsInFrench(string command)
    {
        var error = Assert.Throws<InvalidOperationException>(() => McpActions.Command(command));

        Assert.Equal("La commande doit tenir sur une ligne : enchaînez plusieurs commandes avec « ; ».", error.Message);
    }

    [Fact]
    public void Command_WhenTooLong_ThenFails()
    {
        var error = Assert.Throws<InvalidOperationException>(() => McpActions.Command(new string('a', McpActions.MaxCommandChars + 1)));

        Assert.StartsWith("Commande trop longue", error.Message);
    }

    [Fact]
    public void Command_WhenBlank_ThenNothing()
    {
        var command = McpActions.Command(" ");

        Assert.Null(command);
    }

    [Fact]
    public void Name_WhenTooLong_ThenFails()
    {
        var error = Assert.Throws<InvalidOperationException>(() => McpActions.Name(new string('n', McpActions.MaxNameChars + 1)));

        Assert.StartsWith("Nom trop long", error.Message);
    }

    [Theory]
    [InlineData(McpActions.Run, true)]
    [InlineData(McpActions.Interrupt, true)]
    [InlineData(McpActions.Close, true)]
    [InlineData(McpActions.NewTab, false)]
    [InlineData(McpLayout.Tool, false)]
    public void MayAskConsent_WhenTool_ThenOnlyWritingAndClosing(string tool, bool expected)
    {
        var mayAsk = McpActions.MayAskConsent(tool);

        Assert.Equal(expected, mayAsk);
    }

    [Fact]
    public void Name_WhenPadded_ThenTrimmed()
    {
        var name = McpActions.Name("  API  ");

        Assert.Equal("API", name);
    }
}
