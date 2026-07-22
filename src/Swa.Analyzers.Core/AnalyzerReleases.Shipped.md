## Release 1.0.0

### New Rules

Rule ID | Category    | Severity | Notes
--------|-------------|----------|------
ARCH005 | TestQuality | Info     | Restringe o uso de `NSubstitute.Arg.Any()`.
ARCH006 | TestQuality | Info     | Avisa sobre exclusoes do FluentAssertions em `BeEquivalentTo`.
ARCH015 | Design      | Warning  | Proibe verbos em segmentos de rotas HTTP.
ARCH016 | Performance | Warning  | Evita `Task.Run` em fluxos de request ASP.NET.
ARCH017 | Reliability | Warning  | Proibe fire-and-forget em fluxos de request ASP.NET.
ARCH020 | Security    | Warning  | Exige decisao explicita de autorizacao em endpoints HTTP.
ARCH021 | Performance | Warning  | Prefere `AsNoTracking` em consultas EF Core somente leitura.
ARCH022 | Performance | Warning  | Evita materializacao prematura antes de filtro ou projecao.
ARCH027 | Architecture | Warning  | Evita dependencias de infraestrutura em camadas core.
ARCH029 | Design      | Warning  | Proibe setters publicos em entidades de dominio.
ARCH032 | Maintainability | Info     | Evita propriedades MSBuild duplicadas entre arquivos de projeto e `Directory.Build.props`.
