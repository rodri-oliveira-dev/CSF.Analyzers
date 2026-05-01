# Changelog

## [Unreleased]

- Centralized analyzer option parsing helpers for boolean and string-array `.editorconfig` values and shared wildcard matching without changing rule behavior.
- Documented `.editorconfig` fallback behavior consistently for public analyzer options.
- Shared JSON string-array parsing across configurable analyzers and added support for escaped unicode values in `.editorconfig` options.
- Hardened malformed and excessive analyzer configuration handling for ARCH020, ARCH027 and ARCH030.
- Hardened MSBuild `AdditionalFiles` XML parsing in ARCH030 and ARCH032 with defensive size limits and DTD-prohibiting XML reader settings.

## [0.2.0] - 2026-04-30

- Added release consistency validation for ARCH rules, documentation, tests, SampleApp, changelog updates and package version changes.
