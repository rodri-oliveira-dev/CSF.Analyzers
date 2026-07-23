# AGENTS.md

## Objetivo

Este repositório contém analyzers Roslyn reutilizáveis para .NET, focados em convenções de arquitetura, confiabilidade, performance e qualidade de testes.

O objetivo do agente e fazer mudanças pequenas, corretas, reprodutíveis e coerentes com a estrutura já adotada.

## Fontes principais de verdade

Antes de alterar qualquer coisa, consulte nesta ordem quando relevante:

1. `AGENTS.md`
2. `.agents/skills/`
3. `README.md`
4. `docs/rules/`
5. `Directory.Packages.props`
6. `.editorconfig`
7. `global.json`
8. `CSF.Analyzers.slnx`
9. `src/CSF.Analyzers.{Reliability,Architecture,Testing}/AnalyzerReleases.Unshipped.md`
10. `samples/Swa.Analyzers.*.Sample/`

## Escopo do repositório

A solução principal do repositório e:

- `CSF.Analyzers.slnx`

Os principais componentes estão organizados em:

- `src/CSF.Analyzers.Reliability`: implementação, identificadores e metadados de release das regras `REL###`.
- `src/CSF.Analyzers.Architecture`: implementação, identificadores e metadados de release das regras `ARC###`.
- `src/CSF.Analyzers.Testing`: implementação, identificadores e metadados de release das regras `TST###`.
- `src/CSF.Analyzers.Common`: código-fonte compartilhado incluído nos pacotes de analyzer.
- `tests/Swa.Analyzers.Reliability.Tests`, `tests/Swa.Analyzers.Architecture.Tests` e `tests/Swa.Analyzers.Testing.Tests`: testes automatizados por pacote.
- `samples/Swa.Analyzers.Reliability.Sample`, `samples/Swa.Analyzers.Architecture.Sample` e `samples/Swa.Analyzers.Testing.Sample`: exemplos manuais válidos e inválidos por pacote.
- `docs/rules`: documentação de cada regra `REL###`, `ARC###` ou `TST###`.
- `.agents/skills`: instrucoes especializadas para o Codex trabalhar neste repositório.

## Skills recomendadas

Use a skill mais específica para a tarefa:

- `roslyn-analyzer-rule-change`: criar ou alterar uma regra `REL###`, `ARC###` ou `TST###`.
- `roslyn-analyzer-test-change`: criar ou ajustar testes de analyzer.
- `roslyn-analyzer-doc-rule-change`: criar ou atualizar documentação de regra.
- `roslyn-analyzer-sample-app-change`: criar ou ajustar exemplos manuais no SampleApp.
- `roslyn-analyzer-packaging-release-change`: alterar empacotamento, release metadata, NuGet, CI ou versão.

Não use skills genéricas de serviço .NET para este repositório. Este projeto não é uma API de negócio, não usa Clean Architecture como aplicação, não usa EF Core, Kafka, Outbox, controllers, migrations ou ADRs como fluxo padrão.

## Regras obrigatorias

- Faça a menor mudança possível para resolver o problema.
- Preserve a separação entre analyzer, testes, SampleApp, documentação e empacotamento.
- Não adicione `Version=` em `PackageReference`. O repositório usa Central Package Management.
- Não introduza dependências novas sem necessidade clara.
- Não introduza segredos no repositório.
- Não use paths, projetos, soluções ou comandos que não existam no repo.
- Não altere formato, nomenclatura ou organização fora do escopo solicitado.
- Ao criar ou alterar regra de analyzer, atualize testes, documentação, SampleApp e `AnalyzerReleases.Unshipped.md` quando aplicável.
- Ao alterar comportamento público de regra, atualize o README quando a lista de regras, configurações ou exemplos públicos mudarem.

## Convencoes de implementação

### Dependencias

- Use versões centralizadas em `Directory.Packages.props`.
- Prefira reutilizar dependências já existentes.
- Evite adicionar novos pacotes sem necessidade clara.
- Preserve `PrivateAssets` quando a dependência não deve vazar para consumidores do pacote.
- Preserve `RestorePackagesWithLockFile` e o uso de lock file.

### Estilo e qualidade

- Respeite `.editorconfig`.
- Respeite `Nullable` e `ImplicitUsings` habilitados nos projetos.
- Mantenha nomenclatura consistente com os analyzers existentes.
- Evite refactors amplos não solicitados.
- Evite renomeacoes desnecessárias.
- Evite alterar formatação de arquivos sem necessidade funcional.
- Preserve compatibilidade dos projetos de pacote com `netstandard2.0`.

### Analyzers

- Novas regras devem usar IDs `REL###`, `ARC###` ou `TST###` coerentes com `RuleIdentifiers`.
- Mensagens, títulos, categorias, severidades e help links devem seguir o padrão existente.
- Cada regra deve declarar `DiagnosticDescriptor` com `RuleHelpLinks.ForRule(...)`.
- Use `EnableConcurrentExecution()`.
- Configure a análise de código gerado de forma explícita.
- Use `CancellationToken` em chamadas semânticas.
- Prefira análise sintática quando suficiente.
- Use análise semântica quando ela reduzir falso positivo ou confirmar símbolos de frameworks.
- Evite heurísticas amplas que causem muito ruído.
- Evite diagnósticos em código gerado, stubs irrelevantes ou símbolos ambiguos.
- Quando a regra aceitar configuração por `.editorconfig`, teste valor ausente, valor válido, valor inválido e escopo por arquivo quando relevante.

### Testes

- Use `tests/Swa.Analyzers.TestSupport/Verifier.cs` como padrão.
- Testes devem cobrir diagnósticos esperados e casos negativos relevantes.
- Para regras com dependências externas, use stubs mínimos em string ou no SampleApp quando isso evitar dependência desnecessária.
- Cubra falsos positivos antes de ampliar heurísticas.
- Nomeie testes pelo comportamento observado.
- Mantenha testes focados na regra alterada.

### SampleApp

- Use o SampleApp para exemplos manuais e demonstração.
- Exemplos devem ficar em pastas `Rel###/`, `Arc###/` ou `Tst###/` no sample do pacote correspondente.
- Use `*_Invalid.cs` para código intencionalmente não conforme.
- Use `*_Valid.cs` para código conforme.
- Ajuste o `.editorconfig` do sample correspondente quando necessário para que exemplos inválidos não quebrem a compilação sem necessidade.
- Use stubs apenas para habilitar reconhecimento simbólico necessário ao analyzer.

### Documentacao

- Cada regra deve ter documentação própria em `docs/rules/<ID>.md`.
- A documentação deve explicar objetivo, código não conforme, código conforme, configuração quando houver, heurística, limitações conhecidas e impacto esperado.
- Não documente comportamento que não foi implementado.
- Ao alterar configuração pública de regra, atualize o README.

## Fluxo padrão antes de editar

1. Identifique a regra, projeto ou configuração afetada.
2. Verifique se há impacto em:
   - API do analyzer
   - diagnostic descriptor
   - `RuleIdentifiers`
   - testes
   - samples
   - documentação de regras
   - `AnalyzerReleases.Unshipped.md`
   - empacotamento
   - CI e configuração local
3. Localize testes existentes relacionados a mudança.
4. Faça a menor alteração possível.

## ADRs

- Este repositório documenta regras em `docs/rules/`; não há pasta de ADRs atualmente.
- Não crie ADR para ajustes mecânicos, correção de testes, documentação simples, configuração local ou criação normal de regra.
- Se uma mudança futura introduzir uma decisão arquitetural relevante para o proprio projeto de analyzers, confirme com o usuário antes de criar uma estrutura nova de ADRs.

## Commits

- Quando o usuário solicitar que os ajustes sejam commitados, crie commits usando Conventional Commits.
- Use o formato:
  - `feat:` para novas regras ou funcionalidades
  - `fix:` para correções
  - `refactor:` para refatoracoes sem alteração funcional
  - `test:` para criação ou ajuste de testes
  - `docs:` para documentação
  - `chore:` para ajustes operacionais, tooling, CI ou configuração
- A mensagem deve ser objetiva, em portugues ou ingles conforme o padrão já usado no histórico do repositório.
- Antes de commitar, revise o diff e execute os checks relevantes.
- Não crie commit se houver falha de build ou teste sem registrar claramente o motivo.

## Comandos padrão

Use estes comandos como baseline local:

```bash
dotnet restore ./CSF.Analyzers.slnx
dotnet build ./CSF.Analyzers.slnx --configuration Release --no-restore
dotnet test ./CSF.Analyzers.slnx --configuration Release -m:1
```

Para validação rápida após build:

```bash
dotnet test ./CSF.Analyzers.slnx --configuration Release --no-build -m:1
```

Para aproximar do CI quando aplicável:

```bash
dotnet restore ./CSF.Analyzers.slnx --locked-mode
dotnet build ./CSF.Analyzers.slnx --configuration Release --no-restore
dotnet test ./CSF.Analyzers.slnx --configuration Release -m:1
```

O `-m:1` deve ser mantido nos testes enquanto `dotnet test` contra a `.slnx` falhar antes da descoberta quando o MSBuild usa múltiplos nos.

## Finalizacao

Antes de concluir uma tarefa:

1. Revise o diff.
2. Confirme se analyzer, testes, docs, samples e release metadata estão coerentes.
3. Execute restore, build e testes proporcionais ao impacto.
4. Informe quais validações foram executadas.
5. Se algum comando não foi executado, registre claramente o motivo.
