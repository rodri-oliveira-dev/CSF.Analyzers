# AGENTS.md

## Objetivo

Este repositorio contem analyzers Roslyn reutilizaveis para .NET, focados em convencoes de arquitetura, confiabilidade, performance e qualidade de testes.

O objetivo do agente e fazer mudancas pequenas, corretas, reprodutiveis e coerentes com a estrutura ja adotada.

## Fontes principais de verdade

Antes de alterar qualquer coisa, consulte nesta ordem quando relevante:

1. `AGENTS.md`
2. `.agents/skills/`
3. `README.md`
4. `docs/rules/`
5. `Directory.Packages.props`
6. `.editorconfig`
7. `global.json`
8. `Swa.Analyzers.slnx`
9. `src/Swa.Analyzers.Core/AnalyzerReleases.Unshipped.md`
10. `src/Swa.Analyzers.SampleApp/README.md`

## Escopo do repositorio

A solucao principal do repositorio e:

- `Swa.Analyzers.slnx`

Os principais componentes estao organizados em:

- `src/Swa.Analyzers.Core`: implementacao dos analyzers, diagnostic descriptors, identificadores e metadados de release.
- `tests/Swa.Analyzers.Tests`: testes automatizados dos analyzers.
- `src/Swa.Analyzers.SampleApp`: exemplos manuais validos e invalidos para cada regra.
- `docs/rules`: documentacao de cada regra `ARCH###`.
- `.agents/skills`: instrucoes especializadas para o Codex trabalhar neste repositorio.

## Skills recomendadas

Use a skill mais especifica para a tarefa:

- `roslyn-analyzer-rule-change`: criar ou alterar uma regra `ARCH###`.
- `roslyn-analyzer-test-change`: criar ou ajustar testes de analyzer.
- `roslyn-analyzer-doc-rule-change`: criar ou atualizar documentacao de regra.
- `roslyn-analyzer-sample-app-change`: criar ou ajustar exemplos manuais no SampleApp.
- `roslyn-analyzer-packaging-release-change`: alterar empacotamento, release metadata, NuGet, CI ou versao.

Nao use skills genericas de servico .NET para este repositorio. Este projeto nao e uma API de negocio, nao usa Clean Architecture como aplicacao, nao usa EF Core, Kafka, Outbox, controllers, migrations ou ADRs como fluxo padrao.

## Regras obrigatorias

- Faca a menor mudanca possivel para resolver o problema.
- Preserve a separacao entre analyzer, testes, SampleApp, documentacao e empacotamento.
- Nao adicione `Version=` em `PackageReference`. O repositorio usa Central Package Management.
- Nao introduza dependencias novas sem necessidade clara.
- Nao introduza segredos no repositorio.
- Nao use paths, projetos, solucoes ou comandos que nao existam no repo.
- Nao altere formato, nomenclatura ou organizacao fora do escopo solicitado.
- Ao criar ou alterar regra de analyzer, atualize testes, documentacao, SampleApp e `AnalyzerReleases.Unshipped.md` quando aplicavel.
- Ao alterar comportamento publico de regra, atualize o README quando a lista de regras, configuracoes ou exemplos publicos mudarem.

## Convencoes de implementacao

### Dependencias

- Use versoes centralizadas em `Directory.Packages.props`.
- Prefira reutilizar dependencias ja existentes.
- Evite adicionar novos pacotes sem necessidade clara.
- Preserve `PrivateAssets` quando a dependencia nao deve vazar para consumidores do pacote.
- Preserve `RestorePackagesWithLockFile` e o uso de lock file.

### Estilo e qualidade

- Respeite `.editorconfig`.
- Respeite `Nullable` e `ImplicitUsings` habilitados nos projetos.
- Mantenha nomenclatura consistente com os analyzers existentes.
- Evite refactors amplos nao solicitados.
- Evite renomeacoes desnecessarias.
- Evite alterar formatacao de arquivos sem necessidade funcional.
- Preserve compatibilidade do projeto `Swa.Analyzers.Core` com `netstandard2.0`.

### Analyzers

- Novas regras devem usar IDs `ARCH###` coerentes com `RuleIdentifiers`.
- Mensagens, titulos, categorias, severidades e help links devem seguir o padrao existente.
- Cada regra deve declarar `DiagnosticDescriptor` com `RuleHelpLinks.ForRule(...)`.
- Use `EnableConcurrentExecution()`.
- Configure a analise de codigo gerado de forma explicita.
- Use `CancellationToken` em chamadas semanticas.
- Prefira analise sintatica quando suficiente.
- Use analise semantica quando ela reduzir falso positivo ou confirmar simbolos de frameworks.
- Evite heuristicas amplas que causem muito ruido.
- Evite diagnosticos em codigo gerado, stubs irrelevantes ou simbolos ambiguos.
- Quando a regra aceitar configuracao por `.editorconfig`, teste valor ausente, valor valido, valor invalido e escopo por arquivo quando relevante.

### Testes

- Use `tests/Swa.Analyzers.Tests/Verifier.cs` como padrao.
- Testes devem cobrir diagnosticos esperados e casos negativos relevantes.
- Para regras com dependencias externas, use stubs minimos em string ou no SampleApp quando isso evitar dependencia desnecessaria.
- Cubra falsos positivos antes de ampliar heuristicas.
- Nomeie testes pelo comportamento observado.
- Mantenha testes focados na regra alterada.

### SampleApp

- Use o SampleApp para exemplos manuais e demonstracao.
- Exemplos devem ficar em pasta `Arch###/`.
- Use `*_Invalid.cs` para codigo intencionalmente nao conforme.
- Use `*_Valid.cs` para codigo conforme.
- Ajuste `src/Swa.Analyzers.SampleApp/.editorconfig` para que exemplos invalidos nao quebrem a compilacao sem necessidade.
- Use stubs apenas para habilitar reconhecimento simbolico necessario ao analyzer.

### Documentacao

- Cada regra deve ter documentacao propria em `docs/rules/ARCH###.md`.
- A documentacao deve explicar objetivo, codigo nao conforme, codigo conforme, configuracao quando houver, heuristica, limitacoes conhecidas e impacto esperado.
- Nao documente comportamento que nao foi implementado.
- Ao alterar configuracao publica de regra, atualize o README.

## Fluxo padrao antes de editar

1. Identifique a regra, projeto ou configuracao afetada.
2. Verifique se ha impacto em:
   - API do analyzer
   - diagnostic descriptor
   - `RuleIdentifiers`
   - testes
   - SampleApp
   - documentacao de regras
   - `AnalyzerReleases.Unshipped.md`
   - empacotamento
   - CI e configuracao local
3. Localize testes existentes relacionados a mudanca.
4. Faca a menor alteracao possivel.

## ADRs

- Este repositorio documenta regras em `docs/rules/`; nao ha pasta de ADRs atualmente.
- Nao crie ADR para ajustes mecanicos, correcao de testes, documentacao simples, configuracao local ou criacao normal de regra.
- Se uma mudanca futura introduzir uma decisao arquitetural relevante para o proprio projeto de analyzers, confirme com o usuario antes de criar uma estrutura nova de ADRs.

## Commits

- Quando o usuario solicitar que os ajustes sejam commitados, crie commits usando Conventional Commits.
- Use o formato:
  - `feat:` para novas regras ou funcionalidades
  - `fix:` para correcoes
  - `refactor:` para refatoracoes sem alteracao funcional
  - `test:` para criacao ou ajuste de testes
  - `docs:` para documentacao
  - `chore:` para ajustes operacionais, tooling, CI ou configuracao
- A mensagem deve ser objetiva, em portugues ou ingles conforme o padrao ja usado no historico do repositorio.
- Antes de commitar, revise o diff e execute os checks relevantes.
- Nao crie commit se houver falha de build ou teste sem registrar claramente o motivo.

## Comandos padrao

Use estes comandos como baseline local:

```bash
dotnet restore ./Swa.Analyzers.slnx
dotnet build ./Swa.Analyzers.slnx --configuration Release --no-restore
dotnet test ./Swa.Analyzers.slnx --configuration Release -m:1
```

Para validacao rapida apos build:

```bash
dotnet test ./Swa.Analyzers.slnx --configuration Release --no-build -m:1
```

Para aproximar do CI quando aplicavel:

```bash
dotnet restore ./Swa.Analyzers.slnx --locked-mode
dotnet build ./Swa.Analyzers.slnx --configuration Release --no-restore
dotnet test ./Swa.Analyzers.slnx --configuration Release -m:1
```

O `-m:1` deve ser mantido nos testes enquanto `dotnet test` contra a `.slnx` falhar antes da descoberta quando o MSBuild usa multiplos nos.

## Finalizacao

Antes de concluir uma tarefa:

1. Revise o diff.
2. Confirme se analyzer, testes, docs, SampleApp e release metadata estao coerentes.
3. Execute restore, build e testes proporcionais ao impacto.
4. Informe quais validacoes foram executadas.
5. Se algum comando nao foi executado, registre claramente o motivo.
