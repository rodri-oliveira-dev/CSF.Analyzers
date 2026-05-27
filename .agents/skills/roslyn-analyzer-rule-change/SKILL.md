---
name: roslyn-analyzer-rule-change
description: Use esta skill ao criar ou alterar regras Roslyn ARCH### neste repositório, incluindo analyzer, testes, documentação, SampleApp, README e metadados de release.
---

# Objetivo

Executar mudanças em regras Roslyn do projeto `Swa.Analyzers` com segurança, baixo ruído e consistência com os padrões existentes.

# Quando usar

Use esta skill quando a tarefa envolver:

- criação de nova regra `ARCH###`
- alteração de analyzer existente
- ajuste de `DiagnosticDescriptor`
- alteração de heurística de diagnóstico
- suporte a opções via `.editorconfig`
- ajuste em `RuleIdentifiers`
- testes de analyzer
- documentação em `docs/rules`
- exemplos no SampleApp
- atualização de `AnalyzerReleases.Unshipped.md`

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

# Regras de implementação

- Faça a menor mudança possível.
- Preserve a separação entre analyzer, testes, documentação, SampleApp e release metadata.
- Não adicione `Version=` em `PackageReference`.
- Não adicione dependências novas sem necessidade clara.
- Preserve compatibilidade do projeto Core com `netstandard2.0`.
- Use IDs `ARCH###` definidos em `RuleIdentifiers`.
- Mantenha titulo, mensagem, categoria, severidade e help link consistentes com regras existentes.
- Use `DiagnosticDescriptor` com `RuleHelpLinks.ForRule(...)`.
- Use `EnableConcurrentExecution()`.
- Configure código gerado explicitamente com `ConfigureGeneratedCodeAnalysis(...)`.
- Use `CancellationToken` em chamadas do `SemanticModel`.
- Prefira análise sintática quando suficiente.
- Use análise semântica quando ela reduzir falso positivo ou confirmar símbolos externos.
- Evite heurísticas amplas que aumentem falsos positivos.
- Evite analisar strings dinâmicas se a regra foi definida para literais.
- Não altere formatação ou estrutura fora do escopo da regra.

# Checklist para nova regra

Ao criar uma regra nova:

1. Escolher próximo ID `ARCH###`.
2. Adicionar constante em `src/Swa.Analyzers.Core/RuleIdentifiers.cs`.
3. Criar analyzer em `src/Swa.Analyzers.Core/Rules/`.
4. Definir `DiagnosticDescriptor`.
5. Adicionar testes em `tests/Swa.Analyzers.Tests/Rules/`.
6. Adicionar documentação em `docs/rules/ARCH###.md`.
7. Adicionar exemplos válidos e inválidos no SampleApp quando ajudar na validação manual.
8. Atualizar `src/Swa.Analyzers.Core/AnalyzerReleases.Unshipped.md`.
9. Atualizar tabela de regras no `README.md`.
10. Validar restore, build e testes.

# Checklist para alterar regra existente

Ao alterar uma regra existente:

1. Localizar analyzer, testes, documentação e exemplos da regra.
2. Entender a heurística atual antes de mudar.
3. Adicionar teste que reproduz o novo comportamento ou bug.
4. Fazer alteração mínima.
5. Ajustar docs e SampleApp se o comportamento público mudar.
6. Revisar falso positivo e falso negativo.
7. Executar testes focados e, quando possível, suíte completa.

# Testes obrigatorios

Ao criar ou alterar uma regra, adicionar ou revisar testes cobrindo:

- código inválido com diagnóstico esperado;
- código válido sem diagnóstico;
- casos de falso positivo;
- bordas relevantes da heurística;
- opções via `.editorconfig`, quando existirem;
- símbolos ou stubs necessários sem depender de pacotes externos desnecessários;
- comportamento com valor ausente ou inválido de configuração, quando existir configuração.

Use `tests/Swa.Analyzers.Tests/Verifier.cs` como padrão.

# Documentacao obrigatoria

Ao criar ou alterar comportamento de uma regra, atualizar:

- `docs/rules/ARCH###.md`
- `README.md`, se a lista ou configuração pública mudar
- `src/Swa.Analyzers.Core/AnalyzerReleases.Unshipped.md`, quando aplicável
- `src/Swa.Analyzers.SampleApp`, quando ajudar na validação manual

# Validacao

Comandos base:

```bash
dotnet restore ./Swa.Analyzers.slnx
dotnet build ./Swa.Analyzers.slnx --configuration Release --no-restore
dotnet test ./Swa.Analyzers.slnx --configuration Release -m:1
```

Para validação rápida após build:

```bash
dotnet test ./Swa.Analyzers.slnx --configuration Release --no-build -m:1
```

# Finalizacao

Antes de concluir:

1. Revise o diff.
2. Confirme que analyzer, testes, docs, SampleApp e release metadata estão coerentes.
3. Informe quais validações foram executadas.
4. Se não foi possível executar algum comando, registre claramente o motivo.
