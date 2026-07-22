# ValidaÃ§Ãµes de release

O repositÃ³rio usa `scripts/Validate-Release.ps1` para validar consistÃªncia entre regras ARCH, documentaÃ§Ã£o, testes, SampleApp e metadados de release.

## Regras shipped e unshipped

O projeto separa os metadados de regras em dois arquivos:

- `src/Swa.Analyzers.{Reliability,Architecture,Testing}/AnalyzerReleases.Shipped.md`: regras ja publicadas em alguma versao estavel do pacote correspondente.
- `src/Swa.Analyzers.{Reliability,Architecture,Testing}/AnalyzerReleases.Unshipped.md`: regras novas ou alteracoes de regras ainda nao publicadas.

O formato segue o padrÃ£o de release tracking de analyzers Roslyn: uma seÃ§Ã£o de release, como `## Release 1.0.0`, e subsecoes como `### New Rules` com tabela de `Rule ID`, `Category`, `Severity` e `Notes`.

Na migraÃ§Ã£o para a versÃ£o 2.0, `AnalyzerReleases.Shipped.md` permanece como metadata ativa das regras ainda implementadas. O histÃ³rico dos IDs da linha 1.x fica preservado em `docs/history/v1-analyzer-releases.md`. NÃ£o mova regras para `Shipped` sem base no histÃ³rico do repositÃ³rio, tag, changelog ou pacote publicado.

Fluxo esperado:

1. Ao criar uma nova regra, adicione o ID em `RuleIdentifiers.cs` e registre a regra em `AnalyzerReleases.Unshipped.md`.
2. Antes de publicar, confirme a versÃ£o calculada pelo GitVersion, o `CHANGELOG.md`, docs, README, testes e samples.
3. Depois que uma versÃ£o for efetivamente publicada, mova as regras publicadas de `Unshipped` para uma nova seÃ§Ã£o em `AnalyzerReleases.Shipped.md`, preservando ID, categoria, severidade e notas coerentes com o `DiagnosticDescriptor`.
4. Nunca remova ou renomeie IDs publicados. RemoÃ§Ã£o, renomeaÃ§Ã£o, aumento restritivo de severidade padrÃ£o ou mudanÃ§a incompativel de comportamento deve ser tratada como breaking change.

## Execucao local

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-Release.ps1
```

O script tenta comparar o estado atual com o upstream da branch. Quando necessÃ¡rio, informe refs explicitamente:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-Release.ps1 -BaseRef origin/main -HeadRef HEAD
```

O hook `.githooks/pre-push` executa a mesma validaÃ§Ã£o antes do restore, build, testes e cobertura.

## Validacoes

- Cada analyzer `Arch###*.cs` em `src/Swa.Analyzers.{Reliability,Architecture,Testing}/Rules` precisa ter entrada no `RuleIdentifiers.cs` do pacote.
- Cada `ARCH###` em `RuleIdentifiers.cs` precisa ter analyzer no pacote correspondente, `docs/rules/ARCH###.md`, teste em `tests/Swa.Analyzers.*.Tests/Rules/Arch###*Tests.cs` e sample em `samples/Swa.Analyzers.*.Sample/Arch###`.
- Cada `ARCH###` declarado em `RuleIdentifiers.cs` precisa aparecer no `README.md`.
- Cada `ARCH###` declarado em `RuleIdentifiers.cs` precisa aparecer em exatamente um dos metadados de release: `AnalyzerReleases.Shipped.md` ou `AnalyzerReleases.Unshipped.md`.
- Nenhum `ARCH###` pode aparecer nos metadados de release sem entrada correspondente em `RuleIdentifiers.cs`.
- Um `ARCH###` nÃ£o pode aparecer simultaneamente em `Shipped` e `Unshipped`.
- Uma regra jÃ¡ publicada em `AnalyzerReleases.Shipped.md` na base de comparaÃ§Ã£o precisa permanecer no release ativo ou estar preservada no histÃ³rico v1.
- Quando um novo `ARCH###` aparece em `RuleIdentifiers.cs`, o `AnalyzerReleases.Unshipped.md` do pacote correspondente precisa conter esse ID.

O workflow `.github/workflows/release-check.yml` executa essas validaÃ§Ãµes em `pull_request`, em `push` para `main` e manualmente via `workflow_dispatch`.

## Versao de release

O workflow `.github/workflows/release.yml` usa GitVersion como fonte Ãºnica da versÃ£o publicada. O job `validate` executa `gittools/actions/gitversion/setup` e `gittools/actions/gitversion/execute`, com checkout em `fetch-depth: 0`, e usa o output `semVer` para definir o `PackageVersion` do `dotnet pack`, o nome dos pacotes `.nupkg` e `.snupkg`, a tag `v{SemVer}` e o nome da GitHub Release `Swa.Analyzers v{SemVer}`.

O `GitVersion.yml` usa `workflow: TrunkBased/preview1`, que no GitVersion 6.x habilita a estratÃ©gia `Mainline`. A sintaxe antiga `mode: Mainline` nÃ£o Ã© aceita pela CLI 6.x.

NÃ£o atualize `VersionPrefix` manualmente para preparar release. O projeto nÃ£o usa mais `VersionPrefix` como fonte da versÃ£o publicada; commits semÃ¢nticos e tags existentes determinam a prÃ³xima versÃ£o via `GitVersion.yml`.

`1.0.0` Ã© a primeira versÃ£o estÃ¡vel do pacote. A partir dela, o projeto segue Semantic Versioning:

- `MAJOR`: mudanÃ§as incompatÃ­veis para consumidores, como remocao ou renomeaÃ§Ã£o de regra, alteraÃ§Ã£o incompativel de empacotamento, remocao de opÃ§Ã£o pÃºblica ou aumento restritivo de severidade padrÃ£o.
- `MINOR`: novas regras ARCH, novas opÃ§Ãµes pÃºblicas ou capacidades compatÃ­veis.
- `PATCH`: correÃ§Ãµes compatÃ­veis de bugs, falsos positivos, falsos negativos, documentaÃ§Ã£o, exemplos, build ou empacotamento.

NuGet.org nÃ£o permite reutilizar uma versÃ£o de pacote jÃ¡ publicada. Antes de publicar, confirme que a versÃ£o calculada pelo GitVersion, `CHANGELOG.md`, tag, GitHub Release e artefatos locais apontam para uma versÃ£o ainda nÃ£o publicada.

As tags de release seguem o formato `vX.Y.Z`, por exemplo `v1.1.1`, `v1.2.0` ou `v2.0.0`.

Commits semÃ¢nticos influenciam o incremento calculado:

- `fix: corrige falso positivo em rota HTTP` gera `PATCH`.
- `perf: reduz alocaÃ§Ãµes no analyzer` gera `PATCH`.
- `feat: adiciona nova regra ARCH016` gera `MINOR`.
- `feat!: altera contrato de configuraÃ§Ã£o` gera `MAJOR`.
- `BREAKING CHANGE:` no corpo do commit gera `MAJOR`.
- `docs:`, `test:`, `style:`, `chore:` e `ci:` nÃ£o forÃ§am incremento, salvo quando usam `!` ou `BREAKING CHANGE:`.

Antes de reexecutar uma release que falhou por duplicidade, verifique a versÃ£o calculada pelo GitVersion, o histÃ³rico de tags e as mensagens dos commits desde a Ãºltima tag.

A publicaÃ§Ã£o no NuGet.org permanece comentada no workflow atÃ© que o secret `NUGET_API_KEY` e um environment protegido sejam configurados explicitamente no repositÃ³rio. NÃ£o habilite o step de publicaÃ§Ã£o sem revisÃ£o de governanÃ§a.
