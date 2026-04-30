<#
.SYNOPSIS
Validates release consistency for Swa.Analyzers rules and package metadata.
#>
[CmdletBinding()]
param(
    [string]$BaseRef,
    [string]$HeadRef = "HEAD"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$zeroSha = "0000000000000000000000000000000000000000"
$failures = New-Object System.Collections.Generic.List[string]

function Add-Failure {
    param([string]$Message)

    $script:failures.Add($Message)
}

function Invoke-Git {
    param([string[]]$Arguments)

    $output = & git @Arguments 2>$null
    if ($LASTEXITCODE -ne 0) {
        return $null
    }

    return $output
}

function Test-GitCommit {
    param([string]$Ref)

    if ([string]::IsNullOrWhiteSpace($Ref) -or $Ref -eq $zeroSha) {
        return $false
    }

    $null = Invoke-Git @("rev-parse", "--verify", "$Ref^{commit}")
    return $LASTEXITCODE -eq 0
}

function Resolve-BaseRef {
    if (-not [string]::IsNullOrWhiteSpace($BaseRef) -and $BaseRef -ne $zeroSha) {
        return $BaseRef
    }

    if (-not [string]::IsNullOrWhiteSpace($env:RELEASE_CHECK_BASE_REF) -and $env:RELEASE_CHECK_BASE_REF -ne $zeroSha) {
        return $env:RELEASE_CHECK_BASE_REF
    }

    if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_BASE_REF)) {
        return "origin/$($env:GITHUB_BASE_REF)"
    }

    $upstream = Invoke-Git @("rev-parse", "--abbrev-ref", "--symbolic-full-name", "@{u}")
    if ($upstream) {
        return ($upstream | Select-Object -First 1)
    }

    $originMain = "origin/main"
    if (Test-GitCommit $originMain) {
        return $originMain
    }

    return $null
}

function Get-RepositoryPath {
    param([string]$Path)

    return ($Path -replace "\\", "/")
}

function Get-CurrentContent {
    param([string]$Path)

    $fullPath = Join-Path $repoRoot $Path
    if (-not (Test-Path -LiteralPath $fullPath)) {
        return $null
    }

    return Get-Content -LiteralPath $fullPath -Raw
}

function Get-RefContent {
    param(
        [string]$Ref,
        [string]$Path
    )

    if (-not (Test-GitCommit $Ref)) {
        return $null
    }

    $gitPath = Get-RepositoryPath $Path
    $content = Invoke-Git @("show", "$Ref`:$gitPath")
    if ($null -eq $content) {
        return $null
    }

    return ($content -join [Environment]::NewLine)
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

function Get-VersionPrefix {
    param([string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $null
    }

    $match = [regex]::Match($Text, "<VersionPrefix>\s*([^<]+?)\s*</VersionPrefix>")
    if (-not $match.Success) {
        return $null
    }

    return $match.Groups[1].Value.Trim()
}

$repoRoot = Invoke-Git @("rev-parse", "--show-toplevel")
if (-not $repoRoot) {
    throw "release-check: nao foi possivel localizar a raiz do repositorio."
}

$repoRoot = ($repoRoot | Select-Object -First 1)
Set-Location $repoRoot

if (-not [string]::IsNullOrWhiteSpace($env:RELEASE_CHECK_HEAD_REF)) {
    $HeadRef = $env:RELEASE_CHECK_HEAD_REF
}

$resolvedBaseRef = Resolve-BaseRef

Write-Host "release-check: validando consistencia das regras ARCH"
if ($resolvedBaseRef) {
    Write-Host "release-check: usando base '$resolvedBaseRef' e head '$HeadRef'"
}
else {
    Write-Host "release-check: base de comparacao nao encontrada; validacoes dependentes de diff serao ignoradas"
}

$rulesDirectory = Join-Path $repoRoot "src/Swa.Analyzers.Core/Rules"
$ruleIdentifiersPath = "src/Swa.Analyzers.Core/RuleIdentifiers.cs"
$readmePath = "README.md"
$unshippedPath = "src/Swa.Analyzers.Core/AnalyzerReleases.Unshipped.md"
$coreProjectPath = "src/Swa.Analyzers.Core/Swa.Analyzers.Core.csproj"
$changelogPath = "CHANGELOG.md"

$ruleIdentifiersContent = Get-CurrentContent $ruleIdentifiersPath
$readmeContent = Get-CurrentContent $readmePath
$unshippedContent = Get-CurrentContent $unshippedPath
$coreProjectContent = Get-CurrentContent $coreProjectPath

if (-not $ruleIdentifiersContent) {
    Add-Failure "RuleIdentifiers.cs nao foi encontrado em '$ruleIdentifiersPath'."
}

if (-not $readmeContent) {
    Add-Failure "README.md nao foi encontrado."
}

if (-not $unshippedContent) {
    Add-Failure "AnalyzerReleases.Unshipped.md nao foi encontrado em '$unshippedPath'."
}

$ruleIds = @(Get-ArchIds $ruleIdentifiersContent)
$ruleIdSet = @{}
foreach ($ruleId in $ruleIds) {
    $ruleIdSet[$ruleId] = $true
}

$analyzerFiles = @(Get-ChildItem -LiteralPath $rulesDirectory -File -Filter "Arch*.cs" |
    Where-Object { $_.Name -match "^Arch(?<Number>\d{3}).*\.cs$" } |
    Sort-Object Name)

foreach ($analyzerFile in $analyzerFiles) {
    $number = [regex]::Match($analyzerFile.Name, "^Arch(?<Number>\d{3})").Groups["Number"].Value
    $ruleId = "ARCH$number"

    if (-not $ruleIdSet.ContainsKey($ruleId)) {
        Add-Failure "$($analyzerFile.FullName): analyzer $($analyzerFile.Name) nao possui entrada '$ruleId' em RuleIdentifiers.cs."
    }

    $docPath = Join-Path $repoRoot "docs/rules/$ruleId.md"
    if (-not (Test-Path -LiteralPath $docPath)) {
        Add-Failure "$($analyzerFile.Name): documentacao obrigatoria ausente em docs/rules/$ruleId.md."
    }

    $testFiles = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot "tests/Swa.Analyzers.Tests/Rules") -File -Filter "Arch$number*Tests.cs")
    if ($testFiles.Count -eq 0) {
        Add-Failure "$($analyzerFile.Name): teste obrigatorio ausente em tests/Swa.Analyzers.Tests/Rules/Arch$number*Tests.cs."
    }

    $sampleDirectory = Join-Path $repoRoot "src/Swa.Analyzers.SampleApp/Arch$number"
    if (-not (Test-Path -LiteralPath $sampleDirectory -PathType Container)) {
        Add-Failure "$($analyzerFile.Name): pasta de SampleApp obrigatoria ausente em src/Swa.Analyzers.SampleApp/Arch$number."
    }
}

foreach ($ruleId in $ruleIds) {
    if ($readmeContent -notmatch [regex]::Escape($ruleId)) {
        Add-Failure "RuleIdentifiers.cs contem '$ruleId', mas README.md nao possui entrada correspondente."
    }
}

if ($resolvedBaseRef -and (Test-GitCommit $resolvedBaseRef)) {
    $changedFiles = @(Invoke-Git @("diff", "--name-only", $resolvedBaseRef, $HeadRef) |
        ForEach-Object { Get-RepositoryPath $_ })
    $changedFiles += @(Invoke-Git @("diff", "--name-only", "--cached") |
        ForEach-Object { Get-RepositoryPath $_ })
    $changedFiles += @(Invoke-Git @("diff", "--name-only") |
        ForEach-Object { Get-RepositoryPath $_ })
    $changedFiles += @(Invoke-Git @("ls-files", "--others", "--exclude-standard") |
        ForEach-Object { Get-RepositoryPath $_ })
    $changedFiles = @($changedFiles | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)

    $baseIdentifiersContent = Get-RefContent $resolvedBaseRef $ruleIdentifiersPath
    $baseRuleIds = @(Get-ArchIds $baseIdentifiersContent)
    $baseRuleIdSet = @{}
    foreach ($ruleId in $baseRuleIds) {
        $baseRuleIdSet[$ruleId] = $true
    }

    foreach ($ruleId in $ruleIds) {
        if (-not $baseRuleIdSet.ContainsKey($ruleId) -and $unshippedContent -notmatch [regex]::Escape($ruleId)) {
            Add-Failure "Nova regra '$ruleId' detectada, mas AnalyzerReleases.Unshipped.md nao contem esse ID."
        }
    }

    $baseProjectContent = Get-RefContent $resolvedBaseRef $coreProjectPath
    $baseVersionPrefix = Get-VersionPrefix $baseProjectContent
    $currentVersionPrefix = Get-VersionPrefix $coreProjectContent
    $changelogChanged = $changedFiles -contains $changelogPath

    if ($baseVersionPrefix -and $currentVersionPrefix -and $baseVersionPrefix -ne $currentVersionPrefix -and -not $changelogChanged) {
        Add-Failure "VersionPrefix mudou de '$baseVersionPrefix' para '$currentVersionPrefix', mas CHANGELOG.md nao foi alterado no mesmo diff."
    }
}
elseif ($resolvedBaseRef) {
    Add-Failure "Base de comparacao '$resolvedBaseRef' nao foi encontrada. Use fetch-depth: 0 no CI ou informe -BaseRef valido."
}

if ($failures.Count -gt 0) {
    Write-Host ""
    Write-Host "release-check: falhou com $($failures.Count) problema(s):" -ForegroundColor Red
    foreach ($failure in $failures) {
        Write-Host "- $failure" -ForegroundColor Red
    }

    exit 1
}

Write-Host "release-check: validacoes aprovadas"
