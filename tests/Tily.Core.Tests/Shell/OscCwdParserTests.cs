using System.Text;
using Tily.Core.Shell;
using Xunit;

namespace Tily.Core.Tests.Shell;

public sealed class OscCwdParserTests
{
    private const string Escape = "\u001b";

    [Fact]
    public void Feed_WhenOsc7WithStringTerminator_ThenReportsLocalPath()
    {
        var parser = new OscCwdParser();
        string? received = null;
        parser.CurrentDirectoryChanged += path => received = path;

        parser.Feed(Encoding.UTF8.GetBytes($"texte{Escape}]7;file:///C:/Users/Maxime%20X{Escape}\\suite"));

        Assert.Equal(@"C:\Users\Maxime X", received);
    }

    [Fact]
    public void Feed_WhenOsc7WithBell_ThenReportsLocalPath()
    {
        var parser = new OscCwdParser();
        string? received = null;
        parser.CurrentDirectoryChanged += path => received = path;

        parser.Feed(Encoding.UTF8.GetBytes($"{Escape}]7;file:///D:/Projets\u0007"));

        Assert.Equal(@"D:\Projets", received);
    }

    [Fact]
    public void Feed_WhenOsc7WithHostAndDrivePath_ThenReportsLocalPath()
    {
        var parser = new OscCwdParser();
        string? received = null;
        parser.CurrentDirectoryChanged += path => received = path;

        parser.Feed(Encoding.UTF8.GetBytes($"{Escape}]7;file://pc-loick/C:/Users/Loick%20B{Escape}\\"));

        Assert.Equal(@"C:\Users\Loick B", received);
    }

    [Fact]
    public void Feed_WhenOsc7WithUncPath_ThenReportsUncPath()
    {
        var parser = new OscCwdParser();
        string? received = null;
        parser.CurrentDirectoryChanged += path => received = path;

        parser.Feed(Encoding.UTF8.GetBytes($"{Escape}]7;file://serveur/partage/dossier{Escape}\\"));

        Assert.Equal(@"\\serveur\partage\dossier", received);
    }

    [Fact]
    public void Feed_WhenSequenceSplitAcrossChunks_ThenStillReports()
    {
        var parser = new OscCwdParser();
        string? received = null;
        parser.CurrentDirectoryChanged += path => received = path;

        parser.Feed(Encoding.UTF8.GetBytes($"{Escape}]7;file:///C:/Wi"));
        parser.Feed(Encoding.UTF8.GetBytes($"ndows{Escape}\\"));

        Assert.Equal(@"C:\Windows", received);
    }

    [Fact]
    public void Feed_WhenOtherOsc_ThenIgnores()
    {
        var parser = new OscCwdParser();
        var count = 0;
        parser.CurrentDirectoryChanged += _ => count++;

        parser.Feed(Encoding.UTF8.GetBytes($"{Escape}]0;titre\u0007{Escape}]9;9;C:\\x\u0007"));

        Assert.Equal(0, count);
    }
}
