using Tily.Core.Shell;
using Xunit;

namespace Tily.Core.Tests.Shell;

public sealed class DroppedPathsTests
{
    [Fact]
    public void Format_WhenPathNeedsNoQuoting_ThenInsertsItFollowedBySpace()
    {
        var text = DroppedPaths.Format([@"C:\Temp\notes.txt"], "powershell");

        Assert.Equal(@"C:\Temp\notes.txt ", text);
    }

    [Fact]
    public void Format_WhenPowerShellPathHasSpaces_ThenWrapsInSingleQuotes()
    {
        var text = DroppedPaths.Format([@"C:\Mes projets\notes.txt"], "powershell");

        Assert.Equal(@"'C:\Mes projets\notes.txt' ", text);
    }

    [Fact]
    public void Format_WhenPowerShellPathHasApostrophe_ThenDoublesIt()
    {
        var text = DroppedPaths.Format([@"C:\Photos\l'été.png"], "pwsh");

        Assert.Equal(@"'C:\Photos\l''été.png' ", text);
    }

    [Fact]
    public void Format_WhenPowerShellPathHasTypographicApostrophe_ThenQuotesAndDoublesIt()
    {
        var text = DroppedPaths.Format([@"C:\Docs\Lettre d’information.pdf"], "powershell");

        Assert.Equal(@"'C:\Docs\Lettre d’’information.pdf' ", text);
    }

    [Fact]
    public void Format_WhenPathHasOnlyNonBreakingSpace_ThenQuotesIt()
    {
        var text = DroppedPaths.Format(["C:\\Docs\\A\u00A0B.txt"], "powershell");

        Assert.Equal("'C:\\Docs\\A\u00A0B.txt' ", text);
    }

    [Fact]
    public void Format_WhenPowerShellPathHasDollar_ThenQuotesItLiterally()
    {
        var text = DroppedPaths.Format([@"C:\$Recycle.Bin"], "powershell");

        Assert.Equal(@"'C:\$Recycle.Bin' ", text);
    }

    [Fact]
    public void Format_WhenCmdPathHasSpaces_ThenWrapsInDoubleQuotes()
    {
        var text = DroppedPaths.Format([@"C:\Program Files\Git"], "cmd");

        Assert.Equal(@"""C:\Program Files\Git"" ", text);
    }

    [Fact]
    public void Format_WhenGitBashPath_ThenSingleQuotesKeepBackslashes()
    {
        var text = DroppedPaths.Format([@"C:\Temp\notes.txt"], "gitbash");

        Assert.Equal(@"'C:\Temp\notes.txt' ", text);
    }

    [Fact]
    public void Format_WhenGitBashPathHasApostrophe_ThenClosesAndEscapesIt()
    {
        var text = DroppedPaths.Format([@"C:\l'été"], "gitbash");

        Assert.Equal(@"'C:\l'\''été' ", text);
    }

    [Fact]
    public void Format_WhenSeveralPaths_ThenSeparatesThemBySpaces()
    {
        var text = DroppedPaths.Format([@"C:\a.txt", @"C:\Mes projets", "  "], "powershell");

        Assert.Equal(@"C:\a.txt 'C:\Mes projets' ", text);
    }
}
