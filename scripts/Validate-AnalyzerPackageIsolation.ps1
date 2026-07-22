param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $repoRoot "tests/Swa.Analyzers.PackageValidation.Tests/Swa.Analyzers.PackageValidation.Tests.csproj"

dotnet test $testProject --configuration $Configuration --no-build
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Analyzer package isolation validated."
