param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

$packages = @(
    @{
        Name = "Swa.Analyzers.Reliability"
        ExpectedIds = @("ARCH016", "ARCH017", "ARCH021", "ARCH022")
    },
    @{
        Name = "Swa.Analyzers.Architecture"
        ExpectedIds = @("ARCH015", "ARCH020", "ARCH027", "ARCH029", "ARCH032")
    },
    @{
        Name = "Swa.Analyzers.Testing"
        ExpectedIds = @("ARCH005", "ARCH006")
    }
)

$allIds = @{}
$failures = New-Object System.Collections.Generic.List[string]

foreach ($package in $packages) {
    $assemblyPath = Join-Path $repoRoot "src/$($package.Name)/bin/$Configuration/netstandard2.0/$($package.Name).dll"

    if (-not (Test-Path -LiteralPath $assemblyPath)) {
        $failures.Add("Assembly not found: $assemblyPath")
        continue
    }

    $assembly = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
    $diagnosticIds = New-Object System.Collections.Generic.List[string]

    foreach ($type in $assembly.GetTypes()) {
        if ($type.IsAbstract) {
            continue
        }

        if (-not $type.IsSubclassOf([Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer])) {
            continue
        }

        $analyzer = [Activator]::CreateInstance($type)
        foreach ($descriptor in $analyzer.SupportedDiagnostics) {
            $diagnosticIds.Add($descriptor.Id)
        }
    }

    $actualIds = @($diagnosticIds | Sort-Object -Unique)
    $expectedIds = @($package.ExpectedIds | Sort-Object -Unique)

    foreach ($missingId in Compare-Object -ReferenceObject $expectedIds -DifferenceObject $actualIds -PassThru | Where-Object { $_ -in $expectedIds }) {
        $failures.Add("$($package.Name) does not expose expected diagnostic ID $missingId.")
    }

    foreach ($unexpectedId in Compare-Object -ReferenceObject $expectedIds -DifferenceObject $actualIds -PassThru | Where-Object { $_ -in $actualIds }) {
        $failures.Add("$($package.Name) exposes unexpected diagnostic ID $unexpectedId.")
    }

    foreach ($id in $actualIds) {
        if (-not $allIds.ContainsKey($id)) {
            $allIds[$id] = New-Object System.Collections.Generic.List[string]
        }

        $allIds[$id].Add($package.Name)
    }
}

foreach ($entry in $allIds.GetEnumerator()) {
    if ($entry.Value.Count -gt 1) {
        $failures.Add("Diagnostic ID $($entry.Key) is duplicated in: $($entry.Value -join ', ').")
    }
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) {
        Write-Error $failure
    }

    exit 1
}

Write-Host "Analyzer package isolation validated."
