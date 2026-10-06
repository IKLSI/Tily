using System.Text;
using System.Text.Json;
using Tily.Host.Bridge;
using Xunit;

namespace Tily.Core.Tests.Bridge;

public sealed class TerminalOutputWriterTests
{
    private const string TerminalText = "\u001b[1;32mvert\u001b[0m \"cité\" <b>&é\\r\n\u0007 😀 ✓\t";

    [Fact]
    public void Send_WhenTextHasControlSequences_ThenSameLineAsReflectionSerializer()
    {
        var lines = new List<string>();
        var writer = new TerminalOutputWriter(HostBridge.JsonOptions.Encoder, line => lines.Add(Encoding.UTF8.GetString(line)));

        writer.Send("pane-1", new StringBuilder(TerminalText));

        Assert.Equal([Legacy("pane-1", TerminalText) + "\n"], lines);
    }

    [Fact]
    public void Send_WhenTextLongerThanMessage_ThenSplitsLikeBeforeAcrossChunks()
    {
        var lines = new List<string>();
        var writer = new TerminalOutputWriter(HostBridge.JsonOptions.Encoder, line => lines.Add(Encoding.UTF8.GetString(line)), 7);
        var text = new StringBuilder();
        foreach (var part in new[] { "abc", "\u001bdefgh", "ijklmnopq", "r" })
        {
            text.Append(part);
        }

        var full = text.ToString();
        writer.Send("pane-2", text);

        var expected = Enumerable.Range(0, (full.Length + 6) / 7)
            .Select(index => Legacy("pane-2", full.Substring(index * 7, Math.Min(7, full.Length - index * 7))) + "\n");
        Assert.Equal(expected, lines);
    }

    private static string Legacy(string pane, string data) =>
        JsonSerializer.Serialize(new { type = "terminal.output", pane, data }, HostBridge.JsonOptions);
}
