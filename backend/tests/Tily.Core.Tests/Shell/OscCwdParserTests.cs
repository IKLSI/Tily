using System.Text;
using Tily.Core.Shell;
using Xunit;

namespace Tily.Core.Tests.Shell;

public sealed class OscCwdParserTests
{
    private const string Escape = "\u001b";

    [Fact]
    public void Feed_WhenOsc7WithStringTerminator_ThenReportsDecodedPath()
    {
        var received = Parse($"texte{Escape}]7;file:///Files/Mes%20projets{Escape}\\suite");

        Assert.Equal("/Files/Mes projets", received);
    }

    [Fact]
    public void Feed_WhenOsc7WithBell_ThenReportsPath()
    {
        var received = Parse($"{Escape}]7;file:///Files/Projects\u0007");

        Assert.Equal("/Files/Projects", received);
    }

    [Fact]
    public void Feed_WhenOsc7WithHost_ThenIgnoresHost()
    {
        var received = Parse($"{Escape}]7;file://MacBook-Pro.local/Users/loick/Dev{Escape}\\");

        Assert.Equal("/Users/loick/Dev", received);
    }

    [Fact]
    public void Feed_WhenOsc7WithEncodedUtf8_ThenDecodesIt()
    {
        var received = Parse($"{Escape}]7;file:///Files/%C3%A9t%C3%A9%23%25{Escape}\\");

        Assert.Equal("/Files/été#%", received);
    }

    [Fact]
    public void Feed_WhenSequenceSplitAcrossChunks_ThenStillReports()
    {
        var parser = new OscCwdParser();
        string? received = null;
        parser.CurrentDirectoryChanged += path => received = path;

        parser.Feed(Encoding.UTF8.GetBytes($"{Escape}]7;file:///Fi"));
        parser.Feed(Encoding.UTF8.GetBytes($"les{Escape}\\"));

        Assert.Equal("/Files", received);
    }

    [Fact]
    public void Feed_WhenOtherOsc_ThenIgnores()
    {
        var parser = new OscCwdParser();
        var count = 0;
        parser.CurrentDirectoryChanged += _ => count++;

        parser.Feed(Encoding.UTF8.GetBytes($"{Escape}]0;titre\u0007{Escape}]9;9;/tmp\u0007"));

        Assert.Equal(0, count);
    }

    private static string? Parse(string text)
    {
        var parser = new OscCwdParser();
        string? received = null;
        parser.CurrentDirectoryChanged += path => received = path;
        parser.Feed(Encoding.UTF8.GetBytes(text));
        return received;
    }
}
