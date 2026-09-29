using System.Text;

namespace Dock.Core.Shell;

public static class PowerShellIntegration
{
    public const string PromptWrapperScript = """
        $global:__DockOriginalPrompt = $function:prompt
        $global:__DockLastCommandId = $null
        $global:__DockOriginalReadLine = $null
        function global:prompt {
            $dockSucceeded = $?
            try {
                $dockEsc = [char]27
                $dockPath = $PWD.ProviderPath
                if ($PWD.Provider.Name -eq 'FileSystem' -and $dockPath) {
                    $dockUri = ([System.Uri]$dockPath).AbsoluteUri
                    [Console]::Write("$dockEsc]7;$dockUri$dockEsc\")
                }
                $dockLast = Get-History -Count 1
                if ($dockLast -and $dockLast.Id -ne $global:__DockLastCommandId) {
                    $global:__DockLastCommandId = $dockLast.Id
                    $dockMilliseconds = [int64]($dockLast.EndExecutionTime - $dockLast.StartExecutionTime).TotalMilliseconds
                    $dockCommandText = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes([string]$dockLast.CommandLine))
                    [Console]::Write("$dockEsc]6973;done;$dockMilliseconds;$([int]$dockSucceeded);$dockCommandText$dockEsc\")
                }
                if (-not $global:__DockOriginalReadLine -and (Test-Path Function:\PSConsoleHostReadLine)) {
                    $global:__DockOriginalReadLine = $function:PSConsoleHostReadLine
                    function global:PSConsoleHostReadLine {
                        $dockCommandLine = $global:__DockOriginalReadLine.Invoke()
                        [Console]::Write("$([char]27)]6973;exec;$([Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes([string]$dockCommandLine)))$([char]27)\")
                        $dockCommandLine
                    }
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
