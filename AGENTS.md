# AGENTS.md

## Objetivo

Este repositorio contem analyzers Roslyn reutilizaveis para .NET, focados em convencoes de arquitetura, confiabilidade, performance e qualidade de testes.

O objetivo do agente e fazer mudancas pequenas, corretas, reprodutiveis e coerentes com a estrutura ja adotada.

## Fontes principais de verdade

Antes de alterar qualquer coisa, consulte nesta ordem quando relevante:

1. `README.md`
2. `docs/rules/`
3. `Directory.Packages.props`
4. `.editorconfig`
5. `Swa.Analyzers.slnx`
6. `src/Swa.Analyzers.Core/AnalyzerReleases.Unshipped.md`
7. `src/Swa.Analyzers.SampleApp/README.md`

## Escopo do repositorio

A solucao principal do repositorio e:

- `Swa.Analyzers.slnx`

Os principais componentes estao organizados em:

- `src/Swa.Analyzers.Core`: implementacao dos analyzers e descritores de diagnostico.
- `src/Swa.Analyzers.SampleApp`: exemplos manuais validos e invalidos para cada regra.
- `tests/Swa.Analyzers.Tests`: testes automatizados dos analyzers.
- `docs/rules`: documentacao de cada regra `ARCH###`.

## Regras obrigatorias

- Faca a menor mudanca possivel para resolver o problema.
- Preserve a separacao entre analyzer, testes, sample app e documentacao.
- Nao adicione `Version=` em `PackageReference`. O repositorio usa Central Package Management.
- Nao introduza dependencias novas sem necessidade clara.
- Nao introduza segredos no repositorio.
- Nao use paths, projetos, solucoes ou comandos que nao existam no repo.
- Ao criar ou alterar regra de analyzer, atualize os testes, a documentacao em `docs/rules/` e `AnalyzerReleases.Unshipped.md` quando aplicavel.

## Convencoes de implementacao

### Dependencias

- Use versoes centralizadas em `Directory.Packages.props`.
- Prefira reutilizar dependencias ja existentes.
- Evite adicionar novos pacotes sem necessidade clara.

### Estilo e qualidade

- Respeite `.editorconfig`.
- Respeite `Nullable` e `ImplicitUsings` habilitados nos projetos.
- Mantenha nomenclatura consistente com os analyzers existentes.
- Evite refactors amplos nao solicitados.
- Evite renomeacoes desnecessarias.
- Evite alterar formatacao de arquivos sem necessidade funcional.

### Analyzers

- Novas regras devem usar IDs `ARCH###` coerentes com `RuleIdentifiers`.
- Mensagens, titulos, categorias, severidades e help links devem seguir o padrao existente.
- Cada regra deve ter exemplos validos e invalidos no sample app quando isso ajudar a validacao manual.
- Testes devem cobrir diagnosticos esperados e casos negativos relevantes.

## Fluxo padrao antes de editar

1. Identifique a regra, projeto ou configuracao afetada.
2. Verifique se ha impacto em:
   - API do analyzer
   - testes
   - sample app
   - documentacao de regras
   - empacotamento/metadados de release
   - CI e configuracao local
3. Localize testes existentes relacionados a mudanca.
4. Faca a menor alteracao possivel.

## Commits

- Quando o usuario solicitar que os ajustes sejam commitados, criar commits usando Conventional Commits.
- Usar o formato:
  - `feat:` para novas regras ou funcionalidades
  - `fix:` para correcoes
  - `refactor:` para refatoracoes sem alteracao funcional
  - `test:` para criacao ou ajuste de testes
  - `docs:` para documentacao
  - `chore:` para ajustes operacionais, tooling ou configuracao
- A mensagem deve ser objetiva, em portugues ou ingles conforme o padrao ja usado no historico do repositorio.
- Antes de commitar, revisar o diff e executar os checks relevantes.
- Nao criar commit se houver falha de build/teste sem registrar claramente o motivo.

## ADRs

- Este repositorio documenta regras em `docs/rules/`; nao ha pasta de ADRs atualmente.
- Nao crie ADR para ajustes mecanicos, correcao de testes, documentacao simples ou configuracao local.
- Se uma mudanca futura introduzir uma decisao arquitetural relevante para o projeto de analyzers, confirme com o usuario antes de criar uma estrutura nova de ADRs.

## Comandos padrao

Use estes comandos como baseline:

```bash
dotnet restore ./Swa.Analyzers.slnx
dotnet build ./Swa.Analyzers.slnx --configuration Release --no-restore -m:1
dotnet test ./Swa.Analyzers.slnx --configuration Release --no-build -m:1
```
