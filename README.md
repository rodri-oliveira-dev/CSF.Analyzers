# Swa.Analyzers

Analyzers Roslyn reutilizaveis para .NET, focados em convencoes de arquitetura, confiabilidade e qualidade de testes.

## Projetos

- `src/Swa.Analyzers.Core`: implementacao dos analyzers e descritores de diagnostico.
- `tests/Swa.Analyzers.Tests`: testes automatizados dos analyzers.
- `src/Swa.Analyzers.SampleApp`: exemplos manuais validos e invalidos para cada regra.

Cada regra tem documentacao propria em [docs/rules](docs/rules). Os diagnosticos publicados pelo pacote usam *help links* absolutos para estes arquivos no repositorio publico, facilitando o acesso quando o analyzer e distribuido via NuGet.

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

## Como validar

- **Restore**: `dotnet restore ./Swa.Analyzers.slnx`
- **Build**: `dotnet build ./Swa.Analyzers.slnx --configuration Release --no-restore`
- **Testes**: `dotnet test ./Swa.Analyzers.slnx --configuration Release --no-build -m:1` (a orquestracao do VSTest na `.slnx` falha antes da descoberta quando o MSBuild usa multiplos nos)
- **Manual**: veja [src/Swa.Analyzers.SampleApp/README.md](src/Swa.Analyzers.SampleApp/README.md) (exemplos por regra e build com diagnosticos)
