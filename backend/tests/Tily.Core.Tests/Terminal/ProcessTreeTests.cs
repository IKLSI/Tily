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

    [Fact]
    public void IsSameProcess_WhenSameStartAndGroup_ThenTrue()
    {
        var known = new ProcessEntryModel(300, 100, Terminal, "node", 300, 300, 1_700_000_000_000_000);
        var current = known with { ParentId = 1, TerminalDevice = NoTerminal };

        Assert.True(ProcessTree.IsSameProcess(known, current));
    }

    [Fact]
    public void IsSameProcess_WhenIdentifierReusedLater_ThenFalse()
    {
        var known = new ProcessEntryModel(300, 100, Terminal, "node", 300, 300, 1_700_000_000_000_000);
        var current = new ProcessEntryModel(300, 1, NoTerminal, "Safari", 300, 0, 1_700_000_005_000_000);

        Assert.False(ProcessTree.IsSameProcess(known, current));
    }

    [Fact]
    public void IsSameProcess_WhenProcessGone_ThenFalse()
    {
        var known = new ProcessEntryModel(300, 100, Terminal, "node", 300, 300, 1_700_000_000_000_000);

        Assert.False(ProcessTree.IsSameProcess(known, null));
    }

    [Fact]
    public void IsSameProcess_WhenSameStartButUnrelated_ThenFalse()
    {
        var known = new ProcessEntryModel(300, 100, Terminal, "node", 300, 300, 1_700_000_000_000_000);
        var current = new ProcessEntryModel(300, 1, OtherTerminal, "vim", 400, 400, 1_700_000_000_000_000);

        Assert.False(ProcessTree.IsSameProcess(known, current));
    }

    [Fact]
    public void Contains_WhenDescendantOfPaneShell_ThenTrue()
    {
        IReadOnlyList<ProcessEntryModel> snapshot =
        [
            new(100, 1, Terminal, "zsh"),
            new(300, 100, Terminal, "claude"),
            new(400, 300, NoTerminal, "tily-mcp")
        ];

        Assert.True(ProcessTree.Contains(snapshot, 100, 400));
    }

    [Fact]
    public void Contains_WhenProcessOfAnotherPane_ThenFalse()
    {
        IReadOnlyList<ProcessEntryModel> snapshot =
        [
            new(100, 1, Terminal, "zsh"),
            new(200, 1, OtherTerminal, "zsh"),
            new(300, 200, OtherTerminal, "claude"),
            new(400, 300, NoTerminal, "tily-mcp")
        ];

        Assert.False(ProcessTree.Contains(snapshot, 100, 400));
    }
}
