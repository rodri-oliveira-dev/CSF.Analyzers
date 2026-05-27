---
name: roslyn-analyzer-packaging-release-change
description: Use esta skill ao alterar empacotamento, release metadata, NuGet, CI, versão, lock file ou workflows deste projeto de analyzers.
---

# Objetivo

Preservar a distribuição correta do pacote `Swa.Analyzers`, evitando dependências vazadas para consumidores e mantendo CI, lock file e metadados consistentes.

# Quando usar

Use esta skill quando a tarefa envolver:

- `src/Swa.Analyzers.Core/Swa.Analyzers.Core.csproj`
- `Directory.Packages.props`
- `packages.lock.json`
- `AnalyzerReleases.Unshipped.md`
- workflows em `.github/workflows`
- empacotamento NuGet
- versão do pacote
- SDK em `global.json`
- README do pacote
- configuração de build e release

# Regras de empacotamento

- Preserve `Swa.Analyzers.Core` em `netstandard2.0`, salvo decisão explícita em contrário.
- Preserve `IncludeBuildOutput=false`.
- Preserve `SuppressDependenciesWhenPacking=true`.
- Preserve o destino do analyzer em `analyzers/dotnet/cs`.
- Não adicione `Version=` em `PackageReference`.
- Use `Directory.Packages.props` para versões.
- Mantenha dependências de Roslyn com `PrivateAssets="all"` quando aplicável.
- Atualize lock file quando alterar dependências.
- Não altere versão de SDK sem validar impacto no CI.

# Regras de release metadata

- Ao criar nova regra, atualize `src/Swa.Analyzers.Core/AnalyzerReleases.Unshipped.md`.
- Mantenha ID, categoria, severidade e descrição coerentes com o `DiagnosticDescriptor`.
- Não remova histórico de regra sem necessidade explícita.

# Regras de CI

- Preserve restore com `--locked-mode` quando o workflow depender de lock file.
- Preserve `-m:1` nos testes enquanto a orquestração da `.slnx` falhar com múltiplos nos.
- Prefira comandos alinhados ao README e ao `AGENTS.md`.
- Não reduzir validações de PR sem justificativa clara.

# Validacao

Comandos recomendados:

```bash
dotnet restore ./Swa.Analyzers.slnx --locked-mode
dotnet build ./Swa.Analyzers.slnx --configuration Release --no-restore
dotnet test ./Swa.Analyzers.slnx --configuration Release -m:1
```

Se alterar empacotamento, validar também o pack quando apropriado:

```bash
dotnet pack src/Swa.Analyzers.Core/Swa.Analyzers.Core.csproj --configuration Release --no-build
```
