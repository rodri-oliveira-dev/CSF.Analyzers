# Release checks

O repositorio usa `scripts/Validate-Release.ps1` para validar consistencia entre regras ARCH, documentacao, testes, SampleApp e metadados de release.

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
- Cada analyzer precisa ter `docs/rules/ARCH###.md`, teste `tests/Swa.Analyzers.Tests/Rules/Arch###*Tests.cs` e pasta `src/Swa.Analyzers.SampleApp/Arch###`.
- Cada `ARCH###` declarado em `RuleIdentifiers.cs` precisa aparecer no `README.md`.
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
