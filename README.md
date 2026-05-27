# Swa.Analyzers

Analyzers Roslyn reutilizáveis para .NET, focados em convenções de arquitetura, confiabilidade e qualidade de testes.

## Projetos

- `src/Swa.Analyzers.Core`: implementação dos analyzers e descritores de diagnóstico.
- `src/Swa.Analyzers.CodeFixes`: implementação dos code fixes distribuído junto ao pacote.
- `tests/Swa.Analyzers.Tests`: testes automatizados dos analyzers.
- `src/Swa.Analyzers.SampleApp`: exemplos manuais válidos e inválidos para cada regra.

Cada regra tem documentação própria em [docs/rules](docs/rules). Os diagnósticos publicados pelo pacote usam *help links* absolutos para estes arquivos no repositório público, facilitando o acesso quando o analyzer é distribuído via NuGet.

Para introduzir o pacote em projetos existentes, veja o guia de [adoção gradual](docs/adoption.md), com exemplos de severidades, suppressions e tratamento de legado. Se quiser partir de uma política pronta, use os [perfis de adoção via `.editorconfig`](docs/editorconfig-profiles.md).

## Code fixes

Algumas regras mecânicas oferecem code fixes seguros na IDE. O suporte inicial está disponível para `ARCH001`, convertendo métodos concretos e funções locais `async void` diagnosticados para `async Task` quando a regra já descartou event handlers padrão e contratos de interface/override.

## Regras existentes

| ID      | Título (resumo)                                  | Categoria   | Severidade padrão | Doc                              |
| ------- | ------------------------------------------------ | ----------- | ----------------- | -------------------------------- |
| ARCH001 | Evite `async void` fora de event handlers        | Reliability | Warning           | [ARCH001](docs/rules/ARCH001.md) |
| ARCH002 | Evite `Task.ContinueWith`                        | Reliability | Warning           | [ARCH002](docs/rules/ARCH002.md) |
| ARCH003 | Proíba `NotBeNull()` em testes                   | TestQuality | Info              | [ARCH003](docs/rules/ARCH003.md) |
| ARCH004 | Exija o nome `_sut` em testes unitários          | TestQuality | Info              | [ARCH004](docs/rules/ARCH004.md) |
| ARCH005 | Restrinja o uso de `NSubstitute.Arg.Any()`       | TestQuality | Info              | [ARCH005](docs/rules/ARCH005.md) |
| ARCH006 | Alerte sobre exclusões em `BeEquivalentTo()`     | TestQuality | Info              | [ARCH006](docs/rules/ARCH006.md) |
| ARCH007 | Detecte concatenação de strings em loops         | Performance | Info              | [ARCH007](docs/rules/ARCH007.md) |
| ARCH008 | Proíba composição manual de paths                | Reliability | Info              | [ARCH008](docs/rules/ARCH008.md) |
| ARCH009 | Proíba bloqueio síncrono sobre async             | Reliability | Warning           | [ARCH009](docs/rules/ARCH009.md) |
| ARCH010 | Exija propagação de `CancellationToken`          | Reliability | Warning           | [ARCH010](docs/rules/ARCH010.md) |
| ARCH011 | Proíba lógica async ou bloqueante em construtores | Reliability | Warning           | [ARCH011](docs/rules/ARCH011.md) |
| ARCH012 | Prefira `DateTimeOffset` a `DateTime`            | Reliability | Info              | [ARCH012](docs/rules/ARCH012.md) |
| ARCH013 | Restrinja frameworks de mock a NSubstitute       | TestQuality | Info              | [ARCH013](docs/rules/ARCH013.md) |
| ARCH014 | Prefira `Is.Equivalent` a `NSubstitute.Arg.Is`   | TestQuality | Info              | [ARCH014](docs/rules/ARCH014.md) |
| ARCH015 | Proíba verbos em rotas HTTP                      | Design      | Warning           | [ARCH015](docs/rules/ARCH015.md) |
| ARCH016 | Evite `Task.Run` em fluxo de request ASP.NET     | Performance | Warning           | [ARCH016](docs/rules/ARCH016.md) |
| ARCH017 | Proíba fire-and-forget em fluxo de request       | Reliability | Warning           | [ARCH017](docs/rules/ARCH017.md) |
| ARCH018 | Evite instanciação direta de `HttpClient`        | Reliability | Warning           | [ARCH018](docs/rules/ARCH018.md) |
| ARCH019 | Evite `Authorize` com `AllowAnonymous`           | Security    | Warning           | [ARCH019](docs/rules/ARCH019.md) |
| ARCH020 | Exija autorização explícita em endpoints         | Security    | Warning           | [ARCH020](docs/rules/ARCH020.md) |
| ARCH021 | Prefira `AsNoTracking` em consultas EF de leitura | Performance | Warning           | [ARCH021](docs/rules/ARCH021.md) |
| ARCH022 | Evite materialização prematura de consultas      | Performance | Warning           | [ARCH022](docs/rules/ARCH022.md) |
| ARCH023 | Prefira `TimeProvider` para hora atual           | Testability | Warning           | [ARCH023](docs/rules/ARCH023.md) |
| ARCH024 | Evite strings interpoladas em chamadas `ILogger` | Observability | Warning           | [ARCH024](docs/rules/ARCH024.md) |
| ARCH025 | Exija categoria `ILogger` compatível             | Observability | Warning           | [ARCH025](docs/rules/ARCH025.md) |
| ARCH026 | Evite configuração CORS insegura                 | Security    | Warning           | [ARCH026](docs/rules/ARCH026.md) |
| ARCH027 | Evite dependências de infraestrutura no core     | Architecture | Warning           | [ARCH027](docs/rules/ARCH027.md) |
| ARCH028 | Proíba propriedades mutáveis em records          | Design      | Warning           | [ARCH028](docs/rules/ARCH028.md) |
| ARCH029 | Proíba setters públicos em entidades de domínio  | Design      | Warning           | [ARCH029](docs/rules/ARCH029.md) |
| ARCH030 | Detecte `PackageReference` duplicado entre projetos | Maintainability | Info              | [ARCH030](docs/rules/ARCH030.md) |
| ARCH031 | Prefira `System.Threading.Lock` a locks em `object` | Performance | Warning           | [ARCH031](docs/rules/ARCH031.md) |
| ARCH032 | Evite propriedades MSBuild duplicadas            | Maintainability | Info              | [ARCH032](docs/rules/ARCH032.md) |
| ARCH033 | Evite `BuildServiceProvider` no registro de serviços | Reliability | Warning           | [ARCH033](docs/rules/ARCH033.md) |

## Como configurar

### Requisitos

- .NET SDK 10.x, fixado pelo `global.json` do repositório.

Configure severidade via `.editorconfig` normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH001.severity = warning
dotnet_diagnostic.ARCH008.severity = info
```

Algumas regras aceitam opções próprias via `.editorconfig`. Exemplo para `ARCH015`:

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

Exemplo para `ARCH023`:

```ini
[*.cs]
dotnet_diagnostic.ARCH023.allowed_namespaces = ["MyApp.Infrastructure.Time"]
dotnet_diagnostic.ARCH023.allowed_types = ["MachineTimeSource"]
dotnet_diagnostic.ARCH023.ignore_simple_logging = true
```

Exemplo para `ARCH026`:

```ini
[*.cs]
dotnet_diagnostic.ARCH026.disallow_any_origin = true
```

Exemplo para `ARCH027`:

```ini
[*.cs]
dotnet_diagnostic.ARCH027.core_namespace_patterns = "*.Domain;*.Application"
dotnet_diagnostic.ARCH027.forbidden_namespace_patterns = "Microsoft.EntityFrameworkCore;Microsoft.AspNetCore;StackExchange.Redis;Npgsql"
dotnet_diagnostic.ARCH027.allowed_namespace_patterns =
dotnet_diagnostic.ARCH027.ignore_tests = true
```

Exemplo para `ARCH028`:

```ini
[*.cs]
dotnet_diagnostic.ARCH028.allow_non_public_setters = true
```

Exemplo para `ARCH029`:

```ini
[*.cs]
dotnet_diagnostic.ARCH029.entity_namespaces = ["MyApp.Domain.Entities", "MyApp.Domain.Aggregates"]
dotnet_diagnostic.ARCH029.entity_base_types = ["Entity", "AggregateRoot"]
dotnet_diagnostic.ARCH029.allow_internal_setters = false
```

Exemplo para `ARCH030`:

```ini
[*.csproj]
dotnet_diagnostic.ARCH030.allowed_packages = ["Microsoft.NET.Test.Sdk", "xunit", "coverlet.collector"]
dotnet_diagnostic.ARCH030.allowed_project_patterns = ["*.Tests.csproj", "*.Benchmarks.csproj"]
```

Exemplo para `ARCH031`:

```ini
[*.cs]
dotnet_diagnostic.ARCH031.minimum_target_framework = net9.0
dotnet_diagnostic.ARCH031.report_local_variables = true
```

Exemplo para `ARCH032`:

```ini
[*.csproj]
dotnet_diagnostic.ARCH032.ignored_properties = ["TargetFramework", "TargetFrameworks", "AssemblyName", "RootNamespace"]
dotnet_diagnostic.ARCH032.compare_values = true
```

Exemplo para `ARCH033`:

```ini
[*.cs]
dotnet_diagnostic.ARCH033.ignore_tests = true
```

As páginas de cada regra documentam o fallback das opções públicas, incluindo valor default, tratamento de valores vazios, inválidos, casing inesperado e JSON malformado quando aplicável. Em geral, arrays JSON malformados são ignorados e booleanos inválidos voltam ao default da regra.

## Como validar

- **Restore**: `dotnet restore ./Swa.Analyzers.slnx`
- **Build**: `dotnet build ./Swa.Analyzers.slnx --configuration Release --no-restore`
- **Testes**: `dotnet test ./Swa.Analyzers.slnx --configuration Release --no-build -m:1` (a orquestração do VSTest na `.slnx` falha antes da descoberta quando o MSBuild usa múltiplos nos)
- **Release check**: `pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-Release.ps1` (consistência entre regras ARCH, docs, testes, SampleApp e metadados de release)
- **Manual**: veja [src/Swa.Analyzers.SampleApp/README.md](src/Swa.Analyzers.SampleApp/README.md) (exemplos por regra e build com diagnósticos)

Detalhes das validações de release estão em [docs/release.md](docs/release.md).

## Release e versionamento

As releases usam GitVersion, configurado em [GitVersion.yml](GitVersion.yml), como fonte única da versão publicada. O workflow de release usa a versão `semVer` calculada pelo GitVersion para o `PackageVersion` do NuGet, a tag `vX.Y.Z` e a GitHub Release.

Não atualize `VersionPrefix` manualmente para preparar releases. Commits semânticos determinam o incremento: `fix:` e `perf:` geram patch, `feat:` gera minor, e `!` ou `BREAKING CHANGE:` geram major. Commits `docs:`, `test:`, `style:`, `chore:` e `ci:` não forçam incremento, salvo quando indicam breaking change.

Os metadados de regras publicadas ficam em `src/Swa.Analyzers.Core/AnalyzerReleases.Shipped.md`; regras novas ainda não publicadas ficam em `src/Swa.Analyzers.Core/AnalyzerReleases.Unshipped.md`. O release check valida que os IDs em `RuleIdentifiers.cs`, docs, README, testes, SampleApp e metadados shipped/unshipped permanecam consistentes.
