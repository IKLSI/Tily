$paneId = $env:DOCK_PANE_ID
if ([string]::IsNullOrWhiteSpace($paneId)) { exit 0 }

$raw = [System.IO.StreamReader]::new([Console]::OpenStandardInput(), [System.Text.UTF8Encoding]::new($false)).ReadToEnd()
try { $hook = $raw | ConvertFrom-Json } catch { exit 0 }

$dataDirectory = if ([string]::IsNullOrWhiteSpace($env:DOCK_DATA_DIR)) { Join-Path $env:LOCALAPPDATA 'Dock' } else { $env:DOCK_DATA_DIR }
$directory = Join-Path $dataDirectory 'agents'
$file = Join-Path $directory "$paneId.json"
$eventName = [string]$hook.hook_event_name

if ($eventName -eq 'SessionEnd') {
    Remove-Item -LiteralPath $file -Force -ErrorAction SilentlyContinue
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
