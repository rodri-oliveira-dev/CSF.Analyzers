---
name: roslyn-analyzer-rule-change
description: Use esta skill ao criar ou alterar regras Roslyn ARCH### neste repositorio, incluindo analyzer, testes, documentacao, SampleApp, README e metadados de release.
---

# Objetivo

Executar mudancas em regras Roslyn do projeto `Swa.Analyzers` com seguranca, baixo ruido e consistencia com os padroes existentes.

# Quando usar

Use esta skill quando a tarefa envolver:

- criacao de nova regra `ARCH###`
- alteracao de analyzer existente
- ajuste de `DiagnosticDescriptor`
- alteracao de heuristica de diagnostico
- suporte a opcoes via `.editorconfig`
- ajuste em `RuleIdentifiers`
- testes de analyzer
- documentacao em `docs/rules`
- exemplos no SampleApp
- atualizacao de `AnalyzerReleases.Unshipped.md`

# Antes de alterar

1. Identifique a regra afetada.
2. Leia os arquivos relacionados:
   - `AGENTS.md`
   - `README.md`
   - `.editorconfig`
   - `Directory.Packages.props`
   - `global.json`
   - `src/Swa.Analyzers.Core/RuleIdentifiers.cs`
   - `src/Swa.Analyzers.Core/Rules/`
   - `tests/Swa.Analyzers.Tests/Rules/`
   - `tests/Swa.Analyzers.Tests/Verifier.cs`
   - `docs/rules/`
   - `src/Swa.Analyzers.SampleApp/README.md`
   - `src/Swa.Analyzers.SampleApp/.editorconfig`
   - `src/Swa.Analyzers.Core/AnalyzerReleases.Unshipped.md`

# Regras de implementacao

- Faca a menor mudanca possivel.
- Preserve a separacao entre analyzer, testes, documentacao, SampleApp e release metadata.
- Nao adicione `Version=` em `PackageReference`.
- Nao adicione dependencias novas sem necessidade clara.
- Preserve compatibilidade do projeto Core com `netstandard2.0`.
- Use IDs `ARCH###` definidos em `RuleIdentifiers`.
- Mantenha titulo, mensagem, categoria, severidade e help link consistentes com regras existentes.
- Use `DiagnosticDescriptor` com `RuleHelpLinks.ForRule(...)`.
- Use `EnableConcurrentExecution()`.
- Configure codigo gerado explicitamente com `ConfigureGeneratedCodeAnalysis(...)`.
- Use `CancellationToken` em chamadas do `SemanticModel`.
- Prefira analise sintatica quando suficiente.
- Use analise semantica quando ela reduzir falso positivo ou confirmar simbolos externos.
- Evite heuristicas amplas que aumentem falsos positivos.
- Evite analisar strings dinamicas se a regra foi definida para literais.
- Nao altere formatacao ou estrutura fora do escopo da regra.

# Checklist para nova regra

Ao criar uma regra nova:

1. Escolher proximo ID `ARCH###`.
2. Adicionar constante em `src/Swa.Analyzers.Core/RuleIdentifiers.cs`.
3. Criar analyzer em `src/Swa.Analyzers.Core/Rules/`.
4. Definir `DiagnosticDescriptor`.
5. Adicionar testes em `tests/Swa.Analyzers.Tests/Rules/`.
6. Adicionar documentacao em `docs/rules/ARCH###.md`.
7. Adicionar exemplos validos e invalidos no SampleApp quando ajudar na validacao manual.
8. Atualizar `src/Swa.Analyzers.Core/AnalyzerReleases.Unshipped.md`.
9. Atualizar tabela de regras no `README.md`.
10. Validar restore, build e testes.

# Checklist para alterar regra existente

Ao alterar uma regra existente:

1. Localizar analyzer, testes, documentacao e exemplos da regra.
2. Entender a heuristica atual antes de mudar.
3. Adicionar teste que reproduz o novo comportamento ou bug.
4. Fazer alteracao minima.
5. Ajustar docs e SampleApp se o comportamento publico mudar.
6. Revisar falso positivo e falso negativo.
7. Executar testes focados e, quando possivel, suite completa.

# Testes obrigatorios

Ao criar ou alterar uma regra, adicionar ou revisar testes cobrindo:

- codigo invalido com diagnostico esperado;
- codigo valido sem diagnostico;
- casos de falso positivo;
- bordas relevantes da heuristica;
- opcoes via `.editorconfig`, quando existirem;
- simbolos ou stubs necessarios sem depender de pacotes externos desnecessarios;
- comportamento com valor ausente ou invalido de configuracao, quando existir configuracao.

Use `tests/Swa.Analyzers.Tests/Verifier.cs` como padrao.

# Documentacao obrigatoria

Ao criar ou alterar comportamento de uma regra, atualizar:

- `docs/rules/ARCH###.md`
- `README.md`, se a lista ou configuracao publica mudar
- `src/Swa.Analyzers.Core/AnalyzerReleases.Unshipped.md`, quando aplicavel
- `src/Swa.Analyzers.SampleApp`, quando ajudar na validacao manual

# Validacao

Comandos base:

```bash
dotnet restore ./Swa.Analyzers.slnx
dotnet build ./Swa.Analyzers.slnx --configuration Release --no-restore
dotnet test ./Swa.Analyzers.slnx --configuration Release -m:1
```

Para validacao rapida apos build:

```bash
dotnet test ./Swa.Analyzers.slnx --configuration Release --no-build -m:1
```

# Finalizacao

Antes de concluir:

1. Revise o diff.
2. Confirme que analyzer, testes, docs, SampleApp e release metadata estao coerentes.
3. Informe quais validacoes foram executadas.
4. Se nao foi possivel executar algum comando, registre claramente o motivo.
