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
            $dockPrompt = if ($global:__DockOriginalPrompt) { & $global:__DockOriginalPrompt } else { "PS $($PWD.Path)> " }
            try {
                $dockWidth = [Math]::Max(1, $Host.UI.RawUI.BufferSize.Width)
                $dockVisible = ($dockPrompt -join '') -replace "$dockEsc\[[0-9;?]*[ -/]*[@-~]", '' -replace "$dockEsc\][^$([char]7)$dockEsc]*($([char]7)|$dockEsc\\)", ''
                $dockRows = 0
                foreach ($dockLine in ($dockVisible -split "`n")) { $dockRows += [Math]::Max(1, [Math]::Ceiling($dockLine.TrimEnd("`r").Length / $dockWidth)) }
                [Console]::Write("$dockEsc]6973;prompt;$dockRows$dockEsc\")
            } catch {}
            $dockPrompt
        }
        """;

    public static string EncodedPromptWrapper() => Convert.ToBase64String(Encoding.Unicode.GetBytes(PromptWrapperScript));
}
