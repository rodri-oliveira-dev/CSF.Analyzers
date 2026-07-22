# Swa.Analyzers

Analyzers Roslyn reutilizÃ¡veis para .NET, focados em convenÃ§Ãµes de arquitetura, confiabilidade e qualidade de testes.

## Projetos

- `src/Swa.Analyzers.Reliability`: pacote `Swa.Analyzers.Reliability`.
- `src/Swa.Analyzers.Architecture`: pacote `Swa.Analyzers.Architecture`.
- `src/Swa.Analyzers.Testing`: pacote `Swa.Analyzers.Testing`.
- `tests/Swa.Analyzers.*.Tests`: testes automatizados por pacote e validacao de isolamento.
- `samples/Swa.Analyzers.*.Sample`: exemplos manuais validos e invalidos por pacote.

Cada regra tem documentaÃ§Ã£o prÃ³pria em [docs/rules](docs/rules). Os diagnÃ³sticos publicados pelo pacote usam *help links* absolutos para estes arquivos no repositÃ³rio pÃºblico, facilitando o acesso quando o analyzer Ã© distribuÃ­do via NuGet.

Para introduzir o pacote em projetos existentes, veja o guia de [adoÃ§Ã£o gradual](docs/adoption.md), com exemplos de severidades, suppressions e tratamento de legado. Se quiser partir de uma polÃ­tica pronta, use os [perfis de adoÃ§Ã£o via `.editorconfig`](docs/editorconfig-profiles.md).

## DocumentaÃ§Ã£o complementar

- [SobreposiÃ§Ã£o entre regras Swa.Analyzers e analyzers externos](docs/reviews/rules-analyzer-overlap.md)

## Regras existentes

| ID      | TÃ­tulo (resumo)                                  | Categoria   | Severidade padrÃ£o | Doc                              |
| ------- | ------------------------------------------------ | ----------- | ----------------- | -------------------------------- |
| ARCH005 | Restrinja o uso de `NSubstitute.Arg.Any()`       | TestQuality | Info              | [ARCH005](docs/rules/ARCH005.md) |
| ARCH006 | Alerte sobre exclusÃµes em `BeEquivalentTo()`     | TestQuality | Info              | [ARCH006](docs/rules/ARCH006.md) |
| ARCH015 | ProÃ­ba verbos em rotas HTTP                      | Design      | Warning           | [ARCH015](docs/rules/ARCH015.md) |
| ARCH016 | Evite `Task.Run` em fluxo de request ASP.NET     | Performance | Warning           | [ARCH016](docs/rules/ARCH016.md) |
| ARCH017 | ProÃ­ba fire-and-forget em fluxo de request       | Reliability | Warning           | [ARCH017](docs/rules/ARCH017.md) |
| ARCH020 | Exija autorizaÃ§Ã£o explÃ­cita em endpoints         | Security    | Warning           | [ARCH020](docs/rules/ARCH020.md) |
| ARCH021 | Prefira `AsNoTracking` em consultas EF de leitura | Performance | Warning           | [ARCH021](docs/rules/ARCH021.md) |
| ARCH022 | Evite materializaÃ§Ã£o prematura de consultas      | Performance | Warning           | [ARCH022](docs/rules/ARCH022.md) |
| ARCH027 | Evite dependÃªncias de infraestrutura no core     | Architecture | Warning           | [ARCH027](docs/rules/ARCH027.md) |
| ARCH029 | ProÃ­ba setters pÃºblicos em entidades de domÃ­nio  | Design      | Warning           | [ARCH029](docs/rules/ARCH029.md) |
| ARCH032 | Evite propriedades MSBuild duplicadas            | Maintainability | Info              | [ARCH032](docs/rules/ARCH032.md) |

Distribuicao por pacote:

- `Swa.Analyzers.Reliability`: `ARCH016`, `ARCH017`, `ARCH021`, `ARCH022`.
- `Swa.Analyzers.Architecture`: `ARCH015`, `ARCH020`, `ARCH027`, `ARCH029`, `ARCH032`.
- `Swa.Analyzers.Testing`: `ARCH005`, `ARCH006`.

Distribuição por pacote:

- `Swa.Analyzers.Reliability`: `ARCH016`, `ARCH017`, `ARCH021`, `ARCH022`.
- `Swa.Analyzers.Architecture`: `ARCH015`, `ARCH020`, `ARCH027`, `ARCH029`, `ARCH032`.
- `Swa.Analyzers.Testing`: `ARCH005`, `ARCH006`.

## Como configurar

### Requisitos

- .NET SDK 10.x, fixado pelo `global.json` do repositÃ³rio.

Configure severidade via `.editorconfig` normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH005.severity = warning
dotnet_diagnostic.ARCH016.severity = warning
```

Algumas regras aceitam opÃ§Ãµes prÃ³prias via `.editorconfig`. Exemplo para `ARCH015`:

```ini
[*.cs]
dotnet_diagnostic.ARCH015.route_language = pt-BR
dotnet_diagnostic.ARCH015.additional_verbs = ["ativar", "inativar", "recalcular"]
```

Exemplo para `ARCH020`:

```ini
[*.cs]
dotnet_diagnostic.ARCH020.allowed_routes = ["/internal/status", "/diagnostics/*"]
dotnet_diagnostic.ARCH020.allowed_methods = ["Ping"]
dotnet_diagnostic.ARCH020.ignored_namespaces = ["Sample.PublicEndpoints"]
```

Exemplo para `ARCH027`:

```ini
[*.cs]
dotnet_diagnostic.ARCH027.core_namespace_patterns = "*.Domain;*.Application"
dotnet_diagnostic.ARCH027.forbidden_namespace_patterns = "Microsoft.EntityFrameworkCore;Microsoft.AspNetCore;StackExchange.Redis;Npgsql"
dotnet_diagnostic.ARCH027.allowed_namespace_patterns =
dotnet_diagnostic.ARCH027.ignore_tests = true
```

Exemplo para `ARCH029`:

```ini
[*.cs]
dotnet_diagnostic.ARCH029.entity_namespaces = ["MyApp.Domain.Entities", "MyApp.Domain.Aggregates"]
dotnet_diagnostic.ARCH029.entity_base_types = ["Entity", "AggregateRoot"]
dotnet_diagnostic.ARCH029.allow_internal_setters = false
```

Exemplo para `ARCH032`:

```ini
[*.csproj]
dotnet_diagnostic.ARCH032.ignored_properties = ["TargetFramework", "TargetFrameworks", "AssemblyName", "RootNamespace"]
dotnet_diagnostic.ARCH032.compare_values = true
```

As pÃ¡ginas de cada regra documentam o fallback das opÃ§Ãµes pÃºblicas, incluindo valor default, tratamento de valores vazios, invÃ¡lidos, casing inesperado e JSON malformado quando aplicÃ¡vel. Em geral, arrays JSON malformados sÃ£o ignorados e booleanos invÃ¡lidos voltam ao default da regra.

## Como validar

- **Restore**: `dotnet restore ./Swa.Analyzers.slnx`
- **Build**: `dotnet build ./Swa.Analyzers.slnx --configuration Release --no-restore`
- **Testes**: `dotnet test ./Swa.Analyzers.slnx --configuration Release --no-build -m:1` (a orquestraÃ§Ã£o do VSTest na `.slnx` falha antes da descoberta quando o MSBuild usa mÃºltiplos nos)
- **Release check**: `pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-Release.ps1` (consistÃªncia entre regras ARCH, docs, testes, SampleApp e metadados de release)
- **Manual**: compile os projetos em `samples/Swa.Analyzers.*.Sample` para validar exemplos por pacote.

Detalhes das validaÃ§Ãµes de release estÃ£o em [docs/release.md](docs/release.md).

## Release e versionamento

As releases usam GitVersion, configurado em [GitVersion.yml](GitVersion.yml), como fonte Ãºnica da versÃ£o publicada. O workflow de release usa a versÃ£o `semVer` calculada pelo GitVersion para o `PackageVersion` do NuGet, a tag `vX.Y.Z` e a GitHub Release.

NÃ£o atualize `VersionPrefix` manualmente para preparar releases. Commits semÃ¢nticos determinam o incremento: `fix:` e `perf:` geram patch, `feat:` gera minor, e `!` ou `BREAKING CHANGE:` geram major. Commits `docs:`, `test:`, `style:`, `chore:` e `ci:` nÃ£o forÃ§am incremento, salvo quando indicam breaking change.

Os metadados de regras publicadas ficam em `src/Swa.Analyzers.{Reliability,Architecture,Testing}/AnalyzerReleases.Shipped.md`; regras novas ainda nao publicadas ficam nos respectivos `AnalyzerReleases.Unshipped.md`. O release check valida que os IDs em `RuleIdentifiers.cs`, docs, README, testes, samples e metadados shipped/unshipped permanecam consistentes.
