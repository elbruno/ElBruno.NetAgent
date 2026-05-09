<#
Smoke test for local NuGet package: ElBruno.NetAgent.0.1.0.nupkg
Checks (no network):
 - package file exists
 - package entries can be listed
 - README.md exists inside package
 - elbruno-netagent-icon.png exists inside package
 - .nuspec contains <icon> metadata
 - a DLL or EXE entry can be identified (app assembly)
 - inspects repo source for "dry" occurrences to help confirm dry-run default

Usage:
  .\smoke-test-local-package.ps1 [-Package <path-to-nupkg>]

This script performs inspection only; it does not install or publish anything.
#>
param(
    [string]$Package = (Join-Path $PSScriptRoot "..\artifacts\ElBruno.NetAgent.0.1.0.nupkg"),
    [switch]$VerboseMode
)

function Write-Ok($msg){ Write-Host "[OK]    $msg" -ForegroundColor Green }
function Write-Warn($msg){ Write-Host "[WARN]  $msg" -ForegroundColor Yellow }
function Write-Err($msg){ Write-Host "[ERROR] $msg" -ForegroundColor Red }

if (-not (Test-Path $Package)) {
    Write-Err "Package not found at: $Package"
    exit 2
}

Write-Host "Inspecting package: $Package"
[Reflection.Assembly]::LoadWithPartialName('System.IO.Compression.FileSystem') | Out-Null
$zip = [System.IO.Compression.ZipFile]::OpenRead($Package)
$entries = $zip.Entries | ForEach-Object { $_.FullName }

# helper for case-insensitive contains
function HasEntry($pattern){
    return $entries -match [regex]::Escape($pattern)
}

# 1) README.md
$hasReadme = $entries | Where-Object { $_ -match '(?i)README\.md$' }
if ($hasReadme) { Write-Ok "README.md found in package (examples: $($hasReadme -join ', '))" } else { Write-Warn "README.md not found in package" }

# 2) icon PNG
$iconName = 'elbruno-netagent-icon.png'
$hasIcon = $entries | Where-Object { $_ -match (("(?i)" + [regex]::Escape($iconName) + "$")) }
if ($hasIcon) { Write-Ok "Icon '$iconName' found in package (examples: $($hasIcon -join ', '))" } else { Write-Warn "Icon '$iconName' not found in package" }

# 3) nuspec and icon metadata
$nuspecEntry = $zip.Entries | Where-Object { $_.FullName -match '\.nuspec$' } | Select-Object -First 1
if ($nuspecEntry) {
    Write-Host "Found nuspec: $($nuspecEntry.FullName)"
    $s = $nuspecEntry.Open()
    $sr = New-Object System.IO.StreamReader($s)
    $nuspecXml = $sr.ReadToEnd()
    $sr.Close(); $s.Close()
    try {
        $xml = [xml]$nuspecXml
        $iconNode = $xml.package.metadata.icon
        if ($iconNode -and $iconNode.Trim()) { Write-Ok "<icon> metadata present in nuspec: '$($iconNode.Trim())'" } else { Write-Warn "<icon> metadata missing or empty in nuspec" }
    } catch {
        Write-Warn "Failed to parse nuspec XML: $($_.Exception.Message)"
    }
} else {
    Write-Warn "No .nuspec file found inside package"
}

# 4) find DLLs/EXEs
$appEntries = $entries | Where-Object { $_ -match '\.dll$' -or $_ -match '\.exe$' }
if ($appEntries) { Write-Ok "Binary entries found: $($appEntries -join ', ')" } else { Write-Warn "No .dll or .exe entries found inside package" }

# 5) inspect repo source for 'dry' to help confirm default remains dry-run
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Write-Host "Searching source files for 'dry' (case-insensitive) to help check default mode" -ForegroundColor Cyan
$matches = Get-ChildItem -Path $repoRoot -Include *.cs,*.json -Recurse -ErrorAction SilentlyContinue | Select-String -Pattern 'dry' -CaseSensitive:$false
if ($matches) {
    Write-Ok "Found code or config mentioning 'dry' (showing up to 10 matches):"
    $matches | Select-Object -First 10 | ForEach-Object { Write-Host "  $_" }
} else {
    Write-Warn "No occurrences of 'dry' found in source. Manual review recommended to confirm dry-run default." 
}

# 6) Dry-run / Live-mode notes
Write-Host "Verification notes:" -ForegroundColor Cyan
Write-Host " - This script performs static package inspection only (dry-run)." -ForegroundColor White
Write-Host " - If the codebase exposes a runtime 'live' mode (flag/option), ensure it remains opt-in and blocked by default. This tool does not change runtime behavior." -ForegroundColor White

# summary / exit condition
$warnings = @()
if (-not $hasReadme) { $warnings += 'missing-readme' }
if (-not $hasIcon) { $warnings += 'missing-icon' }
if (-not $nuspecEntry) { $warnings += 'missing-nuspec' }
if ($nuspecEntry -and (-not $iconNode)) { $warnings += 'nuspec-missing-icon-metadata' }
if (-not $appEntries) { $warnings += 'missing-binary' }

if ($warnings.Count -eq 0) {
    Write-Host "\nAll automated checks passed (inspection only)." -ForegroundColor Green
    exit 0
} else {
    Write-Warn "\nAutomated checks completed with warnings: $($warnings -join ', ')"
    exit 1
}
