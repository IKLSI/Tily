using Tily.Core.Session;
using Xunit;

namespace Tily.Core.Tests.Session;

public sealed class WindowTitleTests
{
    [Fact]
    public void For_WhenContextBlank_ThenReturnsApplicationName()
    {
        var title = WindowTitle.For("   ");

        Assert.Equal("Tily", title);
    }

    [Fact]
    public void For_WhenContextGiven_ThenAppendsApplicationName()
    {
        var title = WindowTitle.For("Projet › web");

        Assert.Equal("Projet › web - Tily", title);
    }

    [Fact]
    public void For_WhenContextHasControlCharacters_ThenRemovesThem()
    {
        var title = WindowTitle.For("Projet\n› web\u0007");

        Assert.Equal("Projet› web - Tily", title);
    }

    [Fact]
    public void For_WhenCutFallsInsideSurrogatePair_ThenKeepsThePairOut()
    {
        var context = new string('a', WindowTitle.MaxContextLength - 1) + "😀suite";

        var title = WindowTitle.For(context);

        Assert.Equal(new string('a', WindowTitle.MaxContextLength - 1) + "… - Tily", title);
    }

    [Fact]
    public void For_WhenContextTooLong_ThenShortensItWithEllipsis()
    {
        var title = WindowTitle.For(new string('a', WindowTitle.MaxContextLength + 20));

        Assert.Equal(new string('a', WindowTitle.MaxContextLength) + "… - Tily", title);
    }
}
