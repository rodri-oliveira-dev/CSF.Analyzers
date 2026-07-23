---
name: roslyn-analyzer-rule-change
description: Use esta skill ao criar ou alterar regras Roslyn REL###, ARC### ou TST### neste repositorio, incluindo analyzer, testes, documentacao, samples, README e metadados de release.
---

# Objetivo

Executar mudancas em regras Roslyn do projeto `CSF.Analyzers` com seguranca, baixo ruido e consistencia com os tres pacotes v2.

# Quando usar

Use esta skill quando a tarefa envolver:

- criacao ou alteracao de regra `REL###`, `ARC###` ou `TST###`;
- ajuste de analyzer, `DiagnosticDescriptor`, heuristica ou opcoes via `.editorconfig`;
- ajuste em `RuleIdentifiers`;
- testes de analyzer;
- documentacao em `docs/rules`;
- exemplos em `samples/CSF.Analyzers.*.Sample`;
- atualizacao de `AnalyzerReleases.Unshipped.md`.

# Antes de alterar

1. Identifique o pacote afetado: Reliability, Architecture ou Testing.
2. Leia os arquivos relacionados:
   - `AGENTS.md`
   - `README.md`
   - `.editorconfig`
   - `Directory.Packages.props`
   - `global.json`
   - `src/CSF.Analyzers.<Pacote>/RuleIdentifiers.cs`
   - `src/CSF.Analyzers.<Pacote>/Rules/`
   - `tests/CSF.Analyzers.<Pacote>.Tests/Rules/`
   - `tests/CSF.Analyzers.TestSupport/Verifier.cs`
   - `docs/rules/<grupo>/`
   - `samples/CSF.Analyzers.<Pacote>.Sample/`
   - `src/CSF.Analyzers.<Pacote>/AnalyzerReleases.Unshipped.md`

# Regras de implementacao

- Faca a menor mudanca possivel.
- Preserve a separacao entre analyzer, testes, documentacao, samples e release metadata.
- Nao adicione `Version=` em `PackageReference`.
- Nao adicione dependencias novas sem necessidade clara.
- Preserve os projetos de pacote em `netstandard2.0`.
- Use IDs definidos no `RuleIdentifiers.cs` do pacote correto.
- Mantenha titulo, mensagem, categoria, severidade e help link consistentes.
- Use `DiagnosticDescriptor` com `RuleHelpLinks.ForRule(...)`.
- Use `EnableConcurrentExecution()`.
- Configure codigo gerado explicitamente com `ConfigureGeneratedCodeAnalysis(...)`.
- Use `CancellationToken` em chamadas semanticas.
- Evite heuristicas amplas que aumentem falsos positivos.

# Checklist para nova regra

1. Escolher proximo ID no prefixo do pacote correto.
2. Adicionar constante no `RuleIdentifiers.cs` do pacote.
3. Criar analyzer em `src/CSF.Analyzers.<Pacote>/Rules/`.
4. Definir `DiagnosticDescriptor`.
5. Adicionar testes em `tests/CSF.Analyzers.<Pacote>.Tests/Rules/`.
6. Adicionar documentacao em `docs/rules/<grupo>/<ID>.md`.
7. Adicionar exemplos validos e invalidos no sample do pacote quando ajudar.
8. Atualizar `AnalyzerReleases.Unshipped.md` do pacote.
9. Atualizar tabela de regras no `README.md`.
10. Validar restore, build e testes.

# Validacao

```bash
dotnet restore ./CSF.Analyzers.slnx
dotnet build ./CSF.Analyzers.slnx --configuration Release --no-restore
dotnet test ./CSF.Analyzers.slnx --configuration Release -m:1
```
