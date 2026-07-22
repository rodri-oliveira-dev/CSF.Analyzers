<#
.SYNOPSIS
Validates release consistency for the active Swa.Analyzers packages.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = & git rev-parse --show-toplevel
if (-not $repoRoot) {
    throw "release-check: nao foi possivel localizar a raiz do repositorio."
}

$repoRoot = ($repoRoot | Select-Object -First 1)
$failures = New-Object System.Collections.Generic.List[string]

function Add-Failure {
    param([string]$Message)
    $script:failures.Add($Message)
}

function Get-ArchIds {
    param([string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return @()
    }

    return [regex]::Matches($Text, "ARCH\d{3}") |
        ForEach-Object { $_.Value.ToUpperInvariant() } |
        Sort-Object -Unique
}

$packages = @(
    @{
        Name = "Swa.Analyzers.Reliability"
        TestProject = "tests/Swa.Analyzers.Reliability.Tests"
        SampleProject = "samples/Swa.Analyzers.Reliability.Sample"
        RuleIds = @("ARCH016", "ARCH017", "ARCH021", "ARCH022")
    },
    @{
        Name = "Swa.Analyzers.Architecture"
        TestProject = "tests/Swa.Analyzers.Architecture.Tests"
        SampleProject = "samples/Swa.Analyzers.Architecture.Sample"
        RuleIds = @("ARCH015", "ARCH020", "ARCH027", "ARCH029", "ARCH032")
    },
    @{
        Name = "Swa.Analyzers.Testing"
        TestProject = "tests/Swa.Analyzers.Testing.Tests"
        SampleProject = "samples/Swa.Analyzers.Testing.Sample"
        RuleIds = @("ARCH005", "ARCH006")
    }
)

$readmePath = Join-Path $repoRoot "README.md"
$readmeContent = if (Test-Path -LiteralPath $readmePath) { Get-Content -Raw -LiteralPath $readmePath } else { "" }
if ([string]::IsNullOrWhiteSpace($readmeContent)) {
    Add-Failure "README.md nao foi encontrado ou esta vazio."
}

$seenIds = @{}

foreach ($package in $packages) {
    $projectRoot = Join-Path $repoRoot "src/$($package.Name)"
    $rulesRoot = Join-Path $projectRoot "Rules"
    $identifiersPath = Join-Path $projectRoot "RuleIdentifiers.cs"
    $shippedPath = Join-Path $projectRoot "AnalyzerReleases.Shipped.md"
    $unshippedPath = Join-Path $projectRoot "AnalyzerReleases.Unshipped.md"

    foreach ($requiredPath in @($rulesRoot, $identifiersPath, $shippedPath, $unshippedPath)) {
        if (-not (Test-Path -LiteralPath $requiredPath)) {
            Add-Failure "$($package.Name): caminho obrigatorio ausente: $requiredPath"
        }
    }

    $identifierContent = if (Test-Path -LiteralPath $identifiersPath) { Get-Content -Raw -LiteralPath $identifiersPath } else { "" }
    $shippedContent = if (Test-Path -LiteralPath $shippedPath) { Get-Content -Raw -LiteralPath $shippedPath } else { "" }
    $unshippedContent = if (Test-Path -LiteralPath $unshippedPath) { Get-Content -Raw -LiteralPath $unshippedPath } else { "" }
    $metadataIds = @(Get-ArchIds ($shippedContent + [Environment]::NewLine + $unshippedContent))
    $identifierIds = @(Get-ArchIds $identifierContent)

    foreach ($ruleId in $package.RuleIds) {
        $number = $ruleId.Substring(4)

        if ($seenIds.ContainsKey($ruleId)) {
            Add-Failure "$ruleId aparece em mais de um pacote: $($seenIds[$ruleId]) e $($package.Name)."
        }

        $seenIds[$ruleId] = $package.Name

        if ($identifierIds -notcontains $ruleId) {
            Add-Failure "$($package.Name): RuleIdentifiers.cs nao contem $ruleId."
        }

        if ($metadataIds -notcontains $ruleId) {
            Add-Failure "$($package.Name): metadata shipped/unshipped nao contem $ruleId."
        }

        if (-not (Test-Path -LiteralPath (Join-Path $rulesRoot "Arch$number*"))) {
            $matchingAnalyzers = @(Get-ChildItem -LiteralPath $rulesRoot -Filter "Arch$number*.cs" -File -ErrorAction SilentlyContinue)
            if ($matchingAnalyzers.Count -eq 0) {
                Add-Failure "$($package.Name): analyzer ausente para $ruleId."
            }
        }

        if (-not (Test-Path -LiteralPath (Join-Path $repoRoot "docs/rules/$ruleId.md"))) {
            Add-Failure "${ruleId}: documentacao ausente em docs/rules/$ruleId.md."
        }

        $matchingTests = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot "$($package.TestProject)/Rules") -Filter "Arch$number*Tests.cs" -File -ErrorAction SilentlyContinue)
        if ($matchingTests.Count -eq 0) {
            Add-Failure "${ruleId}: teste ausente em $($package.TestProject)/Rules."
        }

        if (-not (Test-Path -LiteralPath (Join-Path $repoRoot "$($package.SampleProject)/Arch$number") -PathType Container)) {
            Add-Failure "${ruleId}: sample ausente em $($package.SampleProject)/Arch$number."
        }

        if ($readmeContent -notmatch [regex]::Escape($ruleId)) {
            Add-Failure "${ruleId}: README.md nao contem entrada correspondente."
        }
    }

    foreach ($unexpectedId in $identifierIds | Where-Object { $_ -notin $package.RuleIds }) {
        Add-Failure "$($package.Name): RuleIdentifiers.cs contem ID inesperado $unexpectedId."
    }
}

if ($failures.Count -gt 0) {
    Write-Host "release-check: falhou com $($failures.Count) problema(s):" -ForegroundColor Red
    foreach ($failure in $failures) {
        Write-Host "- $failure" -ForegroundColor Red
    }

    exit 1
}

Write-Host "release-check: validacoes aprovadas"
exit 0
