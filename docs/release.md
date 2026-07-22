# Validacoes de release

O repositorio usa `scripts/Validate-Release.ps1` para validar consistencia entre regras, documentacao, testes, samples e metadados de release dos tres pacotes ativos.

## Regras shipped e unshipped

O projeto separa os metadados de regras em dois arquivos por pacote:

- `src/Swa.Analyzers.{Reliability,Architecture,Testing}/AnalyzerReleases.Shipped.md`: regras ja publicadas em alguma versao estavel do pacote correspondente.
- `src/Swa.Analyzers.{Reliability,Architecture,Testing}/AnalyzerReleases.Unshipped.md`: regras novas ou alteracoes de regras ainda nao publicadas.

Na migracao para a versao 2.0, os IDs `REL###`, `ARC###` e `TST###` ficam em `AnalyzerReleases.Unshipped.md` ate a publicacao efetiva. O historico da linha 1.x fica preservado em `docs/history/v1-analyzer-releases.md` e `docs/migration-v2.md`.

Fluxo esperado:

1. Ao criar uma nova regra, adicione o ID em `RuleIdentifiers.cs` e registre a regra em `AnalyzerReleases.Unshipped.md`.
2. Antes de publicar, confirme a versao calculada pelo GitVersion, o `CHANGELOG.md`, docs, README, testes e samples.
3. Depois que uma versao for efetivamente publicada, mova as regras publicadas de `Unshipped` para uma nova secao em `AnalyzerReleases.Shipped.md`, preservando ID, categoria, severidade e notas coerentes com o `DiagnosticDescriptor`.
4. Nunca remova ou renomeie IDs publicados sem tratar a mudanca como breaking change.

## Execucao local

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-Release.ps1
```

## Validacoes

- Cada analyzer `Rel###*.cs`, `Arc###*.cs` ou `Tst###*.cs` precisa ter entrada no `RuleIdentifiers.cs` do pacote.
- Cada ID declarado em `RuleIdentifiers.cs` precisa ter analyzer no pacote correspondente, documento em `docs/rules/<ID>.md`, teste em `tests/Swa.Analyzers.*.Tests/Rules/<Prefix><Number>*Tests.cs` e sample em `samples/Swa.Analyzers.*.Sample/<Prefix><Number>`.
- Cada ID declarado em `RuleIdentifiers.cs` precisa aparecer no `README.md`.
- Cada ID declarado em `RuleIdentifiers.cs` precisa aparecer em exatamente um dos metadados de release: `AnalyzerReleases.Shipped.md` ou `AnalyzerReleases.Unshipped.md`.
- Nenhum ID pode aparecer nos metadados de release sem entrada correspondente em `RuleIdentifiers.cs`.
- Um ID nao pode aparecer simultaneamente em `Shipped` e `Unshipped`.
- Cada prefixo precisa pertencer ao pacote correto: `REL###` em `Swa.Analyzers.Reliability`, `ARC###` em `Swa.Analyzers.Architecture` e `TST###` em `Swa.Analyzers.Testing`.
- Nenhum ID pode aparecer duplicado globalmente entre pacotes.
- IDs historicos `ARCH###` podem permanecer em documentos historicos e de migracao, mas nao sao exigidos como implementacao ativa.
- Os help links dos analyzers devem usar `RuleHelpLinks.ForRule(...)` e apontar para `docs/rules/<ID>.md`.
- Opcoes publicas `dotnet_diagnostic.<ID>.<option>` implementadas precisam estar documentadas, e opcoes documentadas precisam existir na implementacao.

## Inspecao dos pacotes

O script `scripts/Inspect-NuGetPackages.ps1` valida os artefatos gerados para uma versao especifica:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/Inspect-NuGetPackages.ps1 -PackageDirectory ./artifacts/packages -Version 2.0.0
```

O diretório de pacotes deve conter exatamente:

- `Swa.Analyzers.Reliability.<versao>.nupkg`
- `Swa.Analyzers.Reliability.<versao>.snupkg`
- `Swa.Analyzers.Architecture.<versao>.nupkg`
- `Swa.Analyzers.Architecture.<versao>.snupkg`
- `Swa.Analyzers.Testing.<versao>.nupkg`
- `Swa.Analyzers.Testing.<versao>.snupkg`

A inspeção abre cada arquivo e confirma package ID, versao, repository URL, metadata de README, assembly correto em `analyzers/dotnet/cs`, ausencia de DLLs compartilhadas ou legadas e simbolos no `.snupkg`.

O workflow `.github/workflows/release-check.yml` executa essas validacoes em `pull_request`, em `push` para `main` e manualmente via `workflow_dispatch`.

## Versao de release

O workflow `.github/workflows/release.yml` usa GitVersion como fonte unica da versao publicada. O job `validate` executa `gittools/actions/gitversion/setup` e `gittools/actions/gitversion/execute`, com checkout em `fetch-depth: 0`, e usa o output `semVer` para definir o `PackageVersion` do `dotnet pack`, o nome dos pacotes `.nupkg` e `.snupkg`, a tag `v{SemVer}` e o nome da GitHub Release `Swa.Analyzers v{SemVer}`.

O `GitVersion.yml` usa `workflow: TrunkBased/preview1`, que no GitVersion 6.x habilita a estrategia `Mainline`. A sintaxe antiga `mode: Mainline` nao e aceita pela CLI 6.x.

Nao atualize `VersionPrefix` manualmente para preparar release. O projeto nao usa mais `VersionPrefix` como fonte da versao publicada; commits semanticos e tags existentes determinam a proxima versao via `GitVersion.yml`.

A partir de `1.0.0`, o projeto segue Semantic Versioning:

- `MAJOR`: mudancas incompativeis para consumidores, como remocao ou renomeacao de regra, alteracao incompativel de empacotamento, remocao de opcao publica ou aumento restritivo de severidade padrao.
- `MINOR`: novas regras, novas opcoes publicas ou capacidades compativeis.
- `PATCH`: correcoes compativeis de bugs, falsos positivos, falsos negativos, documentacao, exemplos, build ou empacotamento.

NuGet.org nao permite reutilizar uma versao de pacote ja publicada. Antes de publicar, confirme que a versao calculada pelo GitVersion, `CHANGELOG.md`, tag, GitHub Release e artefatos locais apontam para uma versao ainda nao publicada.

As tags de release seguem o formato `vX.Y.Z`, por exemplo `v1.1.1`, `v1.2.0` ou `v2.0.0`.

Commits semanticos influenciam o incremento calculado:

- `fix: corrige falso positivo em rota HTTP` gera `PATCH`.
- `perf: reduz alocacoes no analyzer` gera `PATCH`.
- `feat: adiciona nova regra REL005` gera `MINOR`.
- `feat!: altera contrato de configuracao` gera `MAJOR`.
- `BREAKING CHANGE:` no corpo do commit gera `MAJOR`.
- `docs:`, `test:`, `style:`, `chore:` e `ci:` nao forcam incremento, salvo quando usam `!` ou `BREAKING CHANGE:`.

Antes de reexecutar uma release que falhou por duplicidade, verifique a versao calculada pelo GitVersion, o historico de tags e as mensagens dos commits desde a ultima tag.

A publicacao no NuGet.org permanece comentada no workflow ate que o secret `NUGET_API_KEY` e um environment protegido sejam configurados explicitamente no repositorio. Nao habilite o step de publicacao sem revisao de governanca.
