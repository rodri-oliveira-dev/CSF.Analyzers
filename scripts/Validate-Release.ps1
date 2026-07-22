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

function Read-Text {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return ""
    }

    return Get-Content -Raw -LiteralPath $Path
}

function Get-RuleIds {
    param(
        [string]$Text,
        [switch]$IncludeHistorical
    )

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return @()
    }

    $pattern = if ($IncludeHistorical) { "(REL|ARC|TST|ARCH)\d{3}" } else { "(REL|ARC|TST)\d{3}" }
    return [regex]::Matches($Text, $pattern) |
        ForEach-Object { $_.Value.ToUpperInvariant() }
}

function Get-UniqueRuleIds {
    param([string]$Text)

    return @(Get-RuleIds $Text | Sort-Object -Unique)
}

function Get-RuleFilePrefix {
    param([string]$RuleId)

    $prefix = $RuleId.Substring(0, 3).ToLowerInvariant()
    return [char]::ToUpperInvariant($prefix[0]) + $prefix.Substring(1)
}

function Get-RulePackage {
    param([string]$RuleId)

    switch -Regex ($RuleId) {
        "^REL\d{3}$" { return "Swa.Analyzers.Reliability" }
        "^ARC\d{3}$" { return "Swa.Analyzers.Architecture" }
        "^TST\d{3}$" { return "Swa.Analyzers.Testing" }
        default { return $null }
    }
}

function Get-MetadataRuleIds {
    param([string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return @()
    }

    return [regex]::Matches($Text, "^(REL|ARC|TST)\d{3}\s*\|", [System.Text.RegularExpressions.RegexOptions]::Multiline) |
        ForEach-Object { $_.Value.Split("|")[0].Trim().ToUpperInvariant() }
}

function Get-DocumentedOptions {
    param([string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return @()
    }

    return [regex]::Matches($Text, "dotnet_diagnostic\.(REL|ARC|TST)\d{3}\.[A-Za-z_][A-Za-z0-9_]*") |
        ForEach-Object { $_.Value } |
        Where-Object { $_ -notmatch "\.severity$" } |
        Sort-Object -Unique
}

function Get-ImplementedOptions {
    param([string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return @()
    }

    return [regex]::Matches($Text, '"dotnet_diagnostic\.(REL|ARC|TST)\d{3}\.[A-Za-z_][A-Za-z0-9_]*"') |
        ForEach-Object { $_.Value.Trim('"') } |
        Where-Object { $_ -notmatch "\.severity$" } |
        Sort-Object -Unique
}

$packages = @(
    @{
        Name = "Swa.Analyzers.Reliability"
        Prefix = "REL"
        TestProject = "tests/Swa.Analyzers.Reliability.Tests"
        SampleProject = "samples/Swa.Analyzers.Reliability.Sample"
        RuleIds = @("REL001", "REL002", "REL003", "REL004")
    },
    @{
        Name = "Swa.Analyzers.Architecture"
        Prefix = "ARC"
        TestProject = "tests/Swa.Analyzers.Architecture.Tests"
        SampleProject = "samples/Swa.Analyzers.Architecture.Sample"
        RuleIds = @("ARC001", "ARC002", "ARC003", "ARC004", "ARC005")
    },
    @{
        Name = "Swa.Analyzers.Testing"
        Prefix = "TST"
        TestProject = "tests/Swa.Analyzers.Testing.Tests"
        SampleProject = "samples/Swa.Analyzers.Testing.Sample"
        RuleIds = @("TST001", "TST002")
    }
)

$readmePath = Join-Path $repoRoot "README.md"
$readmeContent = Read-Text $readmePath
if ([string]::IsNullOrWhiteSpace($readmeContent)) {
    Add-Failure "README.md nao foi encontrado ou esta vazio."
}

$allExpectedIds = @($packages | ForEach-Object { $_.RuleIds })
$allSeenIds = @{}
$allIdentifierIds = New-Object System.Collections.Generic.List[string]
$allAnalyzerIds = New-Object System.Collections.Generic.List[string]
$allMetadataIds = New-Object System.Collections.Generic.List[string]
$allActiveSourceText = ""
$expectedOptions = New-Object System.Collections.Generic.HashSet[string]([StringComparer]::Ordinal)
$implementedOptions = New-Object System.Collections.Generic.HashSet[string]([StringComparer]::Ordinal)

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

    $identifierContent = Read-Text $identifiersPath
    $shippedContent = Read-Text $shippedPath
    $unshippedContent = Read-Text $unshippedPath
    $metadataIdsInShipped = @(Get-MetadataRuleIds $shippedContent)
    $metadataIdsInUnshipped = @(Get-MetadataRuleIds $unshippedContent)
    $metadataIds = @($metadataIdsInShipped + $metadataIdsInUnshipped)
    $identifierIds = @(Get-UniqueRuleIds $identifierContent)
    $ruleFiles = @(Get-ChildItem -LiteralPath $rulesRoot -Filter "*.cs" -File -ErrorAction SilentlyContinue)
    $ruleSourceText = ($ruleFiles | ForEach-Object { Read-Text $_.FullName }) -join [Environment]::NewLine
    $allActiveSourceText += [Environment]::NewLine + $identifierContent + [Environment]::NewLine + $ruleSourceText
    $analyzerIds = @(Get-UniqueRuleIds $ruleSourceText)

    $allIdentifierIds.AddRange([string[]]$identifierIds)
    $allAnalyzerIds.AddRange([string[]]$analyzerIds)
    $allMetadataIds.AddRange([string[]]$metadataIds)

    foreach ($option in @(Get-ImplementedOptions $ruleSourceText)) {
        [void]$implementedOptions.Add($option)
    }

    foreach ($id in @($identifierIds + $analyzerIds + $metadataIds | Sort-Object -Unique)) {
        $expectedPackage = Get-RulePackage $id
        if ($expectedPackage -ne $package.Name) {
            Add-Failure "$($package.Name): contem ID $id, mas o prefixo pertence a $expectedPackage."
        }
    }

    foreach ($ruleId in $package.RuleIds) {
        $number = $ruleId.Substring(3)
        $filePrefix = Get-RuleFilePrefix $ruleId

        if ($allSeenIds.ContainsKey($ruleId)) {
            Add-Failure "$ruleId aparece em mais de um pacote: $($allSeenIds[$ruleId]) e $($package.Name)."
        }

        $allSeenIds[$ruleId] = $package.Name

        if ($identifierIds -notcontains $ruleId) {
            Add-Failure "$($package.Name): RuleIdentifiers.cs nao contem $ruleId."
        }

        $metadataCount = @($metadataIds | Where-Object { $_ -eq $ruleId }).Count
        if ($metadataCount -ne 1) {
            Add-Failure "$($package.Name): $ruleId deve aparecer em exatamente um arquivo shipped/unshipped; encontrado $metadataCount vez(es)."
        }

        if (($metadataIdsInShipped -contains $ruleId) -and ($metadataIdsInUnshipped -contains $ruleId)) {
            Add-Failure "$($package.Name): $ruleId aparece simultaneamente em Shipped e Unshipped."
        }

        $matchingAnalyzers = @(Get-ChildItem -LiteralPath $rulesRoot -Filter "$filePrefix$number*.cs" -File -ErrorAction SilentlyContinue)
        if ($matchingAnalyzers.Count -eq 0) {
            Add-Failure "$($package.Name): analyzer ausente para $ruleId."
        }

        $docPath = Join-Path $repoRoot "docs/rules/$ruleId.md"
        $docContent = Read-Text $docPath
        if ([string]::IsNullOrWhiteSpace($docContent)) {
            Add-Failure "${ruleId}: documentacao ausente em docs/rules/$ruleId.md."
        } else {
            if ($docContent -notmatch [regex]::Escape($ruleId)) {
                Add-Failure "${ruleId}: documentacao nao menciona o proprio ID."
            }

            foreach ($option in @(Get-DocumentedOptions $docContent)) {
                [void]$expectedOptions.Add($option)
            }
        }

        $matchingTests = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot "$($package.TestProject)/Rules") -Filter "$filePrefix$number*Tests.cs" -File -ErrorAction SilentlyContinue)
        if ($matchingTests.Count -eq 0) {
            Add-Failure "${ruleId}: teste ausente em $($package.TestProject)/Rules."
        }

        if (-not (Test-Path -LiteralPath (Join-Path $repoRoot "$($package.SampleProject)/$filePrefix$number") -PathType Container)) {
            Add-Failure "${ruleId}: sample ausente em $($package.SampleProject)/$filePrefix$number."
        }

        if ($readmeContent -notmatch [regex]::Escape($ruleId)) {
            Add-Failure "${ruleId}: README.md nao contem entrada correspondente."
        }

        $expectedHelpLink = "RuleHelpLinks.ForRule(RuleIdentifiers."
        $matchingAnalyzerContent = ($matchingAnalyzers | ForEach-Object { Read-Text $_.FullName }) -join [Environment]::NewLine
        if ($matchingAnalyzerContent -notmatch [regex]::Escape($expectedHelpLink)) {
            Add-Failure "${ruleId}: analyzer nao usa RuleHelpLinks.ForRule(RuleIdentifiers...)."
        }
    }

    foreach ($unexpectedId in $identifierIds | Where-Object { $_ -notin $package.RuleIds }) {
        Add-Failure "$($package.Name): RuleIdentifiers.cs contem ID inesperado $unexpectedId."
    }

    foreach ($orphanMetadataId in $metadataIds | Where-Object { $_ -notin $identifierIds } | Sort-Object -Unique) {
        Add-Failure "$($package.Name): metadata shipped/unshipped contem ID orfao $orphanMetadataId."
    }

    foreach ($duplicateMetadataId in $metadataIds | Group-Object | Where-Object { $_.Count -gt 1 } | ForEach-Object { $_.Name }) {
        Add-Failure "$($package.Name): metadata shipped/unshipped contem ID duplicado $duplicateMetadataId."
    }
}

foreach ($id in @($allIdentifierIds + $allAnalyzerIds + $allMetadataIds | Sort-Object -Unique)) {
    if ($allExpectedIds -notcontains $id) {
        Add-Failure "ID ativo orfao ou inesperado encontrado: $id."
    }
}

foreach ($duplicateId in $allIdentifierIds | Group-Object | Where-Object { $_.Count -gt 1 } | ForEach-Object { $_.Name }) {
    Add-Failure "ID duplicado globalmente em RuleIdentifiers.cs: $duplicateId."
}

foreach ($duplicateId in $allMetadataIds | Group-Object | Where-Object { $_.Count -gt 1 } | ForEach-Object { $_.Name }) {
    Add-Failure "ID duplicado globalmente em metadata shipped/unshipped: $duplicateId."
}

foreach ($option in $implementedOptions) {
    if (-not $expectedOptions.Contains($option)) {
        Add-Failure "Opcao publica implementada mas nao documentada em docs/rules: $option."
    }
}

foreach ($option in $expectedOptions) {
    if (-not $implementedOptions.Contains($option)) {
        Add-Failure "Opcao publica documentada mas nao implementada: $option."
    }
}

$activeHistoricalIds = @(Get-RuleIds $allActiveSourceText -IncludeHistorical | Where-Object { $_ -like "ARCH*" } | Sort-Object -Unique)
foreach ($historicalId in $activeHistoricalIds) {
    Add-Failure "ID historico $historicalId aparece em implementacao ativa; ARCH### deve permanecer apenas como historico/documentacao."
}

$helpLinksPath = Join-Path $repoRoot "src/Swa.Analyzers.Common/RuleHelpLinks.cs"
$helpLinksContent = Read-Text $helpLinksPath
if ($helpLinksContent -notmatch "docs/rules/") {
    Add-Failure "RuleHelpLinks.cs nao aponta para docs/rules."
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
