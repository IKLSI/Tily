using System.Text;
using Tily.Host.Bridge;
using Xunit;

namespace Tily.Core.Tests.Bridge;

public sealed class PaneOutputBufferTests
{
    [Fact]
    public void Append_WhenCharacterSplitBetweenReads_ThenDecodedOnce()
    {
        var buffer = new PaneOutputBuffer("pane-1");
        var bytes = Encoding.UTF8.GetBytes("é😀");

        buffer.Append(bytes.AsSpan(0, 1));
        buffer.Append(bytes.AsSpan(1, 3));
        buffer.Append(bytes.AsSpan(4));

        Assert.Equal("é😀", buffer.Take()?.ToString());
    }

    [Fact]
    public void Append_WhenLargerThanDecodeChunk_ThenKeepsWholeText()
    {
        var buffer = new PaneOutputBuffer("pane-1");
        var text = string.Concat(Enumerable.Repeat("ligne é 😀\r\n", 5000));

        buffer.Append(Encoding.UTF8.GetBytes(text));

        Assert.Equal(text, buffer.Take()?.ToString());
    }

    [Fact]
    public void Take_WhenRecycledBuilder_ThenNextTakeHasOnlyNewText()
    {
        var buffer = new PaneOutputBuffer("pane-1");
        buffer.Append("avant"u8);
        buffer.Recycle(buffer.Take()!);

        buffer.Append("après"u8);

        Assert.Equal("après", buffer.Take()?.ToString());
    }

    [Fact]
    public void Take_WhenNothingPending_ThenNull()
    {
        var buffer = new PaneOutputBuffer("pane-1");

        Assert.Null(buffer.Take());
    }
}
