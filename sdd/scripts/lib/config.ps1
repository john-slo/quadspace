# =====================================================================
# config.ps1 - shared helpers (config, .env, text I/O, slug).
# Windows PowerShell 5.1 compatible. Dot-source this file; it defines
# functions only and has no side effects on load.
#   . (Join-Path $PSScriptRoot 'config.ps1')
# =====================================================================

Set-StrictMode -Version Latest

function Read-SddText {
    param([Parameter(Mandatory = $true)][string]$Path)
    return [System.IO.File]::ReadAllText($Path)
}

function Write-SddText {
    # Writes UTF-8 WITHOUT BOM so files are clean for git and System.Text.Json.
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Text
    )
    $dir = Split-Path -Parent $Path
    if ($dir -and -not (Test-Path -LiteralPath $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    $enc = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Text, $enc)
}

function ConvertTo-SddJson {
    param(
        [Parameter(Mandatory = $true)][AllowNull()]$Object,
        [int]$Depth = 12
    )
    return ($Object | ConvertTo-Json -Depth $Depth)
}

function Get-SddConfig {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { throw "SDD config not found: $Path" }
    $raw = [System.IO.File]::ReadAllText($Path)
    return ($raw | ConvertFrom-Json)
}

function Get-SddSlug {
    param([Parameter(Mandatory = $true)][string]$Title)
    $s = $Title.ToLowerInvariant()
    $s = [System.Text.RegularExpressions.Regex]::Replace($s, '[^a-z0-9]+', '-')
    $s = $s.Trim('-')
    if ($s.Length -gt 50) { $s = $s.Substring(0, 50).Trim('-') }
    if ([string]::IsNullOrWhiteSpace($s)) { $s = 'feature' }
    return $s
}

function Get-SddTimestampUtc {
    return [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
}
