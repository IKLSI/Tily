$paneId = $env:TILY_PANE_ID
if ([string]::IsNullOrWhiteSpace($paneId)) { exit 0 }

$raw = [System.IO.StreamReader]::new([Console]::OpenStandardInput(), [System.Text.UTF8Encoding]::new($false)).ReadToEnd()
try { $hook = $raw | ConvertFrom-Json } catch { exit 0 }

$dataDirectory = if ([string]::IsNullOrWhiteSpace($env:TILY_DATA_DIR)) { Join-Path $env:LOCALAPPDATA 'Tily' } else { $env:TILY_DATA_DIR }
$directory = Join-Path $dataDirectory 'agents'
$file = Join-Path $directory "$paneId.json"
$eventName = [string]$hook.hook_event_name

if ($eventName -eq 'SessionEnd') {
    Remove-Item -LiteralPath $file -Force -ErrorAction SilentlyContinue
    exit 0
}

function ConvertFrom-BashPath([string]$path) {
    if ($path -match '^/([a-zA-Z])(/.*)?$') { $path = $Matches[1].ToUpperInvariant() + ':' + $(if ($Matches[2]) { $Matches[2] } else { '/' }) }
    elseif ($path -match '^~(/.*)?$') { $path = $HOME + $Matches[1] }
    return $path -replace '/', '\'
}

function Resolve-HtmlOpenTarget([string]$command, [string]$workingDirectory) {
    $match = [regex]::Match($command, '^\s*start(?:\s+(?:""|''''))?\s+(?:"(?<path>[^"]+)"|''(?<path>[^'']+)''|(?<path>[^\s"''&|;<>`$]+))\s*$', 'IgnoreCase')
    if (-not $match.Success -or $match.Groups['path'].Value -notmatch '\.html?$') { return $null }
    try {
        $target = ConvertFrom-BashPath $match.Groups['path'].Value
        if (-not [System.IO.Path]::IsPathRooted($target)) {
            if ([string]::IsNullOrWhiteSpace($workingDirectory)) { return $null }
            $target = [System.IO.Path]::Combine((ConvertFrom-BashPath $workingDirectory), $target)
        }
        $target = [System.IO.Path]::GetFullPath($target)
    } catch { return $null }
    if (-not (Test-Path -LiteralPath $target -PathType Leaf)) { return $null }
    return $target
}

function Submit-PreviewRequest([string]$path) {
    $requests = Join-Path $dataDirectory 'previews'
    New-Item -ItemType Directory -Path $requests -Force -ErrorAction Stop | Out-Null
    $name = "$paneId-$([guid]::NewGuid().ToString('N'))"
    $temporary = Join-Path $requests "$name.tmp"
    $json = @{ pane = $paneId; path = $path } | ConvertTo-Json -Compress
    [System.IO.File]::WriteAllText($temporary, $json, [System.Text.UTF8Encoding]::new($false))
    Move-Item -LiteralPath $temporary -Destination (Join-Path $requests "$name.json") -Force -ErrorAction Stop
}

function Write-HookOutput($output) {
    $bytes = [System.Text.UTF8Encoding]::new($false).GetBytes(($output | ConvertTo-Json -Compress -Depth 4))
    $stdout = [Console]::OpenStandardOutput()
    $stdout.Write($bytes, 0, $bytes.Length)
    $stdout.Flush()
}

if ($eventName -eq 'PreToolUse' -and $hook.tool_name -eq 'Bash') {
    $target = Resolve-HtmlOpenTarget ([string]$hook.tool_input.command) ([string]$hook.cwd)
    if (-not $target) { exit 0 }
    try { Submit-PreviewRequest $target } catch { exit 0 }
    $reason = "Tily a ouvert $target dans son aperçu HTML, à côté du terminal : l’utilisateur le relit dans Tily, inutile de l’ouvrir autrement."
    Write-HookOutput @{ hookSpecificOutput = @{ hookEventName = 'PreToolUse'; permissionDecision = 'deny'; permissionDecisionReason = $reason } }
    exit 0
}

function Get-ToolDetail($toolInput) {
    if ($null -eq $toolInput) { return $null }
    if ($toolInput.questions) { return [string]@($toolInput.questions)[0].question }
    foreach ($name in 'command', 'file_path', 'notebook_path', 'url', 'query', 'pattern', 'description') {
        $property = $toolInput.PSObject.Properties[$name]
        if ($property -and -not [string]::IsNullOrWhiteSpace([string]$property.Value)) { return [string]$property.Value }
    }
    return $toolInput | ConvertTo-Json -Compress -Depth 4
}

function Get-LastAssistantText($hook) {
    if (-not [string]::IsNullOrWhiteSpace([string]$hook.last_assistant_message)) { return [string]$hook.last_assistant_message }
    $transcript = [string]$hook.transcript_path
    if ([string]::IsNullOrWhiteSpace($transcript) -or -not (Test-Path -LiteralPath $transcript)) { return $null }
    $lines = @(Get-Content -LiteralPath $transcript -Tail 200 -Encoding UTF8 | Where-Object { $_ -like '*"type":"assistant"*' })
    for ($index = $lines.Count - 1; $index -ge 0; $index--) {
        try { $entry = $lines[$index] | ConvertFrom-Json } catch { continue }
        if ($entry.type -ne 'assistant' -or $entry.isSidechain) { continue }
        $texts = @($entry.message.content | Where-Object { $_.type -eq 'text' -and $_.text } | ForEach-Object { $_.text })
        if ($texts.Count -gt 0) { return $texts -join ' ' }
    }
    return $null
}

$waitingNotifications = @('permission_prompt', 'elicitation_dialog', 'agent_needs_input')
$state = $null
$message = $null
$detail = $null
switch ($eventName) {
    'SessionStart' { $state = 'unknown' }
    'UserPromptSubmit' { $state = 'working' }
    'PreToolUse' { if ($hook.tool_name -eq 'AskUserQuestion') { $state = 'waiting'; $message = 'Question posée.'; $detail = Get-ToolDetail $hook.tool_input } }
    'PostToolUse' { $state = 'working' }
    'Stop' { $state = 'done'; $detail = Get-LastAssistantText $hook }
    'StopFailure' { $state = 'error'; $message = 'Erreur signalée par Claude Code.' }
    'PermissionRequest' { $state = 'waiting'; $message = "Autorisation demandée : $($hook.tool_name)"; $detail = Get-ToolDetail $hook.tool_input }
    'Notification' {
        if ($hook.notification_type -in $waitingNotifications) {
            if (Test-Path -LiteralPath $file) {
                try { if ((Get-Content -LiteralPath $file -Raw -Encoding UTF8 | ConvertFrom-Json).state -eq 'waiting') { exit 0 } } catch { }
            }
            $state = 'waiting'
            $message = if ($hook.notification_type -eq 'permission_prompt') { 'Autorisation demandée.' } else { 'Saisie attendue.' }
        }
    }
}
if (-not $state) { exit 0 }

New-Item -ItemType Directory -Path $directory -Force | Out-Null
$json = @{ agent = 'claude'; state = $state; message = $message; detail = $detail } | ConvertTo-Json -Compress
[System.IO.File]::WriteAllText($file, $json, [System.Text.UTF8Encoding]::new($false))
