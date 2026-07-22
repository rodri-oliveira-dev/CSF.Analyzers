### New Rules

Rule ID | Category    | Severity | Notes
--------|-------------|----------|------
REL001 | Performance | Warning  | Novo ID v2. Evita `Task.Run` em fluxos de request ASP.NET.
REL002 | Reliability | Warning  | Novo ID v2. Proibe fire-and-forget em fluxos de request ASP.NET.
REL003 | Performance | Disabled | Novo ID v2. Opt-in com severidade base Info; prefere `AsNoTracking` em consultas EF Core somente leitura.
REL004 | Performance | Warning  | Novo ID v2. Evita materializacao prematura antes de filtro ou projecao.
