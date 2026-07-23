# Swa.Analyzers

`Swa.Analyzers` distribui analyzers Roslyn para políticas contextuais de projetos .NET. A v2 separa o produto em três pacotes independentes para que cada solução instale somente as regras que fazem sentido para seu risco operacional, arquitetura e padrão de testes.

Analyzers genéricos do .NET, Roslyn, SonarAnalyzer ou Meziantou.Analyzer verificam práticas amplas de linguagem e plataforma. Estes pacotes cobrem decisões de time que dependem de contexto: endpoints ASP.NET devem declarar autorização explicitamente, camadas core não devem depender de infraestrutura, entidades de domínio podem ter mutabilidade restrita e testes podem rejeitar matchers ou exclusões amplas.

## Pacotes

| Pacote | Escopo | Regras |
| ------ | ------ | ------ |
| [`Swa.Analyzers.Reliability`](docs/packages/reliability.md) | Confiabilidade e performance operacional em ASP.NET, hosted services e EF Core. | `REL001`, `REL002`, `REL003`, `REL004`, `REL005`, `REL006` |
| [`Swa.Analyzers.Architecture`](docs/packages/architecture.md) | Políticas de autorização, rotas, dependências de camadas, DDD e MSBuild. | `ARC001`, `ARC002`, `ARC003`, `ARC004`, `ARC005`, `ARC006` |
| [`Swa.Analyzers.Testing`](docs/packages/testing.md) | Qualidade de testes com NSubstitute (`Arg.Any`/`AnyArgs`) e FluentAssertions. | `TST001`, `TST002` |

## Status de publicação

Os pacotes v2 são gerados pelo workflow de release e anexados à GitHub Release, mas a publicação no NuGet.org ainda está comentada até a configuração explícita de `NUGET_API_KEY` e de um ambiente protegido. Em 2026-07-23, a API pública do NuGet.org retorna `404` para `Swa.Analyzers.Reliability`, `Swa.Analyzers.Architecture` e `Swa.Analyzers.Testing`.

Os comandos abaixo são o formato esperado para consumo quando os pacotes estiverem publicados no NuGet.org ou disponíveis em um feed privado/local.

## Instalação

Instale cada pacote no projeto que deve receber aquela política. Não há metapacote `Swa.Analyzers` na v2 inicial.

```powershell
dotnet add package Swa.Analyzers.Reliability
dotnet add package Swa.Analyzers.Architecture
dotnet add package Swa.Analyzers.Testing
```

Em repositórios com Central Package Management, declare as versões em `Directory.Packages.props` e use `PackageReference` sem `Version`.

## Quick start

1. Escolha o pacote que representa a política que você quer validar.
2. Instale a partir do NuGet.org quando publicado, ou a partir do feed privado/local usado pelo repositório.
3. Compile o projeto e revise os diagnósticos habilitados por padrão.
4. Ative regras opt-in apenas quando elas representarem uma política real do time.

## Exemplo mínimo

Com `Swa.Analyzers.Architecture`, um endpoint sem decisão explícita de autorização emite `ARC001`:

```csharp
app.MapGet("/orders", () => Results.Ok());
```

Código conforme:

```csharp
app.MapGet("/orders", () => Results.Ok())
    .RequireAuthorization();

app.MapGet("/health", () => Results.Ok())
    .AllowAnonymous();
```

## Regras

| ID | Pacote | Categoria | Padrão | Documentação |
| -- | ------ | --------- | ------ | ------------ |
| `REL001` | Reliability | Performance | `warning`, habilitada | [REL001](docs/rules/reliability/REL001.md) |
| `REL002` | Reliability | Reliability | `warning`, habilitada | [REL002](docs/rules/reliability/REL002.md) |
| `REL003` | Reliability | Performance | `info`, opt-in | [REL003](docs/rules/reliability/REL003.md) |
| `REL004` | Reliability | Performance | `warning`, habilitada | [REL004](docs/rules/reliability/REL004.md) |
| `REL005` | Reliability | Reliability | `warning`, habilitada | [REL005](docs/rules/reliability/REL005.md) |
| `REL006` | Reliability | Reliability | `warning`, habilitada | [REL006](docs/rules/reliability/REL006.md) |
| `ARC001` | Architecture | Security | `warning`, habilitada | [ARC001](docs/rules/architecture/ARC001.md) |
| `ARC002` | Architecture | Architecture | `warning`, habilitada | [ARC002](docs/rules/architecture/ARC002.md) |
| `ARC003` | Architecture | Design | `info`, opt-in | [ARC003](docs/rules/architecture/ARC003.md) |
| `ARC004` | Architecture | Design | `info`, opt-in | [ARC004](docs/rules/architecture/ARC004.md) |
| `ARC005` | Architecture | Maintainability | `info`, opt-in | [ARC005](docs/rules/architecture/ARC005.md) |
| `ARC006` | Architecture | Architecture | `info`, opt-in | [ARC006](docs/rules/architecture/ARC006.md) |
| `TST001` | Testing | TestQuality | `info`, opt-in | [TST001](docs/rules/testing/TST001.md) |
| `TST002` | Testing | TestQuality | `info`, opt-in | [TST002](docs/rules/testing/TST002.md) |

Regras habilitadas por padrão: `REL001`, `REL002`, `REL004`, `REL005`, `REL006`, `ARC001`, `ARC002`.

Regras opt-in: `REL003`, `ARC003`, `ARC004`, `ARC005`, `ARC006`, `TST001`, `TST002`. Elas só emitem diagnóstico quando a severidade é ativada via configuração de analyzer. Para regras de código-fonte, use `.editorconfig`; para `ARC005`, prefira `.globalconfig`, pois o diagnóstico é reportado em arquivos MSBuild passados como `AdditionalFiles`.

```ini
[*.cs]
dotnet_diagnostic.REL003.severity = info
dotnet_diagnostic.ARC003.severity = info
dotnet_diagnostic.TST001.severity = warning
```

Exemplo de `.globalconfig` para `ARC005`:

```ini
is_global = true
dotnet_diagnostic.ARC005.severity = warning
```

Opções específicas ficam documentadas nas páginas das regras e dos pacotes.

## Guias

- [Adoção gradual](docs/adoption.md)
- [Perfis de `.editorconfig`](docs/editorconfig-profiles.md)
- [Migração da v1 para a v2](docs/migration-v2.md)
- [Contribuindo com regras](docs/contributing-rules.md)
- [Validações de release](docs/release.md)
- [Sobreposição com analyzers externos](docs/reviews/rules-analyzer-overlap.md)

## Contribuição

Use a solução principal:

```powershell
dotnet restore ./Swa.Analyzers.slnx
dotnet build ./Swa.Analyzers.slnx --configuration Release --no-restore
dotnet test ./Swa.Analyzers.slnx --configuration Release -m:1
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-Release.ps1
```

Os samples ficam em `samples/Swa.Analyzers.*.Sample` e validam exemplos manuais por pacote.

## Publicação

Os três pacotes usam GitVersion como fonte única de versão. O workflow de release gera `Swa.Analyzers.Reliability`, `Swa.Analyzers.Architecture` e `Swa.Analyzers.Testing` com a mesma versão calculada.

A publicação no NuGet.org permanece comentada no workflow até que `NUGET_API_KEY` e o ambiente protegido sejam configurados explicitamente. Portanto, este repositório não afirma que os pacotes v2 já estão publicados.
