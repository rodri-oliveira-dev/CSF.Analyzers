param(
    [string]$Configuration = "Release",
    [string]$PackageDirectory = "artifacts/packages",
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $repoRoot "tests/Swa.Analyzers.PackageValidation.Tests/Swa.Analyzers.PackageValidation.Tests.csproj"

dotnet test $testProject --configuration $Configuration --no-build
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if (-not [string]::IsNullOrWhiteSpace($Version)) {
    & (Join-Path $PSScriptRoot "Inspect-NuGetPackages.ps1") -PackageDirectory $PackageDirectory -Version $Version
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

Write-Host "Analyzer package isolation validated."
