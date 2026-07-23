# CSF rename SDD plan

## Objetivo

Definir a especificacao oficial para migrar a identidade atual do repositorio `rodri-oliveira-dev/Swa.Analyzers` para `CSF`, sem executar a renomeacao nesta etapa.

Este documento e a fonte de verdade para os proximos prompts da migracao. Qualquer etapa futura deve preservar os IDs `REL###`, `ARC###` e `TST###`, que nao fazem parte da renomeacao.

## Estado atual

- Identidade principal: `Swa.Analyzers`.
- Repositorio publico referenciado em metadados: `https://github.com/rodri-oliveira-dev/Swa.Analyzers`.
- Solucao principal: `Swa.Analyzers.slnx`.
- Pacotes ativos:
  - `Swa.Analyzers.Reliability`
  - `Swa.Analyzers.Architecture`
  - `Swa.Analyzers.Testing`
- Projetos de analyzer em `src/Swa.Analyzers.*`.
- Testes em `tests/Swa.Analyzers.*`.
- Samples em `samples/Swa.Analyzers.*.Sample`.
- Namespaces publicos/internos usam `Swa.Analyzers.*`.
- Scripts e workflows validam explicitamente nomes de pacotes, assemblies, PDBs, caminhos, release names e repository URL com `Swa.Analyzers`.
- Ha arquivos de IDE `.vs` e `.vscode` rastreados no Git que contem ou carregam a identidade atual, mas deveriam ser tratados antes da migracao de produto.

## Estado desejado

- Identidade canonica: `CSF`.
- Produto principal: `CSF.Analyzers`.
- Repositorio e metadados publicos devem apontar para a identidade `CSF.Analyzers`, incluindo repository URL, package project URL, release name e referencias de documentacao.
- Pacotes esperados:
  - `CSF.Analyzers.Reliability`
  - `CSF.Analyzers.Architecture`
  - `CSF.Analyzers.Testing`
- Projetos, assemblies, PDBs, namespaces, usings, samples, testes, scripts, workflows e documentacao devem convergir para `CSF.Analyzers.*`.
- Nomes historicos `Swa.Analyzers` devem permanecer apenas onde forem documentacao historica deliberada, migracao de usuarios ou compatibilidade temporaria explicitamente aprovada.

## Convencao de casing

| Origem | Destino | Observacao |
| ------ | ------- | ---------- |
| `Swa` | `CSF` | Uso padrao em namespaces, tipos, projetos, pacotes e docs. |
| `SWA` | `CSF` | Uso em texto uppercase, constantes ou siglas. |
| `swa` | `csf` | Somente quando o contexto exigir lowercase, como paths normalizados, IDs de cache, labels ou comandos. |
| `Swa.Analyzers` | `CSF.Analyzers` | Forma canonica do produto e prefixo de pacotes. |

Nao aplicar substituicao cega em palavras nao relacionadas. Exemplo: `csharp_style_prefer_tuple_swap` contem `swa` dentro de `swap` e nao faz parte da identidade.

## Escopo

- Renomear identidade de produto em codigo, namespaces, usings, assemblies, projetos, solucao, pacotes, docs, samples, testes, scripts, hooks e workflows.
- Atualizar metadados NuGet/MSBuild: `PackageId`, `RepositoryUrl`, `PackageProjectUrl`, `AssemblyName`, caminhos de analyzer, targets e validacoes de pacote.
- Atualizar comandos de build/test/pack/release que apontam para `Swa.Analyzers.slnx` ou projetos `Swa.Analyzers.*`.
- Atualizar docs publicas e internas para a nova identidade.
- Atualizar instrucoes de agentes e skills locais depois que a nova estrutura existir.
- Ajustar arquivos de workspace/configuracao quando forem deliberadamente mantidos no repositorio.
- Remover do Git arquivos rastreados que deveriam estar ignorados antes ou durante a migracao, quando isso for aprovado pela etapa especifica.

## Fora de escopo

- Renumerar, renomear ou alterar semantica dos IDs `REL###`, `ARC###` e `TST###`.
- Alterar comportamento de analyzers.
- Criar novas regras.
- Reescrever historico Git.
- Publicar no NuGet.org.
- Fazer push.
- Criar compatibilidade binaria por type forwarding sem uma etapa propria aprovada.
- Renomear referencias historicas quando o objetivo do texto for documentar a identidade antiga.

## Inventario de ocorrencias

Buscas executadas localmente em 2026-07-23, case-insensitive, no checkout inteiro excluindo `.git` e tambem restritas a arquivos rastreados pelo Git.

### Resumo global

| Busca | Arquivos varridos | Arquivos com ocorrencias | Ocorrencias em conteudo | Paths com `swa`/`Swa`/`SWA` |
| ----- | ----------------- | ------------------------ | ----------------------- | ---------------------------- |
| Checkout inteiro sem `.git` | 1635 | 557 | 8597 | 483 |
| Apenas arquivos rastreados | 227 | 174 | 1062 | 156 |

O checkout inteiro inclui artefatos ignorados de build/test/pack. Esses artefatos nao devem guiar contratos da migracao.

### Padroes rastreados por conteudo

| Padrao | Arquivos | Ocorrencias |
| ------ | -------- | ----------- |
| `Swa.Analyzers` | 168 | 1020 |
| palavra `Swa` | 168 | 1027 |
| palavra `SWA` | 0 | 0 |
| palavra `swa` | 4 | 6 |
| `rodri-oliveira-dev/Swa.Analyzers` | 4 | 5 |
| `https://github.com/rodri-oliveira-dev/Swa.Analyzers` | 4 | 5 |

### Classificacao obrigatoria

As categorias abaixo nao sao mutuamente exclusivas; um mesmo arquivo pode aparecer em mais de uma categoria quando o contexto for relevante.

| Categoria | Arquivos no escopo | Arquivos com ocorrencias | Ocorrencias | Exemplos relevantes |
| --------- | ------------------ | ------------------------ | ----------- | ------------------- |
| source code | 38 | 32 | 80 | `src/Swa.Analyzers.*`, `RuleIdentifiers.cs`, `RuleHelpLinks.cs`, `AnalyzerReleases.*.md` |
| namespaces/usings | 103 | 94 | 163 | namespaces `Swa.Analyzers.*`, usings de testes, namespaces `Swa.Analyzers.SampleApp.*` |
| solution/projects | 11 | 11 | 51 | `Swa.Analyzers.slnx`, `*.csproj` de src/tests/samples |
| tests | 43 | 43 | 91 | `tests/Swa.Analyzers.*.Tests`, `tests/Swa.Analyzers.TestSupport` |
| samples | 58 | 35 | 38 | `samples/Swa.Analyzers.*.Sample`, namespaces `Swa.Analyzers.SampleApp.*` |
| NuGet/MSBuild | 25 | 17 | 56 | `Directory.Build.props`, `Directory.Build.targets`, `packages.lock.json`, `buildTransitive/*.targets`, csproj |
| scripts | 3 | 3 | 30 | `Validate-Release.ps1`, `Inspect-NuGetPackages.ps1`, `Validate-AnalyzerPackageIsolation.ps1` |
| CI/workflows/hooks | 8 | 4 | 21 | `.github/workflows/dotnet.yml`, `release.yml`, `codeql.yml`, `.githooks/pre-push` |
| documentacao | 38 | 35 | 211 | `README.md`, `CHANGELOG.md`, `docs/packages`, `docs/rules`, `docs/migration-v2.md`, `docs/specs/next-analyzers` |
| configuracao | 18 | 13 | 494 | `.editorconfig`, `.gitignore`, `Swa.Analyzers.code-workspace`, `.vs`, `.vscode` |
| URLs | 227 | 14 | 52 | repository URL, package project URL, links externos em docs/specs |
| instrucoes de agentes | 7 | 6 | 45 | `.agents/skills/*/SKILL.md` |
| nomes de arquivos/diretorios | 156 | 156 | 156 | `Swa.Analyzers.slnx`, `src/Swa.Analyzers.*`, `tests/Swa.Analyzers.*`, `samples/Swa.Analyzers.*`, `.vs/Swa.Analyzers.slnx` |
| arquivos rastreados que deveriam estar ignorados | 13 | 13 | 13 | `.vs/*`, `.vscode/*` |

### Arquivos rastreados que deveriam estar ignorados

Os seguintes arquivos estao rastreados e devem ser tratados em etapa propria antes de depender do inventario de filesystem:

- `.vs/ProjectEvaluation/swa.analyzers.metadata.v10.bin`
- `.vs/ProjectEvaluation/swa.analyzers.projects.v10.bin`
- `.vs/ProjectEvaluation/swa.analyzers.strings.v10.bin`
- `.vs/Swa.Analyzers.slnx/DesignTimeBuild/.dtbcache.v2`
- `.vs/Swa.Analyzers.slnx/FileContentIndex/cf511b9d-91b3-4172-9aa0-77a8ed253c99.vsidx`
- `.vs/Swa.Analyzers.slnx/v18/.futdcache.v2`
- `.vs/Swa.Analyzers.slnx/v18/.suo`
- `.vs/Swa.Analyzers.slnx/v18/DocumentLayout.backup.json`
- `.vs/Swa.Analyzers.slnx/v18/DocumentLayout.json`
- `.vscode/extensions.json`
- `.vscode/launch.json`
- `.vscode/settings.json`
- `.vscode/tasks.json`

Arquivos ignorados presentes no checkout apos o baseline: 1408 no total, agrupados em `artifacts` 46, `bin` 971, `obj` 385 e `TestResults` 6.

## Arquivos e diretorios afetados

- Raiz:
  - `Swa.Analyzers.slnx`
  - `Swa.Analyzers.code-workspace`
  - `README.md`
  - `CHANGELOG.md`
  - `Directory.Build.props`
  - `Directory.Build.targets`
  - `Directory.Packages.props`
  - `.editorconfig`
  - `.gitignore`
  - `global.json`
  - `coverlet.runsettings`
- Source:
  - `src/Swa.Analyzers.Reliability`
  - `src/Swa.Analyzers.Architecture`
  - `src/Swa.Analyzers.Testing`
  - `src/Swa.Analyzers.Common`
  - `src/Swa.Analyzers.Architecture/buildTransitive/Swa.Analyzers.Architecture.targets`
- Tests:
  - `tests/Swa.Analyzers.Reliability.Tests`
  - `tests/Swa.Analyzers.Architecture.Tests`
  - `tests/Swa.Analyzers.Testing.Tests`
  - `tests/Swa.Analyzers.PackageValidation.Tests`
  - `tests/Swa.Analyzers.TestSupport`
- Samples:
  - `samples/Swa.Analyzers.Reliability.Sample`
  - `samples/Swa.Analyzers.Architecture.Sample`
  - `samples/Swa.Analyzers.Testing.Sample`
- Scripts:
  - `scripts/Validate-Release.ps1`
  - `scripts/Inspect-NuGetPackages.ps1`
  - `scripts/Validate-AnalyzerPackageIsolation.ps1`
- CI e hooks:
  - `.github/workflows/*.yml`
  - `.github/dependabot.yml` se labels, paths ou grupos forem renomeados
  - `.githooks/pre-push`
  - `.githooks/commit-msg` somente se mensagem/padrao mencionar identidade
- Documentacao:
  - `docs/packages/*`
  - `docs/rules/**`
  - `docs/migration-v2.md`
  - `docs/release.md`
  - `docs/contributing-rules.md`
  - `docs/adoption.md`
  - `docs/editorconfig-profiles.md`
  - `docs/specs/next-analyzers/**`
  - `docs/history/**` somente quando nao for registro historico intencional
- Instrucoes de agentes:
  - `AGENTS.md`
  - `.agents/skills/**/SKILL.md`
- IDE/config:
  - `.vs/**` e `.vscode/**` devem ser avaliados para remocao do Git em vez de rename mecanico.

## Contratos externos afetados

- IDs NuGet:
  - `Swa.Analyzers.Reliability` -> `CSF.Analyzers.Reliability`
  - `Swa.Analyzers.Architecture` -> `CSF.Analyzers.Architecture`
  - `Swa.Analyzers.Testing` -> `CSF.Analyzers.Testing`
- Assembly names e arquivos de analyzer dentro dos pacotes:
  - `Swa.Analyzers.*.dll` -> `CSF.Analyzers.*.dll`
  - `Swa.Analyzers.*.pdb` -> `CSF.Analyzers.*.pdb`
  - destino deve continuar `analyzers/dotnet/cs`.
- Namespaces consumidos por testes, samples e possiveis consumidores que referenciem tipos diretamente.
- `PackageReference Include=...` em documentacao, samples externos e guias de migracao.
- `RepositoryUrl` e `PackageProjectUrl` em nuspec.
- Nome da GitHub Release: hoje `Swa.Analyzers {tag}`.
- Tags GitVersion continuam `v{SemVer}` e nao precisam carregar o nome do produto.
- Links de help gerados por `RuleHelpLinks.ForRule(...)`, que hoje apontam para `docs/rules` no repositorio atual.
- GitHub repository slug se o repositorio for renomeado de fato.
- Badges, links, clone URLs e docs publicas.
- Artefatos de CI: nomes de pacotes, uploads, paths e inspecoes.

## Dependencias entre etapas

- Limpeza de arquivos rastreados indevidos deve acontecer antes da renomeacao ampla ou ser explicitamente excluida da migracao.
- A solucao deve ser renomeada antes ou junto dos caminhos de projetos para manter comandos locais e workflows coerentes.
- Projetos e diretorios `src` devem ser migrados antes dos testes e samples que referenciam os projetos.
- Namespaces/usings devem mudar junto com `RootNamespace`, `AssemblyName` e `PackageId` para evitar estado intermediario confuso.
- Scripts de validacao devem ser atualizados no mesmo prompt que altera pacotes/assemblies; caso contrario os checks de release falharao por desenho.
- Workflows devem mudar apos os scripts locais passarem.
- Documentacao publica deve ser atualizada depois que os nomes finais de pacote, path e comando estiverem decididos.
- README e docs de migracao devem registrar qualquer compatibilidade temporaria de pacote antigo antes de release.

## Ordem das etapas

1. Criar esta spec SDD e commitar somente documentacao.
2. Limpar ou decidir formalmente sobre `.vs/**` e `.vscode/**` rastreados.
3. Renomear solucao, diretorios e arquivos de projeto em `src`, `tests` e `samples`.
4. Atualizar csproj/MSBuild/NuGet: `PackageId`, `AssemblyName`, project references, `buildTransitive`, shared compile includes e lock files se necessario.
5. Atualizar namespaces, usings e strings de source/tests/samples.
6. Atualizar scripts de validacao e inspecao.
7. Atualizar workflows, hooks e comandos de CI.
8. Atualizar documentacao publica, docs de pacote, docs de release, specs e instrucoes de agentes.
9. Executar validacao completa local: restore locked, build, tests, release validation, pack e package inspection.
10. Criar guia de migracao externa de `Swa.Analyzers.*` para `CSF.Analyzers.*`, incluindo decisoes de SemVer e compatibilidade.

## Riscos

- Breaking change de NuGet package IDs para consumidores.
- Quebra de workflows por path antigo em `.slnx`, `.csproj`, scripts ou hooks.
- Package inspection falhar se assemblies/PDBs mudarem mas scripts esperarem `Swa.Analyzers.*`.
- Help links e repository URL apontarem para repositorio antigo.
- Renomeacao cega alterar textos historicos que deveriam permanecer como referencia de migracao.
- Renomeacao cega alterar `swap`/outros tokens nao relacionados contendo `swa`.
- Arquivos `.vs` binarios rastreados poluirem diffs e inventario.
- Lock files e caches de restore/build mudarem sem necessidade.
- `buildTransitive` carregar nome antigo e quebrar consumo do pacote Architecture.
- A troca de identidade exigir decisao de SemVer major.

## Criterios globais de aceite

- Todas as ocorrencias ativas de `Swa.Analyzers` em codigo, namespaces, projetos, pacotes, scripts, workflows, samples e docs publicas foram migradas ou registradas como excecao.
- IDs `REL###`, `ARC###` e `TST###` permanecem inalterados.
- `dotnet restore`, `dotnet build`, `dotnet test -m:1`, `Validate-Release.ps1`, `dotnet pack` e inspecoes de pacote passam.
- NuGets gerados contem somente assemblies/PDBs `CSF.Analyzers.*` em `analyzers/dotnet/cs`.
- Nenhum pacote legado `Swa.Analyzers` e gerado acidentalmente.
- README, docs de pacote, docs de release e guia de migracao externa refletem a nova identidade.
- Contratos externos antigos estao documentados como breaking change ou como compatibilidade temporaria deliberada.
- Nenhum arquivo de build/test/IDE ignorado e usado como fonte de verdade.

## Estrategia de validacao

Baseline executado nesta etapa em 2026-07-23:

| Comando | Resultado |
| ------- | --------- |
| `dotnet restore ./Swa.Analyzers.slnx` | Aprovado; todos os projetos atualizados para restauracao. |
| `dotnet build ./Swa.Analyzers.slnx --configuration Release --no-restore` | Aprovado; 0 avisos, 0 erros. |
| `dotnet test ./Swa.Analyzers.slnx --configuration Release -m:1` | Aprovado; 246 testes, 0 falhas. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-Release.ps1` | Aprovado; `release-check: validacoes aprovadas`. |
| `dotnet pack ./Swa.Analyzers.slnx --configuration Release --no-build --output ./artifacts/csf-rename-baseline /p:PackageVersion=0.0.0-csf-baseline` | Aprovado; 3 `.nupkg` e 3 `.snupkg` gerados. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Inspect-NuGetPackages.ps1 -PackageDirectory ./artifacts/csf-rename-baseline -Version '0.0.0-csf-baseline'` | Aprovado; package inspection aprovada. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-AnalyzerPackageIsolation.ps1 -PackageDirectory ./artifacts/csf-rename-baseline -Version '0.0.0-csf-baseline'` | Aprovado; 3 testes de isolamento e package inspection aprovados. |

Validacao esperada apos a renomeacao completa:

```powershell
dotnet restore ./CSF.Analyzers.slnx --locked-mode
dotnet build ./CSF.Analyzers.slnx --configuration Release --no-restore
dotnet test ./CSF.Analyzers.slnx --configuration Release -m:1
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-Release.ps1
dotnet pack ./CSF.Analyzers.slnx --configuration Release --no-build --output ./artifacts/packages /p:PackageVersion=<SemVer>
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Inspect-NuGetPackages.ps1 -PackageDirectory ./artifacts/packages -Version '<SemVer>'
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-AnalyzerPackageIsolation.ps1 -PackageDirectory ./artifacts/packages -Version '<SemVer>'
```

Tambem deve ser executada busca final:

```powershell
rg -n -i --hidden --glob '!/.git/**' "swa|Swa\.Analyzers|rodri-oliveira-dev/Swa\.Analyzers"
git ls-files | Select-String -Pattern "swa|Swa\.Analyzers|rodri-oliveira-dev/Swa\.Analyzers" -CaseSensitive:$false
```

Toda ocorrencia remanescente deve ser historica, migratoria ou excecao temporaria documentada.

## Estrategia de rollback

- Cada etapa futura deve ser pequena e revisavel em um commit proprio.
- Se uma etapa falhar antes do commit, descartar somente as alteracoes daquela etapa.
- Se uma etapa ja estiver commitada, reverter o commit especifico com `git revert`, preservando historico.
- Nao usar `git reset --hard` como mecanismo padrao.
- Manter a spec intacta durante rollback, salvo se a propria decisao de migracao for alterada.
- Para falhas em pacote/release, voltar primeiro scripts/workflows ao estado anterior e depois source/projetos, para recuperar validacoes locais rapidamente.

## Excecoes temporarias permitidas entre etapas

- `Swa.Analyzers` pode permanecer em docs historicas, changelog ou guia de migracao enquanto a etapa correspondente nao for atualizada.
- `Swa.Analyzers` pode permanecer em scripts de validacao ate a etapa que renomeia pacotes/assemblies.
- Paths antigos podem coexistir brevemente em docs/specs entre prompts, desde que o codigo buildavel esteja em um estado consistente no fim de cada etapa.
- Artefatos ignorados em `bin`, `obj`, `artifacts` e `TestResults` podem conter nomes antigos e nao precisam ser migrados.
- Arquivos `.vs/**` e `.vscode/**` rastreados devem ser resolvidos em etapa propria; ate la, nao devem ser usados como fonte de contrato.
- Referencias ao pacote legado `Swa.Analyzers` em `docs/migration-v2.md` podem permanecer se estiverem claramente documentando historico ou migracao.

## Decisoes desta etapa

- Nenhuma renomeacao de produto foi implementada.
- A migracao deve ser tratada como breaking change de identidade/empacotamento, salvo decisao futura explicita de compatibilidade.
- Os IDs `REL###`, `ARC###` e `TST###` permanecem fora da renomeacao.
- O inventario deve excluir `.git` e distinguir arquivos rastreados de artefatos ignorados.
- Antes da renomeacao ampla, o repositorio deve decidir o destino dos arquivos `.vs` e `.vscode` rastreados.
