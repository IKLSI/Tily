using Tily.Core.Shell;
using Xunit;

namespace Tily.Core.Tests.Shell;

public sealed class DroppedPathsTests
{
    [Fact]
    public void Format_WhenPathNeedsNoQuoting_ThenInsertsItFollowedBySpace()
    {
        var text = DroppedPaths.Format(["/Users/maxime/notes.txt"], "zsh");

        Assert.Equal("/Users/maxime/notes.txt ", text);
    }

    [Fact]
    public void Format_WhenPathHasSpaces_ThenWrapsInSingleQuotes()
    {
        var text = DroppedPaths.Format(["/Users/maxime/Mes projets/notes.txt"], "zsh");

        Assert.Equal("'/Users/maxime/Mes projets/notes.txt' ", text);
    }

    [Fact]
    public void Format_WhenPathHasApostrophe_ThenClosesAndEscapesIt()
    {
        var text = DroppedPaths.Format(["/Users/maxime/l'été.png"], "bash");

        Assert.Equal("'/Users/maxime/l'\\''été.png' ", text);
    }

    [Fact]
    public void Format_WhenPathHasOnlyNonBreakingSpace_ThenQuotesIt()
    {
        var text = DroppedPaths.Format(["/Users/maxime/A B.txt"], "zsh");

        Assert.Equal("'/Users/maxime/A B.txt' ", text);
    }

    [Theory]
    [InlineData("/tmp/$HOME")]
    [InlineData("/tmp/a*b")]
    [InlineData("/tmp/(copie)")]
    [InlineData("/tmp/~notes")]
    public void Format_WhenPathHasShellSpecials_ThenQuotesItLiterally(string path)
    {
        var text = DroppedPaths.Format([path], "zsh");

        Assert.Equal($"'{path}' ", text);
    }

    [Fact]
    public void Format_WhenSeveralPaths_ThenSeparatesThemBySpaces()
    {
        var text = DroppedPaths.Format(["/tmp/a.txt", "/Users/maxime/Mes projets", "  "], "zsh");

        Assert.Equal("/tmp/a.txt '/Users/maxime/Mes projets' ", text);
    }
}
