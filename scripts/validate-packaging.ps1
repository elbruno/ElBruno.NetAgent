# validate-packaging.ps1
# Phase 9: Packaging Validation Script
# Purpose: Validate that the project produces a well-formed .nupkg
# Constraints: No publish, no external mutations, dry-run only

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir

Write-Host "=== ElBruno.NetAgent - Packaging Validation ===" -ForegroundColor Cyan
Write-Host ""

# 1. Validate project file exists
$csproj = Join-Path $repoRoot "src\ElBruno.NetAgent\ElBruno.NetAgent.csproj"
if (-not (Test-Path $csproj)) {
    Write-Host "ERROR: Project file not found: $csproj" -ForegroundColor Red
    exit 1
}
Write-Host "[OK] Project file exists" -ForegroundColor Green

# 2. Validate package metadata in .csproj
$csprojContent = Get-Content $csproj -Raw
$requiredFields = @(
    "PackageId",
    "Title",
    "Description",
    "Authors",
    "RepositoryUrl",
    "PackageLicenseExpression",
    "PackageTags",
    "Version"
)

$allFieldsPresent = $true
foreach ($field in $requiredFields) {
    if ($csprojContent -match "<$field>") {
        Write-Host "[OK] Metadata field present: $field" -ForegroundColor Green
    } else {
        Write-Host "[MISSING] Metadata field missing: $field" -ForegroundColor Yellow
        $allFieldsPresent = $false
    }
}

if (-not $allFieldsPresent) {
    Write-Host ""
    Write-Host "WARNING: Some metadata fields are missing. Add them to the .csproj file." -ForegroundColor Yellow
}

Write-Host ""

# 3. Build
Write-Host "=== Building ===" -ForegroundColor Cyan
$buildResult = dotnet build $repoRoot\ElBruno.NetAgent.sln --configuration Release 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed" -ForegroundColor Red
    Write-Host $buildResult
    exit 1
}
Write-Host "[OK] Build succeeded" -ForegroundColor Green
Write-Host ""

# 4. Test
Write-Host "=== Testing ===" -ForegroundColor Cyan
$testResult = dotnet test $repoRoot\ElBruno.NetAgent.sln --configuration Release --no-build 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Tests failed" -ForegroundColor Red
    Write-Host $testResult
    exit 1
}
Write-Host "[OK] Tests passed" -ForegroundColor Green
Write-Host ""

# 5. Pack (no-build since we already built)
Write-Host "=== Packing ===" -ForegroundColor Cyan
$packResult = dotnet pack $csproj --configuration Release --no-build --output (Join-Path $repoRoot "artifacts") 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Pack failed" -ForegroundColor Red
    Write-Host $packResult
    exit 1
}

# 6. Find and display the generated .nupkg
$nupkgPath = Get-ChildItem (Join-Path $repoRoot "artifacts") -Filter "*.nupkg" -Recurse | Select-Object -First 1
if ($nupkgPath) {
    $fileSize = (Get-Item $nupkgPath.FullName).Length
    Write-Host "[OK] Pack succeeded" -ForegroundColor Green
    Write-Host ""
    Write-Host "=== Generated Package ===" -ForegroundColor Cyan
    Write-Host "Path:    $($nupkgPath.FullName)" -ForegroundColor White
    Write-Host "Size:    $([math]::Round($fileSize / 1KB, 2)) KB" -ForegroundColor White
    Write-Host "Name:    $($nupkgPath.BaseName)" -ForegroundColor White
    Write-Host ""
    Write-Host "=== Packaging Validation Complete ===" -ForegroundColor Cyan
    Write-Host "Result:  SUCCESS" -ForegroundColor Green
} else {
    Write-Host "ERROR: No .nupkg file found after pack" -ForegroundColor Red
    exit 1
}
