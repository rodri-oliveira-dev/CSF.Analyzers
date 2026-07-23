# CSF rename - documentation

## Estado inicial

- Worktree limpo antes da implementacao.
- A etapa anterior concluiu a migracao de PackageIds, scripts, workflows, hooks e validacoes de release para `CSF.Analyzers.*`.
- `README.md`, `CHANGELOG.md`, `docs/**`, `AGENTS.md` e `.agents/skills/**` ainda continham referencias textuais ao nome antigo em documentacao publica, historica, specs e comandos.
- O repositorio remoto ainda nao tinha sido renomeado nesta etapa. URLs cujo hostname/path dependiam do slug `rodri-oliveira-dev/Swa.Analyzers` permaneceram como excecao temporaria ate a etapa 06.

## Inventario antes das alteracoes

Inventario executado em 2026-07-23 antes das edicoes desta etapa, usando somente arquivos documentais rastreados por Git:

```powershell
git ls-files README.md CHANGELOG.md AGENTS.md docs .agents/skills
rg -n -i --hidden "swa|Swa\.Analyzers|rodri-oliveira-dev/Swa\.Analyzers" README.md CHANGELOG.md AGENTS.md docs .agents/skills
```

Escopo rastreado: 49 arquivos documentais.

| Padrao | Ocorrencias antes |
| ------ | ----------------- |
| `Swa.Analyzers` | 316 |
| palavra `Swa` | 339 |
| palavra `SWA` | 3 |
| palavra `swa` | 11 |
| `rodri-oliveira-dev/Swa.Analyzers` | 7 |
| `https://github.com/rodri-oliveira-dev/Swa.Analyzers` | 4 |

Principais grupos encontrados:

- README, CHANGELOG, docs de adocao, release, pacotes e regras.
- Migration guide v2 e perfis de `.editorconfig`.
- Reviews e history.
- Specs futuras em `docs/specs/next-analyzers`.
- Specs da propria migracao em `docs/specs/csf-rename`.
- Instrucoes de agentes em `AGENTS.md` e `.agents/skills/**`.

## Alteracoes realizadas

- Atualizada a identidade textual ativa para `CSF.Analyzers`, `CSF.Analyzers.Reliability`, `CSF.Analyzers.Architecture` e `CSF.Analyzers.Testing`.
- Atualizados exemplos de instalacao para:

```powershell
dotnet add package CSF.Analyzers.Reliability
dotnet add package CSF.Analyzers.Architecture
dotnet add package CSF.Analyzers.Testing
```

- Atualizados comandos documentais para `./CSF.Analyzers.slnx`.
- Atualizadas tabelas de pacotes nas docs de regras, pacotes, migration guide, reviews e specs de proximos analyzers.
- Atualizados exemplos textuais, paths de samples, paths de source/tests e snippets que representavam a identidade atual do produto.
- Atualizado o comando de busca em `docs/history/refactoring-v2-plan.md` para procurar a identidade `CSF`.
- Preservadas specs anteriores de `docs/specs/csf-rename` como registro migratorio de origem/destino, evitando transformar evidencias historicas em tabelas `CSF -> CSF`.

## Excecoes temporarias de URL resolvidas na etapa 06

As ocorrencias abaixo permaneceram nesta etapa porque o slug remoto ainda dependia do nome anterior do repositorio GitHub. A etapa 06 renomeou o repositorio remoto e removeu essa excecao temporaria para URLs ativas.

| Arquivo | Ocorrencia |
| ------- | ---------- |
| `docs/specs/csf-rename/plan.md` | `rodri-oliveira-dev/Swa.Analyzers` |
| `docs/specs/csf-rename/plan.md` | `https://github.com/rodri-oliveira-dev/Swa.Analyzers` |
| `docs/specs/csf-rename/02-source-projects.md` | `rodri-oliveira-dev/Swa.Analyzers` |
| `docs/specs/csf-rename/04-packaging-release.md` | `https://github.com/rodri-oliveira-dev/Swa.Analyzers` |

## Ocorrencias remanescentes

- `docs/specs/csf-rename/plan.md`, `02-source-projects.md`, `03-tests-samples.md` e `04-packaging-release.md` preservam `Swa.Analyzers` em estado inicial, tabelas origem/destino, criterios de aceite historicos e comandos de busca das etapas ja executadas.
- `docs/rules/architecture/ARC001.md` contem `swagger`; esta ocorrencia e falso positivo da busca por `swa` e nao representa identidade antiga.
- Artefatos ignorados em `bin`, `obj`, `artifacts` e `TestResults` nao fazem parte da fonte de verdade desta etapa.

## Validacao

Validacoes executadas em 2026-07-23:

```powershell
dotnet build ./CSF.Analyzers.slnx --configuration Release
dotnet test ./CSF.Analyzers.slnx --configuration Release -m:1
rg -n -i --hidden "swa|Swa\.Analyzers|rodri-oliveira-dev/Swa\.Analyzers" README.md CHANGELOG.md AGENTS.md docs .agents/skills
rg -n "dotnet add package (Swa|CSF)\.Analyzers|dotnet restore ./.*\.slnx|dotnet build ./.*\.slnx|dotnet test ./.*\.slnx" README.md docs
```

| Comando | Resultado |
| ------- | --------- |
| `dotnet build ./CSF.Analyzers.slnx --configuration Release` | Aprovado; warnings esperados dos samples invalidos e `EnableGenerateDocumentationFile`; 0 erros. |
| `dotnet test ./CSF.Analyzers.slnx --configuration Release -m:1` | Aprovado; 246 testes, 0 falhas. |
| `rg -n -i --hidden "swa|Swa\.Analyzers|rodri-oliveira-dev/Swa\.Analyzers" README.md CHANGELOG.md AGENTS.md docs .agents/skills` | Aprovado; remanescentes restritos a specs migratorias, inventario desta spec, URLs temporarias e falso positivo `swagger`. |
| `rg -n "dotnet add package (Swa|CSF)\.Analyzers|dotnet restore ./.*\.slnx|dotnet build ./.*\.slnx|dotnet test ./.*\.slnx" README.md docs` | Aprovado; comandos ativos usam `CSF.Analyzers.*` e `CSF.Analyzers.slnx`; ocorrencias antigas aparecem apenas em specs migratorias. |
| Link-check relativo em `README.md` e `docs/**/*.md` | Aprovado; nenhum link relativo quebrado encontrado. |

## Decisoes

- Documentacao publica e operacional usa `CSF`.
- Referencias ao nome antigo em specs da propria migracao permanecem quando descrevem estado anterior, origem de rename ou contrato legado.
- URLs do GitHub com slug antigo permanecem ate o rename remoto ser realizado.
- Nao houve alteracao de comportamento de analyzer, package metadata, scripts ou workflows nesta etapa.
