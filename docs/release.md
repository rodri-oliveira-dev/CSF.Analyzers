# Release checks

O repositorio usa `scripts/Validate-Release.ps1` para validar consistencia entre regras ARCH, documentacao, testes, SampleApp, changelog e versao do pacote.

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
- Quando `VersionPrefix` muda em `src/Swa.Analyzers.Core/Swa.Analyzers.Core.csproj`, `CHANGELOG.md` tambem precisa mudar no mesmo diff.
- Quando um novo `ARCH###` aparece em `RuleIdentifiers.cs`, `src/Swa.Analyzers.Core/AnalyzerReleases.Unshipped.md` precisa conter esse ID.

O workflow `.github/workflows/release-check.yml` executa essas validacoes em `pull_request`, em `push` para `main` e manualmente via `workflow_dispatch`.

## Versao de release

O workflow `.github/workflows/release.yml` usa o `VersionPrefix` de `src/Swa.Analyzers.Core/Swa.Analyzers.Core.csproj` como versao oficial do pacote. Essa versao define o `PackageVersion` do `dotnet pack`, o nome dos pacotes `.nupkg` e `.snupkg`, a tag `v{VersionPrefix}` e o nome da GitHub Release `Swa.Analyzers v{VersionPrefix}`.

`1.0.0` e a primeira versao estavel do pacote. A partir dela, o projeto segue Semantic Versioning:

- `MAJOR`: mudancas incompativeis para consumidores, como remocao ou renomeacao de regra, alteracao incompativel de empacotamento, remocao de opcao publica ou aumento restritivo de severidade padrao.
- `MINOR`: novas regras ARCH, novas opcoes publicas ou capacidades compativeis.
- `PATCH`: correcoes compativeis de bugs, falsos positivos, falsos negativos, documentacao, exemplos, build ou empacotamento.

NuGet.org nao permite reutilizar uma versao de pacote ja publicada. Antes de publicar, confirme que `VersionPrefix`, `CHANGELOG.md`, tag, GitHub Release e artefatos locais apontam para uma versao ainda nao publicada.

A publicacao no NuGet.org permanece comentada no workflow ate que o secret `NUGET_API_KEY` e um environment protegido sejam configurados explicitamente no repositorio. Nao habilite o step de publicacao sem revisao de governanca.
