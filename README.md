# Swa.Analyzers

Analyzers Roslyn reutilizaveis para .NET, focados em convencoes de arquitetura, confiabilidade e qualidade de testes.

## Projetos

- `src/Swa.Analyzers.Core`: implementacao dos analyzers e descritores de diagnostico.
- `tests/Swa.Analyzers.Tests`: testes automatizados dos analyzers.
- `src/Swa.Analyzers.SampleApp`: exemplos manuais validos e invalidos para cada regra.

Cada regra tem documentacao propria em [docs/rules](docs/rules). Os diagnosticos publicados pelo pacote usam *help links* absolutos para estes arquivos no repositorio publico, facilitando o acesso quando o analyzer e distribuido via NuGet.

Para introduzir o pacote em projetos existentes, veja o guia de [adocao gradual](docs/adoption.md), com exemplos de severidades, suppressions e tratamento de legado.

## Regras existentes

| ID      | Titulo (resumo)                                  | Categoria   | Severidade padrao | Doc                              |
| ------- | ------------------------------------------------ | ----------- | ----------------- | -------------------------------- |
| ARCH001 | Avoid async void outside event handlers          | Reliability | Warning           | [ARCH001](docs/rules/ARCH001.md) |
| ARCH002 | Avoid Task.ContinueWith                          | Reliability | Warning           | [ARCH002](docs/rules/ARCH002.md) |
| ARCH003 | Prohibit NotBeNull() in tests                    | TestQuality | Info              | [ARCH003](docs/rules/ARCH003.md) |
| ARCH004 | Enforce _sut naming in unit tests                | TestQuality | Info              | [ARCH004](docs/rules/ARCH004.md) |
| ARCH005 | Restrict usage of NSubstitute Arg.Any()          | TestQuality | Info              | [ARCH005](docs/rules/ARCH005.md) |
| ARCH006 | Warn on exclusions in BeEquivalentTo()           | TestQuality | Info              | [ARCH006](docs/rules/ARCH006.md) |
| ARCH007 | Detect string concatenation inside loops         | Performance | Info              | [ARCH007](docs/rules/ARCH007.md) |
| ARCH008 | Prohibit manual path composition                 | Reliability | Info              | [ARCH008](docs/rules/ARCH008.md) |
| ARCH009 | Prohibit sync over async blocking calls          | Reliability | Warning           | [ARCH009](docs/rules/ARCH009.md) |
| ARCH010 | Enforce CancellationToken propagation            | Reliability | Warning           | [ARCH010](docs/rules/ARCH010.md) |
| ARCH011 | Prohibit async or blocking logic in constructors | Reliability | Warning           | [ARCH011](docs/rules/ARCH011.md) |
| ARCH012 | Prefer DateTimeOffset over DateTime              | Reliability | Info              | [ARCH012](docs/rules/ARCH012.md) |
| ARCH013 | Restrict mocking frameworks to NSubstitute       | TestQuality | Info              | [ARCH013](docs/rules/ARCH013.md) |
| ARCH014 | Prefer Is.Equivalent over NSubstitute Arg.Is     | TestQuality | Info              | [ARCH014](docs/rules/ARCH014.md) |
| ARCH015 | Prohibit verbs in HTTP routes                    | Design      | Warning           | [ARCH015](docs/rules/ARCH015.md) |
| ARCH016 | Avoid Task.Run in ASP.NET request flow           | Performance | Warning           | [ARCH016](docs/rules/ARCH016.md) |
| ARCH017 | Prohibit fire-and-forget in request flow         | Reliability | Warning           | [ARCH017](docs/rules/ARCH017.md) |
| ARCH018 | Avoid direct HttpClient instantiation            | Reliability | Warning           | [ARCH018](docs/rules/ARCH018.md) |
| ARCH019 | Avoid Authorize with AllowAnonymous              | Security    | Warning           | [ARCH019](docs/rules/ARCH019.md) |
| ARCH020 | Require explicit endpoint authorization          | Security    | Warning           | [ARCH020](docs/rules/ARCH020.md) |
| ARCH021 | Prefer AsNoTracking for read-only EF queries    | Performance | Warning           | [ARCH021](docs/rules/ARCH021.md) |
| ARCH022 | Avoid premature query materialization           | Performance | Warning           | [ARCH022](docs/rules/ARCH022.md) |
| ARCH023 | Prefer TimeProvider for current time            | Testability | Warning           | [ARCH023](docs/rules/ARCH023.md) |
| ARCH024 | Avoid interpolated strings in ILogger calls     | Observability | Warning           | [ARCH024](docs/rules/ARCH024.md) |
| ARCH025 | Enforce matching ILogger category              | Observability | Warning           | [ARCH025](docs/rules/ARCH025.md) |
| ARCH026 | Avoid insecure CORS configuration              | Security    | Warning           | [ARCH026](docs/rules/ARCH026.md) |
| ARCH027 | Prevent infrastructure dependencies in core layers | Architecture | Warning           | [ARCH027](docs/rules/ARCH027.md) |
| ARCH028 | Prohibit mutable properties in records        | Design      | Warning           | [ARCH028](docs/rules/ARCH028.md) |
| ARCH029 | Prohibit public setters in domain entities    | Design      | Warning           | [ARCH029](docs/rules/ARCH029.md) |
| ARCH030 | Detect duplicated PackageReference across projects | Maintainability | Info              | [ARCH030](docs/rules/ARCH030.md) |
| ARCH031 | Prefer System.Threading.Lock over object locks | Performance | Warning           | [ARCH031](docs/rules/ARCH031.md) |
| ARCH032 | Avoid duplicated MSBuild properties             | Maintainability | Info              | [ARCH032](docs/rules/ARCH032.md) |

## Como configurar

### Requisitos

- .NET SDK 10.x, fixado pelo `global.json` do repositorio.

Configure severidade via `.editorconfig` normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH001.severity = warning
dotnet_diagnostic.ARCH008.severity = info
```

Algumas regras aceitam opcoes proprias via `.editorconfig`. Exemplo para `ARCH015`:

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

As paginas de cada regra documentam o fallback das opcoes publicas, incluindo valor default, tratamento de valores vazios, invalidos, casing inesperado e JSON malformado quando aplicavel. Em geral, arrays JSON malformados sao ignorados e booleanos invalidos voltam ao default da regra.

## Como validar

- **Restore**: `dotnet restore ./Swa.Analyzers.slnx`
- **Build**: `dotnet build ./Swa.Analyzers.slnx --configuration Release --no-restore`
- **Testes**: `dotnet test ./Swa.Analyzers.slnx --configuration Release --no-build -m:1` (a orquestracao do VSTest na `.slnx` falha antes da descoberta quando o MSBuild usa multiplos nos)
- **Release check**: `pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-Release.ps1` (consistencia entre regras ARCH, docs, testes, SampleApp, changelog e versao)
- **Manual**: veja [src/Swa.Analyzers.SampleApp/README.md](src/Swa.Analyzers.SampleApp/README.md) (exemplos por regra e build com diagnosticos)

Detalhes das validacoes de release estao em [docs/release.md](docs/release.md).
