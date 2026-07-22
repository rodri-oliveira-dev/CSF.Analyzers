<#
.SYNOPSIS
Inspects generated analyzer NuGet packages for v2 package isolation.
#>
[CmdletBinding()]
param(
    [string]$PackageDirectory = "artifacts/packages",
    [Parameter(Mandatory = $true)]
    [string]$Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.IO.Compression.FileSystem

$repoRoot = & git rev-parse --show-toplevel
if (-not $repoRoot) {
    throw "package-inspection: nao foi possivel localizar a raiz do repositorio."
}

$repoRoot = ($repoRoot | Select-Object -First 1)
$packageRoot = if ([System.IO.Path]::IsPathRooted($PackageDirectory)) {
    $PackageDirectory
} else {
    Join-Path $repoRoot $PackageDirectory
}

$repositoryUrl = "https://github.com/rodri-oliveira-dev/Swa.Analyzers"
$expectedPackages = @(
    @{
        Id = "Swa.Analyzers.Reliability"
        Assembly = "Swa.Analyzers.Reliability.dll"
        Pdb = "Swa.Analyzers.Reliability.pdb"
    },
    @{
        Id = "Swa.Analyzers.Architecture"
        Assembly = "Swa.Analyzers.Architecture.dll"
        Pdb = "Swa.Analyzers.Architecture.pdb"
    },
    @{
        Id = "Swa.Analyzers.Testing"
        Assembly = "Swa.Analyzers.Testing.dll"
        Pdb = "Swa.Analyzers.Testing.pdb"
    }
)

$failures = New-Object System.Collections.Generic.List[string]

function Add-Failure {
    param([string]$Message)
    $script:failures.Add($Message)
}

function Open-Zip {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        Add-Failure "Artefato ausente: $Path"
        return $null
    }

    return [System.IO.Compression.ZipFile]::OpenRead($Path)
}

function Get-ZipEntryNames {
    param([System.IO.Compression.ZipArchive]$Archive)

    return @($Archive.Entries | ForEach-Object { $_.FullName.Replace("\", "/") })
}

function Read-ZipEntryText {
    param(
        [System.IO.Compression.ZipArchive]$Archive,
        [string]$EntryName
    )

    $entry = $Archive.Entries | Where-Object { $_.FullName -eq $EntryName } | Select-Object -First 1
    if (-not $entry) {
        return ""
    }

    $reader = [System.IO.StreamReader]::new($entry.Open())
    try {
        return $reader.ReadToEnd()
    } finally {
        $reader.Dispose()
    }
}

if (-not (Test-Path -LiteralPath $packageRoot -PathType Container)) {
    throw "package-inspection: diretorio de pacotes nao encontrado: $packageRoot"
}

$allPackageFiles = @(Get-ChildItem -LiteralPath $packageRoot -File -Filter "*.nupkg") +
    @(Get-ChildItem -LiteralPath $packageRoot -File -Filter "*.snupkg")

$legacyArtifacts = @($allPackageFiles | Where-Object { $_.Name -match "^Swa\.Analyzers\.$([regex]::Escape($Version))\.(s)?nupkg$" })
if ($legacyArtifacts.Count -gt 0) {
    Add-Failure "Pacote legado Swa.Analyzers foi gerado: $($legacyArtifacts.Name -join ', ')."
}

$expectedArtifactNames = @()
foreach ($package in $expectedPackages) {
    $expectedArtifactNames += "$($package.Id).$Version.nupkg"
    $expectedArtifactNames += "$($package.Id).$Version.snupkg"
}

$actualArtifactNames = @($allPackageFiles | ForEach-Object { $_.Name } | Sort-Object)
foreach ($expectedName in $expectedArtifactNames) {
    if ($actualArtifactNames -notcontains $expectedName) {
        Add-Failure "Artefato esperado nao encontrado: $expectedName."
    }
}

foreach ($unexpectedName in $actualArtifactNames | Where-Object { $_ -notin $expectedArtifactNames }) {
    Add-Failure "Artefato inesperado em ${packageRoot}: $unexpectedName."
}

foreach ($package in $expectedPackages) {
    $nupkgPath = Join-Path $packageRoot "$($package.Id).$Version.nupkg"
    $snupkgPath = Join-Path $packageRoot "$($package.Id).$Version.snupkg"

    $nupkg = Open-Zip $nupkgPath
    if ($nupkg) {
        try {
            $entries = @(Get-ZipEntryNames $nupkg)
            $expectedAnalyzerEntry = "analyzers/dotnet/cs/$($package.Assembly)"
            $analyzerEntries = @($entries | Where-Object { $_ -match "^analyzers/dotnet/cs/.+\.dll$" })

            if ($entries -notcontains $expectedAnalyzerEntry) {
                Add-Failure "$($package.Id): assembly esperado ausente em $expectedAnalyzerEntry."
            }

            if ($analyzerEntries.Count -ne 1 -or $analyzerEntries[0] -ne $expectedAnalyzerEntry) {
                Add-Failure "$($package.Id): deve conter apenas $expectedAnalyzerEntry em analyzers/dotnet/cs; encontrado: $($analyzerEntries -join ', ')."
            }

            $forbiddenAssemblies = @($entries | Where-Object {
                $_ -match "\.dll$" -and
                $_ -ne $expectedAnalyzerEntry
            })
            if ($forbiddenAssemblies.Count -gt 0) {
                Add-Failure "$($package.Id): DLL nao planejada encontrada: $($forbiddenAssemblies -join ', ')."
            }

            if ($entries -contains "analyzers/dotnet/cs/Swa.Analyzers.dll") {
                Add-Failure "$($package.Id): assembly legado Swa.Analyzers.dll encontrado."
            }

            if ($entries -notcontains "README.md") {
                Add-Failure "$($package.Id): README.md ausente no pacote."
            }

            $nuspecEntry = $nupkg.Entries | Where-Object { $_.FullName -like "*.nuspec" } | Select-Object -First 1
            if (-not $nuspecEntry) {
                Add-Failure "$($package.Id): nuspec ausente."
            } else {
                $nuspecText = Read-ZipEntryText $nupkg $nuspecEntry.FullName
                [xml]$nuspec = $nuspecText
                $metadata = $nuspec.package.metadata

                if ($metadata.id -ne $package.Id) {
                    Add-Failure "$($package.Id): PackageId incorreto no nuspec: $($metadata.id)."
                }

                if ($metadata.version -ne $Version) {
                    Add-Failure "$($package.Id): versao incorreta no nuspec: $($metadata.version)."
                }

                if ($metadata.repository.url -ne $repositoryUrl) {
                    Add-Failure "$($package.Id): repository URL incorreta no nuspec: $($metadata.repository.url)."
                }

                if ($metadata.readme -ne "README.md") {
                    Add-Failure "$($package.Id): README metadata incorreto no nuspec: $($metadata.readme)."
                }
            }
        } finally {
            $nupkg.Dispose()
        }
    }

    $snupkg = Open-Zip $snupkgPath
    if ($snupkg) {
        try {
            $entries = @(Get-ZipEntryNames $snupkg)
            $expectedPdbEntry = "analyzers/dotnet/cs/$($package.Pdb)"
            $pdbEntries = @($entries | Where-Object { $_ -match "\.pdb$" })
            $dllEntries = @($entries | Where-Object { $_ -match "\.dll$" })

            if ($entries -notcontains $expectedPdbEntry) {
                Add-Failure "$($package.Id): simbolos esperados ausentes em $expectedPdbEntry."
            }

            if ($pdbEntries | Where-Object { $_ -notlike "*$($package.Pdb)" }) {
                Add-Failure "$($package.Id): simbolos de outro pacote encontrados: $($pdbEntries -join ', ')."
            }

            if ($dllEntries.Count -gt 0) {
                Add-Failure "$($package.Id): snupkg nao deve conter DLLs: $($dllEntries -join ', ')."
            }
        } finally {
            $snupkg.Dispose()
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Host "package-inspection: falhou com $($failures.Count) problema(s):" -ForegroundColor Red
    foreach ($failure in $failures) {
        Write-Host "- $failure" -ForegroundColor Red
    }

    exit 1
}

Write-Host "package-inspection: validacoes aprovadas para versao $Version"
exit 0
