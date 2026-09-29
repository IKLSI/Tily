using System.Diagnostics;
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
    public void Prompt_WhenCalled_ThenAnnouncesCurrentFolder()
    {
        var output = RunWithWrapper(@"Set-Location 'C:\Windows'", "prompt");

        Assert.Contains("]7;file:///C:/Windows", output);
    }
}
