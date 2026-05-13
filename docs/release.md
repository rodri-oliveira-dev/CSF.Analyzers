# Release checks

O repositorio usa `scripts/Validate-Release.ps1` para validar consistencia entre regras ARCH, documentacao, testes, SampleApp e metadados de release.

## Regras shipped e unshipped

O projeto separa os metadados de regras em dois arquivos:

- `src/Swa.Analyzers.Core/AnalyzerReleases.Shipped.md`: regras ja publicadas em alguma versao estavel do pacote.
- `src/Swa.Analyzers.Core/AnalyzerReleases.Unshipped.md`: regras novas ou alteracoes de regras ainda nao publicadas.

O formato segue o padrao de release tracking de analyzers Roslyn: uma secao de release, como `## Release 1.0.0`, e subsecoes como `### New Rules` com tabela de `Rule ID`, `Category`, `Severity` e `Notes`.

Como transicao conservadora, o baseline publicado em `1.0.0` registra `ARCH001` a `ARCH032` em `AnalyzerReleases.Shipped.md`, conforme o `CHANGELOG.md`. A regra `ARCH033` permanece em `AnalyzerReleases.Unshipped.md` enquanto estiver em `[Unreleased]`. Nao mova regras para `Shipped` sem base no historico do repositorio, tag, changelog ou pacote publicado.

Fluxo esperado:

1. Ao criar uma nova regra, adicione o ID em `RuleIdentifiers.cs` e registre a regra em `AnalyzerReleases.Unshipped.md`.
2. Antes de publicar, confirme a versao calculada pelo GitVersion, o `CHANGELOG.md`, docs, README, testes e SampleApp.
3. Depois que uma versao for efetivamente publicada, mova as regras publicadas de `Unshipped` para uma nova secao em `AnalyzerReleases.Shipped.md`, preservando ID, categoria, severidade e notas coerentes com o `DiagnosticDescriptor`.
4. Nunca remova ou renomeie IDs publicados. Remocao, renomeacao, aumento restritivo de severidade padrao ou mudanca incompativel de comportamento deve ser tratada como breaking change.

## Execucao local

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-Release.ps1
```

O script tenta comparar o estado atual com o upstream da branch. Quando necessario, informe refs explicitamente:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-Release.ps1 -BaseRef origin/main -HeadRef HEAD
```

O hook `.githooks/pre-push` executa a mesma validacao antes do restore, build, testes e cobertura.

## Validacoes

- Cada analyzer `Arch###*.cs` em `src/Swa.Analyzers.Core/Rules` precisa ter entrada em `RuleIdentifiers.cs`.
- Cada `ARCH###` em `RuleIdentifiers.cs` precisa ter analyzer `src/Swa.Analyzers.Core/Rules/Arch###*.cs`, `docs/rules/ARCH###.md`, teste `tests/Swa.Analyzers.Tests/Rules/Arch###*Tests.cs` e pasta `src/Swa.Analyzers.SampleApp/Arch###`.
- Cada `ARCH###` declarado em `RuleIdentifiers.cs` precisa aparecer no `README.md`.
- Cada `ARCH###` declarado em `RuleIdentifiers.cs` precisa aparecer em exatamente um dos metadados de release: `AnalyzerReleases.Shipped.md` ou `AnalyzerReleases.Unshipped.md`.
- Nenhum `ARCH###` pode aparecer nos metadados de release sem entrada correspondente em `RuleIdentifiers.cs`.
- Um `ARCH###` nao pode aparecer simultaneamente em `Shipped` e `Unshipped`.
- Uma regra ja publicada em `AnalyzerReleases.Shipped.md` na base de comparacao nao pode desaparecer do arquivo atual.
- Quando um novo `ARCH###` aparece em `RuleIdentifiers.cs`, `src/Swa.Analyzers.Core/AnalyzerReleases.Unshipped.md` precisa conter esse ID.

O workflow `.github/workflows/release-check.yml` executa essas validacoes em `pull_request`, em `push` para `main` e manualmente via `workflow_dispatch`.

## Versao de release

O workflow `.github/workflows/release.yml` usa GitVersion como fonte unica da versao publicada. O job `validate` executa `gittools/actions/gitversion/setup` e `gittools/actions/gitversion/execute`, com checkout em `fetch-depth: 0`, e usa o output `semVer` para definir o `PackageVersion` do `dotnet pack`, o nome dos pacotes `.nupkg` e `.snupkg`, a tag `v{SemVer}` e o nome da GitHub Release `Swa.Analyzers v{SemVer}`.

O `GitVersion.yml` usa `workflow: TrunkBased/preview1`, que no GitVersion 6.x habilita a estrategia `Mainline`. A sintaxe antiga `mode: Mainline` nao e aceita pela CLI 6.x.

Nao atualize `VersionPrefix` manualmente para preparar release. O projeto nao usa mais `VersionPrefix` como fonte da versao publicada; commits semanticos e tags existentes determinam a proxima versao via `GitVersion.yml`.

`1.0.0` e a primeira versao estavel do pacote. A partir dela, o projeto segue Semantic Versioning:

- `MAJOR`: mudancas incompativeis para consumidores, como remocao ou renomeacao de regra, alteracao incompativel de empacotamento, remocao de opcao publica ou aumento restritivo de severidade padrao.
- `MINOR`: novas regras ARCH, novas opcoes publicas ou capacidades compativeis.
- `PATCH`: correcoes compativeis de bugs, falsos positivos, falsos negativos, documentacao, exemplos, build ou empacotamento.

NuGet.org nao permite reutilizar uma versao de pacote ja publicada. Antes de publicar, confirme que a versao calculada pelo GitVersion, `CHANGELOG.md`, tag, GitHub Release e artefatos locais apontam para uma versao ainda nao publicada.

As tags de release seguem o formato `vX.Y.Z`, por exemplo `v1.1.1`, `v1.2.0` ou `v2.0.0`.

Commits semanticos influenciam o incremento calculado:

- `fix: corrige falso positivo em rota HTTP` gera `PATCH`.
- `perf: reduz alocacoes no analyzer` gera `PATCH`.
- `feat: adiciona nova regra ARCH016` gera `MINOR`.
- `feat!: altera contrato de configuracao` gera `MAJOR`.
- `BREAKING CHANGE:` no corpo do commit gera `MAJOR`.
- `docs:`, `test:`, `style:`, `chore:` e `ci:` nao forcam incremento, salvo quando usam `!` ou `BREAKING CHANGE:`.

Antes de reexecutar uma release que falhou por duplicidade, verifique a versao calculada pelo GitVersion, o historico de tags e as mensagens dos commits desde a ultima tag.

A publicacao no NuGet.org permanece comentada no workflow ate que o secret `NUGET_API_KEY` e um environment protegido sejam configurados explicitamente no repositorio. Nao habilite o step de publicacao sem revisao de governanca.
