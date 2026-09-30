using System.Text;

namespace Tily.Core.Shell;

public static class PowerShellIntegration
{
    public const string PromptWrapperScript = """
        $global:__TilyOriginalPrompt = $function:prompt
        $global:__TilyLastCommandId = $null
        $global:__TilyOriginalReadLine = $null
        function global:prompt {
            $tilySucceeded = $?
            try {
                $tilyEsc = [char]27
                $tilyPath = $PWD.ProviderPath
                if ($PWD.Provider.Name -eq 'FileSystem' -and $tilyPath) {
                    $tilyUri = ([System.Uri]$tilyPath).AbsoluteUri
                    [Console]::Write("$tilyEsc]7;$tilyUri$tilyEsc\")
                }
                $tilyLast = Get-History -Count 1
                if ($tilyLast -and $tilyLast.Id -ne $global:__TilyLastCommandId) {
                    $global:__TilyLastCommandId = $tilyLast.Id
                    $tilyMilliseconds = [int64]($tilyLast.EndExecutionTime - $tilyLast.StartExecutionTime).TotalMilliseconds
                    $tilyCommandText = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes([string]$tilyLast.CommandLine))
                    [Console]::Write("$tilyEsc]6973;done;$tilyMilliseconds;$([int]$tilySucceeded);$tilyCommandText$tilyEsc\")
                }
                if (-not $global:__TilyOriginalReadLine -and (Test-Path Function:\PSConsoleHostReadLine)) {
                    $global:__TilyOriginalReadLine = $function:PSConsoleHostReadLine
                    function global:PSConsoleHostReadLine {
                        $tilyCommandLine = $global:__TilyOriginalReadLine.Invoke()
                        [Console]::Write("$([char]27)]6973;exec;$([Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes([string]$tilyCommandLine)))$([char]27)\")
                        $tilyCommandLine
                    }
                }
            } catch {}
            if (-not $tilySucceeded) { Write-Error '' -ErrorAction Ignore }
            $tilyPrompt = if ($global:__TilyOriginalPrompt) { & $global:__TilyOriginalPrompt } else { "PS $($PWD.Path)> " }
            try {
                $tilyWidth = [Math]::Max(1, $Host.UI.RawUI.BufferSize.Width)
                $tilyVisible = ($tilyPrompt -join '') -replace "$tilyEsc\[[0-9;?]*[ -/]*[@-~]", '' -replace "$tilyEsc\][^$([char]7)$tilyEsc]*($([char]7)|$tilyEsc\\)", ''
                $tilyRows = 0
                foreach ($tilyLine in ($tilyVisible -split "`n")) { $tilyRows += [Math]::Max(1, [Math]::Ceiling($tilyLine.TrimEnd("`r").Length / $tilyWidth)) }
                [Console]::Write("$tilyEsc]6973;prompt;$tilyRows$tilyEsc\")
            } catch {}
            $tilyPrompt
        }
        """;

    public static string EncodedPromptWrapper() => Convert.ToBase64String(Encoding.Unicode.GetBytes(PromptWrapperScript));
}
