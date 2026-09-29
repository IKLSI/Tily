using System.Diagnostics;
using System.Text.RegularExpressions;
using Dock.Core.Shell;
using Xunit;

namespace Dock.Core.Tests.Shell;

public sealed class PowerShellIntegrationTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private static string RunWithWrapper(params string[] commands)
    {
        var script = string.Join('\n', ["function global:prompt { \"statut=$?\" }", PowerShellIntegration.PromptWrapperScript, .. commands]);
        var encoded = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script));
        using var process = Process.Start(new ProcessStartInfo("powershell.exe", $"-NoLogo -NoProfile -NonInteractive -EncodedCommand {encoded}") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true })!;
        var output = process.StandardOutput.ReadToEndAsync();
        if (!process.WaitForExit(Timeout))
        {
            process.Kill(true);
            throw new TimeoutException("PowerShell n'a pas répondu à temps.");
        }

        return string.Concat(output.Result.Where(character => character != '\x1b')).Trim();
    }

    private static List<string> CommandNotices(string output) => Regex.Matches(output, @"\]6973;[0-9a-z;]*").Select(match => match.Value).ToList();

    [Fact]
    public void Prompt_WhenLastCommandFailed_ThenOriginalPromptSeesFailure()
    {
        var output = RunWithWrapper(@"Get-Item 'C:\dock-dossier-inexistant' -ErrorAction SilentlyContinue", "prompt");

        Assert.EndsWith("statut=False", output);
    }

    [Fact]
    public void Prompt_WhenLastCommandSucceeded_ThenOriginalPromptSeesSuccess()
    {
        var output = RunWithWrapper(@"Get-Item 'C:\' | Out-Null", "prompt");

        Assert.EndsWith("statut=True", output);
    }

    [Fact]
    public void Prompt_WhenNewHistoryEntry_ThenAnnouncesCommandDurationAndSuccess()
    {
        var output = RunWithWrapper(
            "$fin = Get-Date",
            "Add-History -InputObject ([pscustomobject]@{ CommandLine = 'pnpm build'; ExecutionStatus = 'Completed'; StartExecutionTime = $fin.AddSeconds(-12); EndExecutionTime = $fin })",
            "Get-Item 'C:\\' | Out-Null",
            "prompt",
            "prompt");

        Assert.Equal(["]6973;done;12000;1"], CommandNotices(output));
    }

    [Fact]
    public void Prompt_WhenNewHistoryEntryFailed_ThenAnnouncesFailure()
    {
        var output = RunWithWrapper(
            "$fin = Get-Date",
            "Add-History -InputObject ([pscustomobject]@{ CommandLine = 'pnpm test'; ExecutionStatus = 'Failed'; StartExecutionTime = $fin.AddSeconds(-3); EndExecutionTime = $fin })",
            "Get-Item 'C:\\dock-dossier-inexistant' -ErrorAction SilentlyContinue",
            "prompt");

        Assert.Equal(["]6973;done;3000;0"], CommandNotices(output));
    }

    [Fact]
    public void Prompt_WhenNoHistory_ThenAnnouncesNoCommand()
    {
        var output = RunWithWrapper("prompt");

        Assert.Empty(CommandNotices(output));
    }

    [Fact]
    public void Prompt_WhenCalled_ThenAnnouncesCurrentFolder()
    {
        var output = RunWithWrapper(@"Set-Location 'C:\Windows'", "prompt");

        Assert.Contains("]7;file:///C:/Windows", output);
    }
}
