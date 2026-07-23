# Historico de regras da linha 1.x

Este documento preserva o historico documental dos IDs `ARCH###` que existiam na linha 1.x do `CSF.Analyzers`.
Esses IDs nao representam necessariamente regras ativas na linha 2.0.

## Release 1.0.0

### New Rules

Rule ID | Category    | Severity | Notes
--------|-------------|----------|------
ARCH001 | Reliability | Warning  | Evita `async void` fora de event handlers padrao.
ARCH002 | Reliability | Warning  | Evita `Task.ContinueWith`; prefira `await`.
ARCH003 | TestQuality | Info     | Evita `NotBeNull()` do FluentAssertions em testes.
ARCH004 | TestQuality | Info     | Exige o nome `_sut` para o system under test.
ARCH005 | TestQuality | Info     | Restringe o uso de `NSubstitute.Arg.Any()`.
ARCH006 | TestQuality | Info     | Avisa sobre exclusoes do FluentAssertions em `BeEquivalentTo`.
ARCH007 | Performance | Info     | Detecta concatenacao de strings dentro de loops.
ARCH008 | Reliability | Info     | Proibe composicao manual de paths em sinks de filesystem.
ARCH009 | Reliability | Warning  | Proibe bloqueio sincrono de operacoes async.
ARCH010 | Reliability | Warning  | Exige propagacao de `CancellationToken` em chamadas async de infraestrutura.
ARCH011 | Reliability | Warning  | Proibe logica async ou bloqueante em construtores.
ARCH012 | Reliability | Info     | Prefere `DateTimeOffset` a `DateTime`.
ARCH013 | TestQuality | Info     | Restringe frameworks de mock a NSubstitute.
ARCH014 | TestQuality | Info     | Prefere `Is.Equivalent` a `NSubstitute.Arg.Is`.
ARCH015 | Design      | Warning  | Proibe verbos em segmentos de rotas HTTP.
ARCH016 | Performance | Warning  | Evita `Task.Run` em fluxos de request ASP.NET.
ARCH017 | Reliability | Warning  | Proibe fire-and-forget em fluxos de request ASP.NET.
ARCH018 | Reliability | Warning  | Evita instanciacao direta de `HttpClient`.
ARCH019 | Security    | Warning  | Evita metadados conflitantes de `Authorize` e `AllowAnonymous`.
ARCH020 | Security    | Warning  | Exige decisao explicita de autorizacao em endpoints HTTP.
ARCH021 | Performance | Warning  | Prefere `AsNoTracking` em consultas EF Core somente leitura.
ARCH022 | Performance | Warning  | Evita materializacao prematura antes de filtro ou projecao.
ARCH023 | Testability | Warning  | Prefere `TimeProvider` a acesso direto ao relogio do sistema.
ARCH024 | Observability | Warning  | Evita strings interpoladas ou concatenacao em chamadas `ILogger`.
ARCH025 | Observability | Warning  | Exige categoria `ILogger` compativel com o tipo que a contem.
ARCH026 | Security    | Warning  | Evita configuracao CORS insegura no ASP.NET Core.
ARCH027 | Architecture | Warning  | Evita dependencias de infraestrutura em camadas core.
ARCH028 | Design      | Warning  | Proibe propriedades mutaveis em records.
ARCH029 | Design      | Warning  | Proibe setters publicos em entidades de dominio.
ARCH030 | Maintainability | Info     | Detecta itens `PackageReference` duplicados entre projetos.
ARCH031 | Performance | Warning  | Prefere `System.Threading.Lock` a monitores baseados em `object`.
ARCH032 | Maintainability | Info     | Evita propriedades MSBuild duplicadas entre arquivos de projeto e `Directory.Build.props`.

## Regras nao publicadas antes da migracao v2

Rule ID | Category    | Severity | Notes
--------|-------------|----------|------
ARCH033 | Reliability | Warning  | Evita `BuildServiceProvider` durante o registro de servicos.
