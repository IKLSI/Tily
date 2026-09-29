using System.Text;

namespace Dock.Core.Shell;

public static class PowerShellIntegration
{
    public const string PromptWrapperScript = """
        $global:__DockOriginalPrompt = $function:prompt
        function global:prompt {
            $dockSucceeded = $?
            try {
                $dockPath = $PWD.ProviderPath
                if ($dockPath) {
                    $dockEsc = [char]27
                    $dockUri = ([System.Uri]$dockPath).AbsoluteUri
                    [Console]::Write("$dockEsc]7;$dockUri$dockEsc\")
                }
            } catch {}
            if (-not $dockSucceeded) { Write-Error '' -ErrorAction Ignore }
            if ($global:__DockOriginalPrompt) { & $global:__DockOriginalPrompt } else { "PS $($PWD.Path)> " }
        }
        """;

    public static string EncodedPromptWrapper() => Convert.ToBase64String(Encoding.Unicode.GetBytes(PromptWrapperScript));
}
