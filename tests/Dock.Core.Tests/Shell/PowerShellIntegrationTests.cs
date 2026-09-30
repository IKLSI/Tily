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

    private static List<string> CommandNotices(string output) => Regex.Matches(output, @"]6973;done;[0-9]+;[01]").Select(match => match.Value).ToList();

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
    public void Prompt_WhenStrictModeIsOn_ThenStillAnnouncesCommand()
    {
        var output = RunWithWrapper(
            "Set-StrictMode -Version Latest",
            "$fin = Get-Date",
            "Add-History -InputObject ([pscustomobject]@{ CommandLine = 'pnpm build'; ExecutionStatus = 'Completed'; StartExecutionTime = $fin.AddSeconds(-2); EndExecutionTime = $fin })",
            "Get-Item 'C:\\' | Out-Null",
            "prompt | Out-Null",
            "\"erreurs=$($Error.Count)\"");

        Assert.Equal(["]6973;done;2000;1"], CommandNotices(output));
    }

    [Fact]
    public void Prompt_WhenInRegistryProvider_ThenLeavesNoError()
    {
        var output = RunWithWrapper("Set-StrictMode -Version Latest", @"Set-Location 'HKCU:\Software'", "prompt | Out-Null", "\"erreurs=$($Error.Count)\"");

        Assert.EndsWith("erreurs=0", output);
    }

    [Fact]
    public void ReadLine_WhenWrappedByPrompt_ThenAnnouncesExecutionWithCommandText()
    {
        var output = RunWithWrapper("function global:PSConsoleHostReadLine { 'git status -s' }", "prompt | Out-Null", "PSConsoleHostReadLine");

        Assert.Contains($"]6973;exec;{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("git status -s"))}", output);
    }

    [Fact]
    public void Prompt_WhenNewHistoryEntry_ThenAnnouncesCommandText()
    {
        var output = RunWithWrapper(
            "$fin = Get-Date",
            "Add-History -InputObject ([pscustomobject]@{ CommandLine = 'pnpm build --filter é'; ExecutionStatus = 'Completed'; StartExecutionTime = $fin.AddSeconds(-12); EndExecutionTime = $fin })",
            "prompt");

        Assert.Contains($"]6973;done;12000;1;{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("pnpm build --filter é"))}", output);
    }

    [Fact]
    public void Prompt_WhenNoHistory_ThenAnnouncesNoCommand()
    {
        var output = RunWithWrapper("prompt");

        Assert.Empty(CommandNotices(output));
    }

    [Fact]
    public void Prompt_WhenOriginalPromptHasTwoLines_ThenAnnouncesTwoRows()
    {
        var output = RunWithWrapper("$global:__DockOriginalPrompt = { \"dépôt main`nPS> \" }", "prompt");

        Assert.Contains("]6973;prompt;2", output);
    }

    [Fact]
    public void Prompt_WhenOriginalPromptIsColored_ThenIgnoresEscapeSequences()
    {
        var output = RunWithWrapper("$global:__DockOriginalPrompt = { \"$([char]27)[32m$([char]27)]8;;https://dock.local$([char]27)\\PS$([char]27)]8;;$([char]27)\\$([char]27)[0m> \" }", "prompt");

        Assert.Contains("]6973;prompt;1", output);
    }

    [Fact]
    public void Prompt_WhenOriginalPromptIsWiderThanBuffer_ThenCountsWrappedRows()
    {
        var output = RunWithWrapper("$global:__DockOriginalPrompt = { ('x' * ($Host.UI.RawUI.BufferSize.Width + 5)) + '> ' }", "prompt");

        Assert.Contains("]6973;prompt;2", output);
    }

    [Fact]
    public void Prompt_WhenCalled_ThenAnnouncesCurrentFolder()
    {
        var output = RunWithWrapper(@"Set-Location 'C:\Windows'", "prompt");

        Assert.Contains("]7;file:///C:/Windows", output);
    }
}
