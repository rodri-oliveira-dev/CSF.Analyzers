# Swa.Analyzers

Analyzers Roslyn reutilizaveis para .NET, focados em convencoes de arquitetura, confiabilidade e qualidade de testes.

## Projetos

- `src/Swa.Analyzers.Core`: implementacao dos analyzers e descritores de diagnostico.
- `tests/Swa.Analyzers.Tests`: testes automatizados dos analyzers.
- `src/Swa.Analyzers.SampleApp`: exemplos manuais validos e invalidos para cada regra.

Cada regra tem documentacao propria em `docs/rules/`, tambem usada como *help link* dos diagnosticos.

## Regras existentes

| ID      | Titulo (resumo)                                  | Categoria   | Severidade padrao | Doc                     |
| ------- | ------------------------------------------------ | ----------- | ----------------- | ----------------------- |
| ARCH001 | Avoid async void outside event handlers          | Reliability | Warning           | `docs/rules/ARCH001.md` |
| ARCH002 | Avoid Task.ContinueWith                          | Reliability | Warning           | `docs/rules/ARCH002.md` |
| ARCH003 | Prohibit NotBeNull() in tests                    | TestQuality | Info              | `docs/rules/ARCH003.md` |
| ARCH004 | Enforce _sut naming in unit tests                | TestQuality | Info              | `docs/rules/ARCH004.md` |
| ARCH005 | Restrict usage of NSubstitute Arg.Any()          | TestQuality | Info              | `docs/rules/ARCH005.md` |
| ARCH006 | Warn on exclusions in BeEquivalentTo()           | TestQuality | Info              | `docs/rules/ARCH006.md` |
| ARCH007 | Detect string concatenation inside loops         | Performance | Info              | `docs/rules/ARCH007.md` |
| ARCH008 | Prohibit manual path composition                 | Reliability | Info              | `docs/rules/ARCH008.md` |
| ARCH009 | Prohibit sync over async blocking calls          | Reliability | Warning           | `docs/rules/ARCH009.md` |
| ARCH010 | Enforce CancellationToken propagation            | Reliability | Warning           | `docs/rules/ARCH010.md` |
| ARCH011 | Prohibit async or blocking logic in constructors | Reliability | Warning           | `docs/rules/ARCH011.md` |
| ARCH012 | Prefer DateTimeOffset over DateTime              | Reliability | Info              | `docs/rules/ARCH012.md` |
| ARCH013 | Restrict mocking frameworks to NSubstitute       | TestQuality | Info              | `docs/rules/ARCH013.md` |
| ARCH014 | Prefer Is.Equivalent over NSubstitute Arg.Is     | TestQuality | Info              | `docs/rules/ARCH014.md` |

## Como configurar

### Requisitos

- .NET SDK 10.x

Configure severidade via `.editorconfig` normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH001.severity = warning
dotnet_diagnostic.ARCH008.severity = info
```

## Como validar

- **Restore**: `dotnet restore ./Swa.Analyzers.slnx`
- **Build**: `dotnet build ./Swa.Analyzers.slnx --configuration Release --no-restore -m:1`
- **Testes**: `dotnet test ./Swa.Analyzers.slnx --configuration Release --no-build -m:1`
- **Manual**: veja `src/Swa.Analyzers.SampleApp/README.md` (exemplos por regra e build com diagnosticos)
