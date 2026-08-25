# =====================================================================
# build-and-test.ps1 - run the configured build / test / format commands.
# Reads process.config.json > stack.* (defaults to .NET) so the wrapper works
# for any stack, and performs two Windows preflights (release locked build
# outputs, normalise line endings) that the agent would otherwise forget.
# Emits a single JSON object on stdout with structured results.
# Windows PowerShell 5.1 compatible.
#   pwsh -File build-and-test.ps1 -Action All
# =====================================================================
[CmdletBinding()]
param(
    [ValidateSet('Build', 'Test', 'Format', 'All')][string]$Action = 'All'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
. (Join-Path $PSScriptRoot 'config.ps1')

# --- Resolve stack commands from config (defaults target .NET/Blazor) ----------------------------
$DefaultStack = @{
    kind          = 'dotnet'
    solution      = ''
    buildCommand  = 'dotnet build {solution} -c Release'
    testCommand   = 'dotnet test {solution} -c Release'
    formatCommand = 'dotnet format {solution} --verify-no-changes'
}

function Get-StackConfig {
    $cfgPath = Join-Path (Get-Location).Path 'process.config.json'
    $stack = $DefaultStack.Clone()
    if (Test-Path -LiteralPath $cfgPath) {
        try {
            $cfg = Get-SddConfig -Path $cfgPath
            if ($cfg.PSObject.Properties.Name -contains 'stack' -and $cfg.stack) {
                foreach ($k in @('kind', 'solution', 'buildCommand', 'testCommand', 'formatCommand')) {
                    if ($cfg.stack.PSObject.Properties.Name -contains $k -and $null -ne $cfg.stack.$k -and "$($cfg.stack.$k)" -ne '') {
                        $stack[$k] = [string]$cfg.stack.$k
                    }
                }
                # solution may legitimately be empty; take it verbatim if the property exists
                if ($cfg.stack.PSObject.Properties.Name -contains 'solution') {
                    $stack['solution'] = [string]$cfg.stack.solution
                }
            }
        }
        catch { }  # fall back to defaults on any parse error
    }
    return $stack
}

function Resolve-Command {
    param([string]$Template, [string]$Solution)
    if ([string]::IsNullOrWhiteSpace($Solution)) {
        $cmd = $Template.Replace('{solution}', '')
    }
    else {
        $cmd = $Template.Replace('{solution}', '"' + $Solution + '"')
    }
    # collapse any double spaces left by an empty {solution}
    while ($cmd.Contains('  ')) { $cmd = $cmd.Replace('  ', ' ') }
    return $cmd.Trim()
}

# --- Preflight: release locked build outputs (Windows) -------------------------------------------
# App hosts / IDEs can hold *.dll outputs open (MSB3021/MSB3027). Stop configured host processes by
# PID before building. Inert unless build.lockingProcesses is set. Never writes to stdout.
function Invoke-LockPreflight {
    try {
        $cfgPath = Join-Path (Get-Location).Path 'process.config.json'
        if (-not (Test-Path -LiteralPath $cfgPath)) { return }
        $cfg = Get-SddConfig -Path $cfgPath
        $names = @()
        if ($cfg.PSObject.Properties.Name -contains 'build' -and
            $cfg.build.PSObject.Properties.Name -contains 'lockingProcesses') {
            $names = @($cfg.build.lockingProcesses)
        }
        foreach ($n in $names) {
            if ([string]::IsNullOrWhiteSpace($n)) { continue }
            Get-Process -Name $n -ErrorAction SilentlyContinue |
                ForEach-Object { Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue }
        }
    }
    catch { }  # preflight is best-effort; never fail the build over it
}

# --- Normalise changed + untracked source files to LF before a format check ----------------------
function Invoke-LineEndingNormalisation {
    try {
        $tracked = @(& git diff --name-only HEAD 2>$null)
        $untracked = @(& git ls-files --others --exclude-standard 2>$null)
        foreach ($f in (@($tracked + $untracked) | Sort-Object -Unique)) {
            if ([string]::IsNullOrWhiteSpace($f)) { continue }
            if ((Test-Path -LiteralPath $f) -and ($f -match '\.(cs|razor|json|md|csproj|props|targets|ps1|yml)$')) {
                $c = [IO.File]::ReadAllText($f)
                if ($c.Contains("`r`n")) { [IO.File]::WriteAllText($f, $c.Replace("`r`n", "`n")) }
            }
        }
    }
    catch { }  # normalisation is best-effort
}

function Invoke-Step {
    param([string]$Name, [string]$CommandLine)
    $raw = & cmd.exe /c $CommandLine 2>&1
    $code = $LASTEXITCODE
    $text = ($raw | Out-String)
    $errors = New-Object System.Collections.Generic.List[string]
    foreach ($m in [regex]::Matches($text, '(?m)^.*?error\s+[A-Za-z]{0,4}\d+:.*$')) {
        $line = $m.Value.Trim()
        if (-not $errors.Contains($line)) { $errors.Add($line) }
    }
    return [pscustomobject]@{
        step       = $Name
        command    = $CommandLine
        ok         = ($code -eq 0)
        exitCode   = $code
        errorCount = $errors.Count
        errors     = $errors.ToArray()
    }
}

$stack = Get-StackConfig
$steps = New-Object System.Collections.Generic.List[object]

if ($Action -eq 'Build' -or $Action -eq 'Test' -or $Action -eq 'All') {
    Invoke-LockPreflight
}

if ($Action -eq 'Build' -or $Action -eq 'All') {
    $steps.Add((Invoke-Step -Name 'build' -CommandLine (Resolve-Command -Template $stack.buildCommand -Solution $stack.solution)))
}
if ($Action -eq 'Test' -or $Action -eq 'All') {
    $last = if ($steps.Count -gt 0) { $steps[$steps.Count - 1] } else { $null }
    if ($null -eq $last -or $last.ok) {
        $steps.Add((Invoke-Step -Name 'test' -CommandLine (Resolve-Command -Template $stack.testCommand -Solution $stack.solution)))
    }
}
if ($Action -eq 'Format' -or $Action -eq 'All') {
    Invoke-LineEndingNormalisation
    $steps.Add((Invoke-Step -Name 'format' -CommandLine (Resolve-Command -Template $stack.formatCommand -Solution $stack.solution)))
}

$ok = $true
foreach ($s in $steps) { if (-not $s.ok) { $ok = $false } }

$result = [pscustomobject]@{
    ok       = $ok
    action   = $Action
    stack    = $stack.kind
    steps    = $steps.ToArray()
}

Write-Output (ConvertTo-SddJson -Object $result)
if (-not $ok) { exit 1 }
