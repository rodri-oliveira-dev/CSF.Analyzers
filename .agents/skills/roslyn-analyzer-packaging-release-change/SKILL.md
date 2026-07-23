---
name: roslyn-analyzer-packaging-release-change
description: Use esta skill ao alterar empacotamento, release metadata, NuGet, CI, versao, lock file ou workflows deste projeto de analyzers.
---

# Objetivo

Preservar a distribuicao correta dos pacotes `CSF.Analyzers.Reliability`, `CSF.Analyzers.Architecture` e `CSF.Analyzers.Testing`, evitando dependencias vazadas para consumidores e mantendo CI, lock file e metadados consistentes.

# Quando usar

Use esta skill quando a tarefa envolver:

- projetos em `src/CSF.Analyzers.{Reliability,Architecture,Testing}`;
- `Directory.Packages.props`;
- `packages.lock.json`;
- `AnalyzerReleases.Unshipped.md`;
- workflows em `.github/workflows`;
- empacotamento NuGet;
- versao do pacote;
- SDK em `global.json`;
- README do pacote;
- configuracao de build e release.

# Regras de empacotamento

- Preserve os projetos de pacote em `netstandard2.0`.
- Preserve `IncludeBuildOutput=false`.
- Preserve `SuppressDependenciesWhenPacking=true`.
- Preserve o destino do analyzer em `analyzers/dotnet/cs`.
- Nao adicione `Version=` em `PackageReference`.
- Use `Directory.Packages.props` para versoes.
- Mantenha dependencias de Roslyn com `PrivateAssets="all"` quando aplicavel.
- Atualize lock file quando alterar dependencias.
- Nao altere versao de SDK sem validar impacto no CI.

# Validacao

```bash
dotnet restore ./CSF.Analyzers.slnx --locked-mode
dotnet build ./CSF.Analyzers.slnx --configuration Release --no-restore
dotnet test ./CSF.Analyzers.slnx --configuration Release -m:1
dotnet pack ./CSF.Analyzers.slnx --configuration Release --no-build
```
