using System.Text;

namespace Dock.Core.Shell;

public static class PowerShellIntegration
{
    public const string PromptWrapperScript = """
        $global:__DockOriginalPrompt = $function:prompt
        function global:prompt {
            $dockSucceeded = $?
            try {
                $dockEsc = [char]27
                $dockPath = $PWD.ProviderPath
                if ($dockPath) {
                    $dockUri = ([System.Uri]$dockPath).AbsoluteUri
                    [Console]::Write("$dockEsc]7;$dockUri$dockEsc\")
                }
                $dockLast = Get-History -Count 1
                if ($dockLast -and $dockLast.Id -ne $global:__DockLastCommandId) {
                    $global:__DockLastCommandId = $dockLast.Id
                    $dockMilliseconds = [int64]($dockLast.EndExecutionTime - $dockLast.StartExecutionTime).TotalMilliseconds
                    [Console]::Write("$dockEsc]6973;done;$dockMilliseconds;$([int]$dockSucceeded)$dockEsc\")
                }
            } catch {}
            if (-not $dockSucceeded) { Write-Error '' -ErrorAction Ignore }
            if ($global:__DockOriginalPrompt) { & $global:__DockOriginalPrompt } else { "PS $($PWD.Path)> " }
        }
        """;

    public static string EncodedPromptWrapper() => Convert.ToBase64String(Encoding.Unicode.GetBytes(PromptWrapperScript));
}
