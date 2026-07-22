# Swa.Analyzers

Analyzers Roslyn reutilizaveis para .NET, focados em convencoes de arquitetura, confiabilidade e qualidade de testes.

## Projetos

- `src/Swa.Analyzers.Reliability`: pacote `Swa.Analyzers.Reliability`.
- `src/Swa.Analyzers.Architecture`: pacote `Swa.Analyzers.Architecture`.
- `src/Swa.Analyzers.Testing`: pacote `Swa.Analyzers.Testing`.
- `tests/Swa.Analyzers.*.Tests`: testes automatizados por pacote e validacao de isolamento.
- `samples/Swa.Analyzers.*.Sample`: exemplos manuais validos e invalidos por pacote.

Cada regra tem documentacao propria em [docs/rules](docs/rules). Os diagnosticos publicados pelo pacote usam help links absolutos para estes arquivos no repositorio publico, facilitando o acesso quando o analyzer e distribuido via NuGet.

Para introduzir o pacote em projetos existentes, veja o guia de [adocao gradual](docs/adoption.md), com exemplos de severidades, suppressions e tratamento de legado. Se quiser partir de uma politica pronta, use os [perfis de adocao via `.editorconfig`](docs/editorconfig-profiles.md).

## Documentacao complementar

- [Sobreposicao historica entre regras Swa.Analyzers e analyzers externos](docs/history/rules-analyzer-overlap.md)
- [Migracao para a v2](docs/migration-v2.md)

## Regras existentes

| ID | Titulo (resumo) | Categoria | Severidade padrao | Doc |
| -- | --------------- | --------- | ----------------- | --- |
| ARC001 | Exija autorizacao explicita em endpoints | Security | Warning | [ARC001](docs/rules/ARC001.md) |
| ARC002 | Evite dependencias de infraestrutura no core | Architecture | Warning | [ARC002](docs/rules/ARC002.md) |
| ARC003 | Proiba verbos em rotas HTTP | Design | Info (opt-in) | [ARC003](docs/rules/ARC003.md) |
| ARC004 | Proiba setters publicos em entidades de dominio | Design | Info (opt-in) | [ARC004](docs/rules/ARC004.md) |
| ARC005 | Evite propriedades MSBuild duplicadas | Maintainability | Info (opt-in) | [ARC005](docs/rules/ARC005.md) |
| REL001 | Evite `Task.Run` em fluxo de request ASP.NET | Performance | Warning | [REL001](docs/rules/REL001.md) |
| REL002 | Proiba fire-and-forget em fluxo de request | Reliability | Warning | [REL002](docs/rules/REL002.md) |
| REL003 | Prefira `AsNoTracking` em consultas EF de leitura | Performance | Info (opt-in) | [REL003](docs/rules/REL003.md) |
| REL004 | Evite materializacao prematura de consultas | Performance | Warning | [REL004](docs/rules/REL004.md) |
| TST001 | Restrinja o uso de `NSubstitute.Arg.Any()` | TestQuality | Info (opt-in) | [TST001](docs/rules/TST001.md) |
| TST002 | Alerte sobre exclusoes em `BeEquivalentTo()` | TestQuality | Info (opt-in) | [TST002](docs/rules/TST002.md) |

Distribuicao por pacote:

- `Swa.Analyzers.Reliability`: `REL001`, `REL002`, `REL003`, `REL004`.
- `Swa.Analyzers.Architecture`: `ARC001`, `ARC002`, `ARC003`, `ARC004`, `ARC005`.
- `Swa.Analyzers.Testing`: `TST001`, `TST002`.

## Como configurar

### Requisitos

- .NET SDK 10.x, fixado pelo `global.json` do repositorio.

Configure severidade via `.editorconfig` normalmente:

```ini
[*.cs]
dotnet_diagnostic.TST001.severity = info
dotnet_diagnostic.REL001.severity = warning
```

Algumas regras aceitam opcoes proprias via `.editorconfig`. Para regras opt-in, defina tambem a severidade.

Exemplo para `ARC003`:

```ini
[*.cs]
dotnet_diagnostic.ARC003.severity = info
dotnet_diagnostic.ARC003.route_language = pt-BR
dotnet_diagnostic.ARC003.additional_verbs = ["ativar", "inativar", "recalcular"]
```

Exemplo para `ARC001`:

```ini
[*.cs]
dotnet_diagnostic.ARC001.allowed_routes = ["/internal/status", "/diagnostics/*"]
dotnet_diagnostic.ARC001.allowed_methods = ["Ping"]
dotnet_diagnostic.ARC001.ignored_namespaces = ["Sample.PublicEndpoints"]
```

Exemplo para `ARC002`:

```ini
[*.cs]
dotnet_diagnostic.ARC002.core_namespace_patterns = "*.Domain;*.Application"
dotnet_diagnostic.ARC002.forbidden_namespace_patterns = "Microsoft.EntityFrameworkCore;Microsoft.AspNetCore;StackExchange.Redis;Npgsql"
dotnet_diagnostic.ARC002.allowed_namespace_patterns =
dotnet_diagnostic.ARC002.ignore_tests = true
```

Exemplo para `ARC004`:

```ini
[*.cs]
dotnet_diagnostic.ARC004.severity = info
dotnet_diagnostic.ARC004.entity_namespaces = ["MyApp.Domain.Entities", "MyApp.Domain.Aggregates"]
dotnet_diagnostic.ARC004.entity_base_types = ["Entity", "AggregateRoot"]
dotnet_diagnostic.ARC004.allow_internal_setters = false
```

Exemplo para `ARC005`:

```ini
[*.csproj]
dotnet_diagnostic.ARC005.severity = info
dotnet_diagnostic.ARC005.ignored_properties = ["TargetFramework", "TargetFrameworks", "AssemblyName", "RootNamespace"]
dotnet_diagnostic.ARC005.compare_values = true
```

As paginas de cada regra documentam o fallback das opcoes publicas, incluindo valor default, tratamento de valores vazios, invalidos, casing inesperado e JSON malformado quando aplicavel. Em geral, arrays JSON malformados sao ignorados e booleanos invalidos voltam ao default da regra.

## Como validar

- **Restore**: `dotnet restore ./Swa.Analyzers.slnx`
- **Build**: `dotnet build ./Swa.Analyzers.slnx --configuration Release --no-restore`
- **Testes**: `dotnet test ./Swa.Analyzers.slnx --configuration Release --no-build -m:1`
- **Release check**: `powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-Release.ps1`
- **Manual**: compile os projetos em `samples/Swa.Analyzers.*.Sample` para validar exemplos por pacote.

Detalhes das validacoes de release estao em [docs/release.md](docs/release.md).

## Release e versionamento

As releases usam GitVersion, configurado em [GitVersion.yml](GitVersion.yml), como fonte unica da versao publicada. O workflow de release usa a versao `semVer` calculada pelo GitVersion para o `PackageVersion` do NuGet, a tag `vX.Y.Z` e a GitHub Release.

Nao atualize `VersionPrefix` manualmente para preparar releases. Commits semanticos determinam o incremento: `fix:` e `perf:` geram patch, `feat:` gera minor, e `!` ou `BREAKING CHANGE:` geram major. Commits `docs:`, `test:`, `style:`, `chore:` e `ci:` nao forcam incremento, salvo quando indicam breaking change.

Os metadados de regras publicadas ficam em `src/Swa.Analyzers.{Reliability,Architecture,Testing}/AnalyzerReleases.Shipped.md`; regras novas ainda nao publicadas ficam nos respectivos `AnalyzerReleases.Unshipped.md`. O release check valida que os IDs em `RuleIdentifiers.cs`, docs, README, testes, samples e metadados shipped/unshipped permanecam consistentes.
