---
name: roslyn-analyzer-packaging-release-change
description: Use esta skill ao alterar empacotamento, release metadata, NuGet, CI, versao, lock file ou workflows deste projeto de analyzers.
---

# Objetivo

Preservar a distribuicao correta do pacote `Swa.Analyzers`, evitando dependencias vazadas para consumidores e mantendo CI, lock file e metadados consistentes.

# Quando usar

Use esta skill quando a tarefa envolver:

- `src/Swa.Analyzers.Core/Swa.Analyzers.Core.csproj`
- `Directory.Packages.props`
- `packages.lock.json`
- `AnalyzerReleases.Unshipped.md`
- workflows em `.github/workflows`
- empacotamento NuGet
- versao do pacote
- SDK em `global.json`
- README do pacote
- configuracao de build e release

# Regras de empacotamento

- Preserve `Swa.Analyzers.Core` em `netstandard2.0`, salvo decisao explicita em contrario.
- Preserve `IncludeBuildOutput=false`.
- Preserve `SuppressDependenciesWhenPacking=true`.
- Preserve o destino do analyzer em `analyzers/dotnet/cs`.
- Nao adicione `Version=` em `PackageReference`.
- Use `Directory.Packages.props` para versoes.
- Mantenha dependencias de Roslyn com `PrivateAssets="all"` quando aplicavel.
- Atualize lock file quando alterar dependencias.
- Nao altere versao de SDK sem validar impacto no CI.

# Regras de release metadata

- Ao criar nova regra, atualize `src/Swa.Analyzers.Core/AnalyzerReleases.Unshipped.md`.
- Mantenha ID, categoria, severidade e descricao coerentes com o `DiagnosticDescriptor`.
- Nao remova historico de regra sem necessidade explicita.

# Regras de CI

- Preserve restore com `--locked-mode` quando o workflow depender de lock file.
- Preserve `-m:1` nos testes enquanto a orquestracao da `.slnx` falhar com multiplos nos.
- Prefira comandos alinhados ao README e ao `AGENTS.md`.
- Nao reduzir validacoes de PR sem justificativa clara.

# Validacao

Comandos recomendados:

```bash
dotnet restore ./Swa.Analyzers.slnx --locked-mode
dotnet build ./Swa.Analyzers.slnx --configuration Release --no-restore
dotnet test ./Swa.Analyzers.slnx --configuration Release -m:1
```

Se alterar empacotamento, validar tambem o pack quando apropriado:

```bash
dotnet pack src/Swa.Analyzers.Core/Swa.Analyzers.Core.csproj --configuration Release --no-build
```
