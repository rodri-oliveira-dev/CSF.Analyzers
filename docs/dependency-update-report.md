# Dependency Update Report

Verification date: 2026-07-22

## Scope

This audit covered Central Package Management, direct `PackageReference` items, `global.json`, the local .NET tool manifest, GitHub Actions workflows, scripts, samples, tests and package validation files. Official sources used: NuGet.org flat-container metadata, Microsoft .NET release metadata, and the official GitHub repositories/tags for each action.

## Inventory

| Dependency | Type | Current version | Latest stable version | Update? | Justification |
| ----------- | ---- | --------------: | --------------------: | -------- | ------------- |
| `coverlet.collector` | NuGet package / coverage collector | `10.0.1` | `10.0.1` | No | Already latest stable on NuGet.org. |
| `Microsoft.CodeAnalysis.CSharp` | NuGet package / Roslyn analyzer compile dependency | `5.3.0` | `5.6.0` | Yes | Minor update within Roslyn 5.x; keeps `netstandard2.0`, remains `PrivateAssets="all"` and is validated by analyzer tests and package inspection. |
| `Microsoft.CodeAnalysis.CSharp.Workspaces` | NuGet package / Roslyn test dependency | `5.3.0` | `5.6.0` | Yes | Kept aligned with `Microsoft.CodeAnalysis.CSharp` to avoid mixed Roslyn versions in analyzer tests. |
| `Microsoft.CodeAnalysis.Analyzers` | NuGet package / Roslyn analyzer rules | `5.3.0` | `5.6.0` | Yes | Kept aligned with Roslyn 5.6.0; package reference remains private to avoid runtime leakage. |
| `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` | NuGet package / analyzer test harness | `1.1.3` | `1.1.4` | Yes | Patch update in analyzer testing harness; no framework migration. |
| `Microsoft.EntityFrameworkCore` | NuGet package / reliability tests only | `9.0.16` | `10.0.10` | Partial | Updated to latest stable 9.x patch `9.0.18`; major `10.0.10` deferred because the tests intentionally stay on the .NET 9 EF line. |
| `MicrosoftAspNetCoreAppRefVersion` / `Microsoft.AspNetCore.App.Ref` | PackageDownload property for tests | `9.0.16` | `10.0.10` | Partial | Updated to latest stable 9.x patch `9.0.18`; major `10.0.10` deferred with EF Core 10. |
| `Microsoft.NET.Test.Sdk` | NuGet package / test runner | `18.5.1` | `18.8.1` | Yes | Minor update in the same VSTest/TestPlatform generation; no xUnit framework migration. |
| `xunit` | NuGet package / test framework | `2.9.3` | `2.9.3` | No | Already latest stable xUnit v2 package on NuGet.org. |
| `xunit.runner.visualstudio` | NuGet package / test adapter | `3.1.5` | `3.1.5` | No | Already latest stable on NuGet.org. |
| `dotnet-reportgenerator-globaltool` | Local .NET tool / coverage reporting | `5.5.7` | `5.5.10` | Yes | Patch update; command remains `reportgenerator`. |
| .NET SDK | `global.json` | `10.0.203` | `10.0.302` | Yes | Latest stable .NET 10 SDK per Microsoft release metadata; `rollForward: latestFeature` preserved and CI already installs `10.0.x`. |
| GitVersion CLI | CI-installed tool through GitTools action `versionSpec: 6.x` | `6.x` | `6.8.2` | No file change | Floating within stable GitVersion 6.x by design; `GitVersion.yml` already uses GitVersion 6 syntax. |
| `actions/checkout` | GitHub Action | `v6.0.2` SHA `de0fac2e4500dabe0009e67214ff5f5447ce83dd` | `v7.0.1` | Yes | Updated SHA to official stable tag while preserving `fetch-depth: 0` where GitVersion requires full history. |
| `actions/setup-dotnet` | GitHub Action | `v5.2.0` SHA `c2fa09f4bde5ebb9d1777cf28262a3eb3db3ced7` | `v6.0.0` | Yes | Updated to current stable setup action; continues installing `10.0.x`. |
| `gittools/actions` | GitHub Action | `v4.5.0` SHA `bc6623af8fc07d5a8903052dd46da33403eec8e8` | `v4.7.0` | Yes | Patch/minor action update; GitVersion CLI line remains `6.x` and workflow/config syntax unchanged. |
| `actions/cache` | GitHub Action | `v5.0.5` SHA `27d5ce7f107fe9357f9df03efb73ab90386fccae` | `v6.1.0` | Yes | Stable action update; NuGet cache key and path preserved. |
| `actions/upload-artifact` | GitHub Action | `v7.0.1` SHA `043fb46d1a93c77aae656e7c1c64a875d1fc6a0a` | `v7.0.1` | No | Already latest stable. |
| `actions/download-artifact` | GitHub Action | `v8.0.1` SHA `3e5f45b2cfb9172054b4087a40e8e0b5a5461e7c` | `v8.0.1` | No | Already latest stable. |
| `actions/github-script` | GitHub Action | `v9.0.0` SHA `3a2844b7e9c422d3c10d287c895573f7108da1b3` | `v9.0.0` | No | Already latest stable; added readable tag comments where touched. |
| `actions/attest` | GitHub Action | `v4.1.0` SHA `59d89421af93a897026c735860bf21b6eb4f7b26` | `v4.2.0` | Yes | Patch update; package attestation subject paths unchanged. |
| `actions/dependency-review-action` | GitHub Action | `v5.0.0` SHA `a1d282b36b6f3519aa1f3fc636f609c47dddb294` | `v5.0.0` | No | Already latest stable. |
| `github/codeql-action` | GitHub Action | `v4.35.4` SHA `68bde559dea0fdcac2102bfdf6230c5f70eb485e` | `v4.37.3` | Yes | Patch/minor CodeQL action update; language, manual build mode, restore and build commands preserved. |
| `ubuntu-latest` | GitHub-hosted runner image | Floating runner label | Current GitHub-hosted stable image | No | No container images are pinned in this repository; changing runner strategy is outside this safe dependency update. |

No `PackageReference` with an inline `Version` was found. No container images were found. No dependency was removed because no unused direct dependency had enough evidence for safe deletion.

## Deferred Major Updates

| Dependency | Latest major | Recommendation |
| ---------- | ------------ | -------------- |
| `Microsoft.EntityFrameworkCore` | `10.0.10` | Defer. The reliability tests use EF Core as a framework symbol/reference line, and the repo currently models ASP.NET/EF references through .NET 9 ref packs for those tests. Move EF/AspNetCore refs to 10.x only in a focused compatibility change. |
| `Microsoft.AspNetCore.App.Ref` | `10.0.10` | Defer together with EF Core 10 to avoid mixing framework reference lines without a behavioral reason. |

## Roslyn Compatibility Notes

Roslyn packages were updated as a matched set from `5.3.0` to `5.6.0`. Analyzer package projects still target `netstandard2.0`, keep `PrivateAssets="all"` for Roslyn references, keep `IncludeBuildOutput=false`, keep `SuppressDependenciesWhenPacking=true`, and continue packing only analyzer DLL/PDB files under `analyzers/dotnet/cs`.

The update does not intentionally change diagnostic IDs, severities, messages, heuristics, `.editorconfig` options, package IDs or release versioning behavior.

## Sources

- NuGet.org package metadata: `https://api.nuget.org/v3-flatcontainer/<package>/index.json`
- Microsoft .NET release metadata: `https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json`
- GitHub action tags: official repositories under `https://github.com/actions`, `https://github.com/gittools/actions`, and `https://github.com/github/codeql-action`
- GitVersion CLI metadata: `https://api.nuget.org/v3-flatcontainer/gitversion.tool/index.json`

## Validation Plan

Required validation after the lock files are updated:

```powershell
dotnet --info
dotnet restore ./Swa.Analyzers.slnx
dotnet build ./Swa.Analyzers.slnx --configuration Release --no-restore
dotnet test ./Swa.Analyzers.slnx --configuration Release --no-build -m:1
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-Release.ps1
dotnet pack ./Swa.Analyzers.slnx --configuration Release --no-build
```

Package inspection should be run against the generated package version after pack artifacts exist.

## Validation Results

Executed locally on 2026-07-22:

| Command | Result |
| ------- | ------ |
| `dotnet --info` | Passed; SDK `10.0.302`, runtime `10.0.10`. |
| `dotnet tool restore` | Passed; restored `dotnet-reportgenerator-globaltool` `5.5.10`. |
| `dotnet restore ./Swa.Analyzers.slnx` | Passed; lock files regenerated. |
| `dotnet build ./Swa.Analyzers.slnx --configuration Release --no-restore` | Passed; expected sample diagnostics were reported, and no new RS analyzer warnings appeared. |
| `dotnet test ./Swa.Analyzers.slnx --configuration Release --no-build -m:1` | Passed; 178 tests passed. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-Release.ps1` | Passed; `release-check: validacoes aprovadas`. |
| `dotnet pack ./Swa.Analyzers.slnx --configuration Release --no-build --output ./artifacts/packages-verify` | Passed; generated the three `.nupkg` and three `.snupkg` files with version `1.0.0` in the local non-GitVersion pack. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Inspect-NuGetPackages.ps1 -PackageDirectory ./artifacts/packages-verify -Version 1.0.0` | Passed; package inspection approved all three packages and symbol packages. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-AnalyzerPackageIsolation.ps1 -PackageDirectory ./artifacts/packages-verify -Version 1.0.0` | Passed; reran package validation tests and package inspection, then approved analyzer package isolation. |
| `dotnet restore ./Swa.Analyzers.slnx --locked-mode` | Passed; updated lock files are consistent. |

`pwsh` is not installed in this Windows environment, so PowerShell scripts were run with Windows PowerShell via `powershell`. The first package inspection against `artifacts/packages` failed only because old package artifacts were already present in that directory; a clean `artifacts/packages-verify` directory passed inspection.
