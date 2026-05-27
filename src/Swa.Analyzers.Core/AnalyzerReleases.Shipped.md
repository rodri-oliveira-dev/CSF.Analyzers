## Release 1.0.0

### New Rules

Rule ID | Category    | Severity | Notes
--------|-------------|----------|------
ARCH001 | Reliability | Warning  | Evita `async void` fora de event handlers padrão.
ARCH002 | Reliability | Warning  | Evita `Task.ContinueWith`; prefira `await`.
ARCH003 | TestQuality | Info     | Evita `NotBeNull()` do FluentAssertions em testes.
ARCH004 | TestQuality | Info     | Exige o nome `_sut` para o system under test.
ARCH005 | TestQuality | Info     | Restringe o uso de `NSubstitute.Arg.Any()`.
ARCH006 | TestQuality | Info     | Avisa sobre exclusões do FluentAssertions em `BeEquivalentTo`.
ARCH007 | Performance | Info     | Detecta concatenação de strings dentro de loops.
ARCH008 | Reliability | Info     | Proíbe composição manual de paths em sinks de filesystem.
ARCH009 | Reliability | Warning  | Proíbe bloqueio síncrono de operações async.
ARCH010 | Reliability | Warning  | Exige propagação de `CancellationToken` em chamadas async de infraestrutura.
ARCH011 | Reliability | Warning  | Proíbe lógica async ou bloqueante em construtores.
ARCH012 | Reliability | Info     | Prefere `DateTimeOffset` a `DateTime`.
ARCH013 | TestQuality | Info     | Restringe frameworks de mock a NSubstitute.
ARCH014 | TestQuality | Info     | Prefere `Is.Equivalent` a `NSubstitute.Arg.Is`.
ARCH015 | Design      | Warning  | Proíbe verbos em segmentos de rotas HTTP.
ARCH016 | Performance | Warning  | Evita `Task.Run` em fluxos de request ASP.NET.
ARCH017 | Reliability | Warning  | Proíbe fire-and-forget em fluxos de request ASP.NET.
ARCH018 | Reliability | Warning  | Evita instanciação direta de `HttpClient`.
ARCH019 | Security    | Warning  | Evita metadados conflitantes de `Authorize` e `AllowAnonymous`.
ARCH020 | Security    | Warning  | Exige decisão explícita de autorização em endpoints HTTP.
ARCH021 | Performance | Warning  | Prefere `AsNoTracking` em consultas EF Core somente leitura.
ARCH022 | Performance | Warning  | Evita materialização prematura antes de filtro ou projeção.
ARCH023 | Testability | Warning  | Prefere `TimeProvider` a acesso direto ao relógio do sistema.
ARCH024 | Observability | Warning  | Evita strings interpoladas ou concatenação em chamadas `ILogger`.
ARCH025 | Observability | Warning  | Exige categoria `ILogger` compatível com o tipo que a contém.
ARCH026 | Security    | Warning  | Evita configuração CORS insegura no ASP.NET Core.
ARCH027 | Architecture | Warning  | Evita dependências de infraestrutura em camadas core.
ARCH028 | Design      | Warning  | Proíbe propriedades mutáveis em records.
ARCH029 | Design      | Warning  | Proíbe setters públicos em entidades de domínio.
ARCH030 | Maintainability | Info     | Detecta itens `PackageReference` duplicados entre projetos.
ARCH031 | Performance | Warning  | Prefere `System.Threading.Lock` a monitores baseados em `object`.
ARCH032 | Maintainability | Info     | Evita propriedades MSBuild duplicadas entre arquivos de projeto e `Directory.Build.props`.
