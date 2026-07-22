## Release 1.0.0

### New Rules

Rule ID | Category    | Severity | Notes
--------|-------------|----------|------
ARCH016 | Performance | Warning  | Evita `Task.Run` em fluxos de request ASP.NET.
ARCH017 | Reliability | Warning  | Proibe fire-and-forget em fluxos de request ASP.NET.
ARCH021 | Performance | Warning  | Prefere `AsNoTracking` em consultas EF Core somente leitura.
ARCH022 | Performance | Warning  | Evita materializacao prematura antes de filtro ou projecao.
