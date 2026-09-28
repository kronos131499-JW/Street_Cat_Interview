$ErrorActionPreference = 'Stop'

$configPath = Join-Path $env:USERPROFILE '.codex\config.toml'
if (-not (Test-Path -LiteralPath $configPath)) {
    throw "Codex config not found: $configPath"
}

$codexCommand = Get-Command codex.exe -ErrorAction SilentlyContinue
if ($codexCommand) {
    $codexExe = $codexCommand.Source
} else {
    $codexBinRoot = Join-Path $env:LOCALAPPDATA 'OpenAI\Codex\bin'
    $codexExe = Get-ChildItem -LiteralPath $codexBinRoot -Filter codex.exe -File -Recurse -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}
if (-not $codexExe -or -not (Test-Path -LiteralPath $codexExe)) {
    throw 'Codex executable was not found. Open Codex Desktop and rerun this script.'
}

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupPath = "$configPath.backup-$timestamp-stream-fix"
Copy-Item -LiteralPath $configPath -Destination $backupPath -ErrorAction Stop

$content = Get-Content -Raw -LiteralPath $configPath
$content = [regex]::Replace(
    $content,
    '(?m)^model_reasoning_effort\s*=\s*"[^"]*"\s*$',
    'model_reasoning_effort = "high"'
)

$providerPattern = '(?ms)(^\[model_providers\.litellm\]\s*$)(.*?)(?=^\[|\z)'
$providerMatch = [regex]::Match($content, $providerPattern)
if (-not $providerMatch.Success) {
    throw 'The [model_providers.litellm] section was not found; no changes were applied.'
}

$providerBody = $providerMatch.Groups[2].Value
$settings = [ordered]@{
    # Fail over promptly instead of leaving the UI in a long reconnect loop.
    request_max_retries    = '4'
    stream_max_retries     = '3'
    stream_idle_timeout_ms = '180000'
}

foreach ($entry in $settings.GetEnumerator()) {
    $linePattern = '(?m)^' + [regex]::Escape($entry.Key) + '\s*=.*$'
    $line = $entry.Key + ' = ' + $entry.Value
    if ([regex]::IsMatch($providerBody, $linePattern)) {
        $providerBody = [regex]::Replace($providerBody, $linePattern, $line)
    } else {
        $providerBody = $providerBody.TrimEnd() + "`r`n" + $line + "`r`n"
    }
}

$replacement = $providerMatch.Groups[1].Value + "`r`n" + $providerBody.TrimStart("`r", "`n")
$updated = $content.Substring(0, $providerMatch.Index) +
    $replacement +
    $content.Substring($providerMatch.Index + $providerMatch.Length)

try {
    Set-Content -LiteralPath $configPath -Value $updated -Encoding utf8 -NoNewline

    $check = & $codexExe --strict-config --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Codex rejected the updated config: $($check -join [Environment]::NewLine)"
    }
} catch {
    Copy-Item -LiteralPath $backupPath -Destination $configPath -Force
    throw "Update failed and the original config was restored. $($_.Exception.Message)"
}

Write-Host 'Codex stream resilience settings updated.' -ForegroundColor Green
Write-Host "Backup: $backupPath"
Write-Host 'Restart Codex Desktop to apply the new settings.'
