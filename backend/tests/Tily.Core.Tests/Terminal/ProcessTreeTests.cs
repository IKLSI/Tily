using Tily.Core.Terminal;
using Xunit;

namespace Tily.Core.Tests.Terminal;

public sealed class ProcessTreeTests
{
    private const uint Terminal = 0x1000005;
    private const uint OtherTerminal = 0x1000006;
    private const uint NoTerminal = 0xFFFFFFFF;

    [Fact]
    public void Members_WhenDescendantsAndTerminalSiblings_ThenListsAllButRoot()
    {
        IReadOnlyList<ProcessEntryModel> snapshot =
        [
            new(1, 0, NoTerminal, "launchd"),
            new(100, 1, Terminal, "zsh"),
            new(101, 100, Terminal, "node"),
            new(102, 101, NoTerminal, "esbuild"),
            new(103, 1, Terminal, "sleep"),
            new(200, 1, OtherTerminal, "vim")
        ];

        var members = ProcessTree.Members(snapshot, 100).Select(entry => entry.Id);

        Assert.Equal([101, 102, 103], members);
    }

    [Fact]
    public void Members_WhenRootGone_ThenUsesKnownTerminal()
    {
        IReadOnlyList<ProcessEntryModel> snapshot = [new(1, 0, NoTerminal, "launchd"), new(103, 1, Terminal, "sleep")];

        var members = ProcessTree.Members(snapshot, 100, Terminal).Select(entry => entry.Id);

        Assert.Equal([103], members);
    }

    [Fact]
    public void Foreground_WhenShellAtPrompt_ThenReportsNothing()
    {
        IReadOnlyList<ProcessEntryModel> snapshot = [new(100, 1, Terminal, "zsh", 100, 100), new(150, 100, Terminal, "gitstatusd", 150, 100)];

        var active = ProcessTree.Foreground(snapshot, 100, ProcessTree.Members(snapshot, 100));

        Assert.Empty(active);
    }

    [Fact]
    public void Foreground_WhenJobRunning_ThenReportsOnlyItsGroup()
    {
        IReadOnlyList<ProcessEntryModel> snapshot =
        [
            new(100, 1, Terminal, "zsh", 100, 300),
            new(150, 100, Terminal, "gitstatusd", 150, 300),
            new(300, 100, Terminal, "node", 300, 300),
            new(301, 300, Terminal, "esbuild", 300, 300)
        ];

        var active = ProcessTree.Foreground(snapshot, 100, ProcessTree.Members(snapshot, 100)).Select(entry => entry.Id);

        Assert.Equal([300, 301], active);
    }

    [Fact]
    public void TerminalDeviceOf_WhenRootHasNoTerminal_ThenReturnsNull()
    {
        IReadOnlyList<ProcessEntryModel> snapshot = [new(100, 1, NoTerminal, "zsh")];

        Assert.Null(ProcessTree.TerminalDeviceOf(snapshot, 100));
    }

    [Theory]
    [InlineData("claude", new[] { "claude", "--resume" })]
    [InlineData("claude", new[] { "node", "--no-warnings", "/opt/homebrew/bin/claude" })]
    [InlineData("codex", new[] { "node", "/opt/homebrew/lib/node_modules/@openai/codex/bin/codex.js" })]
    [InlineData("node", new[] { "node" })]
    [InlineData("zsh", new[] { "-zsh" })]
    public void FromArguments_WhenProgramOrInterpreter_ThenReturnsCommandName(string expected, string[] arguments) =>
        Assert.Equal(expected, ProcessCommandName.FromArguments(arguments));
}
