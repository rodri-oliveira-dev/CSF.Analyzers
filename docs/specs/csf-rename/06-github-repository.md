# CSF rename - GitHub repository

## Estado inicial

- Worktree limpo antes da implementacao.
- A etapa anterior terminou verde conforme `docs/specs/csf-rename/plan.md`:
  - `dotnet restore ./CSF.Analyzers.slnx`
  - `dotnet build ./CSF.Analyzers.slnx --configuration Release --no-restore`
  - `dotnet test ./CSF.Analyzers.slnx --configuration Release -m:1`
  - `powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-Release.ps1`
  - `dotnet pack ./CSF.Analyzers.slnx --configuration Release --no-build --output ./artifacts/csf-rename-baseline /p:PackageVersion=0.0.0-csf-baseline`
  - `powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Inspect-NuGetPackages.ps1 -PackageDirectory ./artifacts/csf-rename-baseline -Version '0.0.0-csf-baseline'`
  - `powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-AnalyzerPackageIsolation.ps1 -PackageDirectory ./artifacts/csf-rename-baseline -Version '0.0.0-csf-baseline'`
- A etapa 04 ja migrou `RepositoryUrl`, `PackageProjectUrl` e help links ativos para a URL alvo.
- A etapa 05 preservou URLs antigas somente em specs migratorias porque o repositorio remoto ainda nao tinha sido renomeado.

## Repositorio remoto

| Item | Valor |
| ---- | ----- |
| Nome remoto atual | `rodri-oliveira-dev/Swa.Analyzers` |
| Nome remoto desejado | `rodri-oliveira-dev/CSF.Analyzers` |
| URL HTTPS atual | `https://github.com/rodri-oliveira-dev/Swa.Analyzers` |
| URL HTTPS desejada | `https://github.com/rodri-oliveira-dev/CSF.Analyzers` |
| URL SSH atual | `git@github.com:rodri-oliveira-dev/Swa.Analyzers.git` |
| URL SSH desejada | `git@github.com:rodri-oliveira-dev/CSF.Analyzers.git` |
| Branch default esperada | `main` |

## URLs internas existentes

Inventario executado antes do rename, excluindo `.git`, `bin`, `obj` e `artifacts`:

- URLs ativas ja apontam para `https://github.com/rodri-oliveira-dev/CSF.Analyzers`.
- URLs antigas `rodri-oliveira-dev/Swa.Analyzers` permanecem em:
  - `docs/specs/csf-rename/plan.md`, como estado inicial e inventario historico;
  - `docs/specs/csf-rename/02-source-projects.md`, como excecao temporaria da etapa 02;
  - `docs/specs/csf-rename/04-packaging-release.md`, como estado inicial e risco temporario;
  - `docs/specs/csf-rename/05-documentation.md`, como excecao temporaria antes desta etapa.

## RepositoryUrl

`Directory.Build.props` define:

```xml
<RepositoryUrl>https://github.com/rodri-oliveira-dev/CSF.Analyzers</RepositoryUrl>
```

O script `scripts/Inspect-NuGetPackages.ps1` tambem valida a mesma URL nos nuspecs gerados.

## PackageProjectUrl

`Directory.Build.props` define:

```xml
<PackageProjectUrl>https://github.com/rodri-oliveira-dev/CSF.Analyzers</PackageProjectUrl>
```

## Help links

`src/CSF.Analyzers.Common/RuleHelpLinks.cs` gera help links com a base:

```text
https://github.com/rodri-oliveira-dev/CSF.Analyzers/blob/main/docs/rules/
```

`tests/CSF.Analyzers.PackageValidation.Tests/AnalyzerPackageIsolationTests.cs` valida essa mesma base para todos os descriptors.

## Referencias em docs

- Documentacao publica e operacional ja usa `CSF.Analyzers`.
- Referencias restantes ao slug antigo em `docs/specs/csf-rename/**` sao historicas ou descrevem excecoes temporarias das etapas anteriores.
- Esta etapa deve atualizar a spec 05 para remover a excecao temporaria do slug remoto antigo.
- Specs anteriores podem manter referencias historicas ao estado antigo quando a semantica do texto for "antes da migracao".

## Impacto no origin

O remote local `origin` inicia como:

```text
origin  git@github.com:rodri-oliveira-dev/Swa.Analyzers.git (fetch)
origin  git@github.com:rodri-oliveira-dev/Swa.Analyzers.git (push)
```

Apos o rename remoto bem-sucedido, `origin` deve ser atualizado explicitamente para:

```text
origin  git@github.com:rodri-oliveira-dev/CSF.Analyzers.git (fetch)
origin  git@github.com:rodri-oliveira-dev/CSF.Analyzers.git (push)
```

Nao depender apenas do redirect automatico do GitHub.

## Criterios de aceite

- `rodri-oliveira-dev/CSF.Analyzers` existe no GitHub.
- O repositorio novo corresponde ao mesmo repository id confirmado antes do rename.
- `rodri-oliveira-dev/Swa.Analyzers` nao e mais o nome canonico do repositorio.
- `origin` local aponta explicitamente para `git@github.com:rodri-oliveira-dev/CSF.Analyzers.git`.
- URLs ativas em metadata NuGet, package project URL, help links, scripts, workflows, badges, README, docs publicas e `.agents` apontam para `CSF.Analyzers`.
- URLs antigas restantes sao somente historicas, migratorias ou inventario de specs anteriores.
- Restore, build, testes, release validation, pack e inspecoes de pacote passam depois do rename.
- O diff e revisado antes do commit.
- Exatamente um commit e criado com a mensagem `chore: update repository identity to CSF.Analyzers`.
- Nao fazer push de commit nesta etapa.

## Rollback

- Se o rename remoto falhar, nao atualizar arquivos locais nem `origin` para o slug novo.
- Se o rename remoto ocorrer mas validacoes locais falharem, corrigir localmente antes do commit quando a causa estiver nesta etapa.
- Se for necessario desfazer o rename remoto antes do commit, renomear o repositorio de volta para `Swa.Analyzers` via GitHub CLI/API e restaurar `origin` para `git@github.com:rodri-oliveira-dev/Swa.Analyzers.git`.
- Se o commit desta etapa ja existir e precisar ser revertido, usar `git revert`, sem reescrever historico.
- Nao usar `git reset --hard` e nao reescrever historico Git.

## Evidencias

Executado em 2026-07-23:

| Comando | Resultado |
| ------- | --------- |
| `gh repo view rodri-oliveira-dev/Swa.Analyzers --json nameWithOwner,id,url,sshUrl,defaultBranchRef,visibility` | Aprovado antes do rename; repositorio `rodri-oliveira-dev/Swa.Analyzers`, id `R_kgDOSElstg`, branch default `main`, publico. |
| `gh repo view rodri-oliveira-dev/CSF.Analyzers --json nameWithOwner,id,url,sshUrl,defaultBranchRef,visibility` | Antes do rename, retornou que o repositorio nao existia. |
| `gh repo rename CSF.Analyzers --repo rodri-oliveira-dev/Swa.Analyzers --yes` | Aprovado. |
| `gh repo view rodri-oliveira-dev/CSF.Analyzers --json nameWithOwner,id,url,sshUrl,defaultBranchRef,visibility` | Aprovado apos o rename; repositorio `rodri-oliveira-dev/CSF.Analyzers`, mesmo id `R_kgDOSElstg`, branch default `main`, publico. |
| `gh repo view rodri-oliveira-dev/Swa.Analyzers --json nameWithOwner,id,url,sshUrl,defaultBranchRef,visibility` | Aprovado apos o rename; o slug antigo resolve por redirect para `rodri-oliveira-dev/CSF.Analyzers` com o mesmo id `R_kgDOSElstg`. |
| `git remote set-url origin git@github.com:rodri-oliveira-dev/CSF.Analyzers.git` | Aprovado. |
| `git remote -v` | Aprovado; fetch e push apontam para `git@github.com:rodri-oliveira-dev/CSF.Analyzers.git`. |
| `dotnet restore ./CSF.Analyzers.slnx` | Aprovado; todos os projetos atualizados para restauracao. |
| `dotnet build ./CSF.Analyzers.slnx --configuration Release --no-restore` | Aprovado; warnings esperados dos samples invalidos e `EnableGenerateDocumentationFile`; 0 erros. |
| `dotnet test ./CSF.Analyzers.slnx --configuration Release -m:1` | Aprovado; 246 testes, 0 falhas. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-Release.ps1` | Aprovado; `release-check: validacoes aprovadas`. |
| `dotnet pack ./CSF.Analyzers.slnx --configuration Release --no-build --output ./artifacts/csf-rename-06-packages-20260723183000 /p:PackageVersion=0.0.0-csf-rename.6` | Aprovado; 3 `.nupkg` e 3 `.snupkg` gerados com prefixo `CSF.Analyzers.*`. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Inspect-NuGetPackages.ps1 -PackageDirectory ./artifacts/csf-rename-06-packages-20260723183000 -Version '0.0.0-csf-rename.6'` | Aprovado; package inspection aprovada. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-AnalyzerPackageIsolation.ps1 -PackageDirectory ./artifacts/csf-rename-06-packages-20260723183000 -Version '0.0.0-csf-rename.6'` | Aprovado; 3 testes de isolamento e package inspection aprovados. |
| `dotnet restore ./CSF.Analyzers.slnx --locked-mode` | Aprovado. |

## Resultado final

- Repositorio GitHub canonico: `rodri-oliveira-dev/CSF.Analyzers`.
- Repository id preservado: `R_kgDOSElstg`.
- URL HTTPS canonica: `https://github.com/rodri-oliveira-dev/CSF.Analyzers`.
- URL SSH canonica: `git@github.com:rodri-oliveira-dev/CSF.Analyzers.git`.
- `origin` local aponta explicitamente para `git@github.com:rodri-oliveira-dev/CSF.Analyzers.git`.
- Metadata ativa de NuGet, `PackageProjectUrl`, help links, scripts e validacoes de pacote apontam para `CSF.Analyzers`.
- Ocorrencias restantes de `Swa.Analyzers` nas specs da migracao sao historicas, migratorias, inventario de etapas anteriores ou rollback.
