# Changelog

## [Unreleased]

### Added

- Added ARCH033 to detect `BuildServiceProvider()` calls on `IServiceCollection` during service registration, with `.editorconfig` support for `dotnet_diagnostic.ARCH033.ignore_tests`.

### Changed

- Migrated release versioning from project `VersionPrefix` to GitVersion, using semantic commits to calculate the NuGet package version and GitHub Release tag.

### Fixed

- Fixed the release workflow order so the .NET SDK is installed before GitVersion runs `dotnet tool install`.

### Breaking Changes

- None.

## [1.0.0] - 2026-05-01

Primeira versao estavel do pacote `Swa.Analyzers`.

### Added

- Published the stable baseline of analyzer rules ARCH001 through ARCH032, covering architecture, reliability, performance, security, observability, design, testability and test-quality conventions.
- Included rule documentation in `docs/rules` and manual examples in `src/Swa.Analyzers.SampleApp`.
- Added local and CI release validation for ARCH rules, documentation, tests, SampleApp, changelog updates and package version changes.
- Added release workflow support for generating `.nupkg` and `.snupkg` packages from the project `VersionPrefix`.

### Changed

- Centralized analyzer option parsing helpers for boolean and string-array `.editorconfig` values and shared wildcard matching without changing rule behavior.
- Documented `.editorconfig` fallback behavior consistently for public analyzer options.
- Shared JSON string-array parsing across configurable analyzers and added support for escaped unicode values in `.editorconfig` options.
- Documented the release process and package version source of truth.
- Hardened GitHub release creation to use `VersionPrefix`, create tag `v1.0.0`, refuse existing tags/releases and keep NuGet publication inactive until explicit governance setup.

### Fixed

- Hardened malformed and excessive analyzer configuration handling for ARCH020, ARCH027 and ARCH030.
- Hardened MSBuild `AdditionalFiles` XML parsing in ARCH030 and ARCH032 with defensive size limits and DTD-prohibiting XML reader settings.

### Breaking Changes

- None.

## [0.2.0] - 2026-04-30

- Added release consistency validation for ARCH rules, documentation, tests, SampleApp, changelog updates and package version changes.
