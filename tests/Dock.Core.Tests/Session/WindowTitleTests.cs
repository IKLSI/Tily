using Dock.Core.Session;
using Xunit;

namespace Dock.Core.Tests.Session;

public sealed class WindowTitleTests
{
    [Fact]
    public void For_WhenContextEmpty_ThenReturnsApplicationName()
    {
        var title = WindowTitle.For("   ");

        Assert.Equal("Dock", title);
    }

    [Fact]
    public void For_WhenContextGiven_ThenAppendsApplicationName()
    {
        var title = WindowTitle.For("Projet › web");

        Assert.Equal("Projet › web - Dock", title);
    }

    [Fact]
    public void For_WhenContextHasControlCharacters_ThenRemovesThem()
    {
        var title = WindowTitle.For("Projet\n› web\u0007");

        Assert.Equal("Projet› web - Dock", title);
    }

    [Fact]
    public void For_WhenContextTooLong_ThenShortensItWithEllipsis()
    {
        var title = WindowTitle.For(new string('a', WindowTitle.MaxContextLength + 20));

        Assert.Equal(new string('a', WindowTitle.MaxContextLength) + "… - Dock", title);
    }
}
