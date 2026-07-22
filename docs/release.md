# Validações de release

O repositório usa `scripts/Validate-Release.ps1` para validar consistência entre regras ARCH, documentação, testes, SampleApp e metadados de release.

## Regras shipped e unshipped

O projeto separa os metadados de regras em dois arquivos:

- `src/Swa.Analyzers.Core/AnalyzerReleases.Shipped.md`: regras já publicadas em alguma versão estável do pacote.
- `src/Swa.Analyzers.Core/AnalyzerReleases.Unshipped.md`: regras novas ou alterações de regras ainda não publicadas.

O formato segue o padrão de release tracking de analyzers Roslyn: uma seção de release, como `## Release 1.0.0`, e subsecoes como `### New Rules` com tabela de `Rule ID`, `Category`, `Severity` e `Notes`.

Na migração para a versão 2.0, `AnalyzerReleases.Shipped.md` permanece como metadata ativa das regras ainda implementadas. O histórico dos IDs da linha 1.x fica preservado em `docs/history/v1-analyzer-releases.md`. Não mova regras para `Shipped` sem base no histórico do repositório, tag, changelog ou pacote publicado.

Fluxo esperado:

1. Ao criar uma nova regra, adicione o ID em `RuleIdentifiers.cs` e registre a regra em `AnalyzerReleases.Unshipped.md`.
2. Antes de publicar, confirme a versão calculada pelo GitVersion, o `CHANGELOG.md`, docs, README, testes e SampleApp.
3. Depois que uma versão for efetivamente publicada, mova as regras publicadas de `Unshipped` para uma nova seção em `AnalyzerReleases.Shipped.md`, preservando ID, categoria, severidade e notas coerentes com o `DiagnosticDescriptor`.
4. Nunca remova ou renomeie IDs publicados. Remoção, renomeação, aumento restritivo de severidade padrão ou mudança incompativel de comportamento deve ser tratada como breaking change.

## Execucao local

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-Release.ps1
```

O script tenta comparar o estado atual com o upstream da branch. Quando necessário, informe refs explicitamente:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-Release.ps1 -BaseRef origin/main -HeadRef HEAD
```

O hook `.githooks/pre-push` executa a mesma validação antes do restore, build, testes e cobertura.

## Validacoes

- Cada analyzer `Arch###*.cs` em `src/Swa.Analyzers.Core/Rules` precisa ter entrada em `RuleIdentifiers.cs`.
- Cada `ARCH###` em `RuleIdentifiers.cs` precisa ter analyzer `src/Swa.Analyzers.Core/Rules/Arch###*.cs`, `docs/rules/ARCH###.md`, teste `tests/Swa.Analyzers.Tests/Rules/Arch###*Tests.cs` e pasta `src/Swa.Analyzers.SampleApp/Arch###`.
- Cada `ARCH###` declarado em `RuleIdentifiers.cs` precisa aparecer no `README.md`.
- Cada `ARCH###` declarado em `RuleIdentifiers.cs` precisa aparecer em exatamente um dos metadados de release: `AnalyzerReleases.Shipped.md` ou `AnalyzerReleases.Unshipped.md`.
- Nenhum `ARCH###` pode aparecer nos metadados de release sem entrada correspondente em `RuleIdentifiers.cs`.
- Um `ARCH###` não pode aparecer simultaneamente em `Shipped` e `Unshipped`.
- Uma regra já publicada em `AnalyzerReleases.Shipped.md` na base de comparação precisa permanecer no release ativo ou estar preservada no histórico v1.
- Quando um novo `ARCH###` aparece em `RuleIdentifiers.cs`, `src/Swa.Analyzers.Core/AnalyzerReleases.Unshipped.md` precisa conter esse ID.

O workflow `.github/workflows/release-check.yml` executa essas validações em `pull_request`, em `push` para `main` e manualmente via `workflow_dispatch`.

## Versao de release

O workflow `.github/workflows/release.yml` usa GitVersion como fonte única da versão publicada. O job `validate` executa `gittools/actions/gitversion/setup` e `gittools/actions/gitversion/execute`, com checkout em `fetch-depth: 0`, e usa o output `semVer` para definir o `PackageVersion` do `dotnet pack`, o nome dos pacotes `.nupkg` e `.snupkg`, a tag `v{SemVer}` e o nome da GitHub Release `Swa.Analyzers v{SemVer}`.

O `GitVersion.yml` usa `workflow: TrunkBased/preview1`, que no GitVersion 6.x habilita a estratégia `Mainline`. A sintaxe antiga `mode: Mainline` não é aceita pela CLI 6.x.

Não atualize `VersionPrefix` manualmente para preparar release. O projeto não usa mais `VersionPrefix` como fonte da versão publicada; commits semânticos e tags existentes determinam a próxima versão via `GitVersion.yml`.

`1.0.0` é a primeira versão estável do pacote. A partir dela, o projeto segue Semantic Versioning:

- `MAJOR`: mudanças incompatíveis para consumidores, como remocao ou renomeação de regra, alteração incompativel de empacotamento, remocao de opção pública ou aumento restritivo de severidade padrão.
- `MINOR`: novas regras ARCH, novas opções públicas ou capacidades compatíveis.
- `PATCH`: correções compatíveis de bugs, falsos positivos, falsos negativos, documentação, exemplos, build ou empacotamento.

NuGet.org não permite reutilizar uma versão de pacote já publicada. Antes de publicar, confirme que a versão calculada pelo GitVersion, `CHANGELOG.md`, tag, GitHub Release e artefatos locais apontam para uma versão ainda não publicada.

As tags de release seguem o formato `vX.Y.Z`, por exemplo `v1.1.1`, `v1.2.0` ou `v2.0.0`.

Commits semânticos influenciam o incremento calculado:

- `fix: corrige falso positivo em rota HTTP` gera `PATCH`.
- `perf: reduz alocações no analyzer` gera `PATCH`.
- `feat: adiciona nova regra ARCH016` gera `MINOR`.
- `feat!: altera contrato de configuração` gera `MAJOR`.
- `BREAKING CHANGE:` no corpo do commit gera `MAJOR`.
- `docs:`, `test:`, `style:`, `chore:` e `ci:` não forçam incremento, salvo quando usam `!` ou `BREAKING CHANGE:`.

Antes de reexecutar uma release que falhou por duplicidade, verifique a versão calculada pelo GitVersion, o histórico de tags e as mensagens dos commits desde a última tag.

A publicação no NuGet.org permanece comentada no workflow até que o secret `NUGET_API_KEY` e um environment protegido sejam configurados explicitamente no repositório. Não habilite o step de publicação sem revisão de governança.
