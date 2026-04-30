---
name: dotnet-service-change
description: Compatibilidade temporaria. Este repositorio nao e um servico .NET; para alteracoes neste projeto, use as skills de Roslyn analyzer.
---

# Skill substituida para este repositorio

Este repositorio e o `Swa.Analyzers`, um projeto de Roslyn analyzers reutilizaveis para .NET.

Nao aplique orientacoes de servicos .NET neste repositorio, como:

- Clean Architecture de aplicacao
- DDD de dominio de negocio
- controllers
- middlewares
- EF Core
- migrations
- Kafka
- Outbox
- autenticacao/autorizacao
- ADRs de servico
- `LedgerService.slnx`

# Skill correta

Use uma das skills abaixo conforme a tarefa:

- `roslyn-analyzer-rule-change`
- `roslyn-analyzer-test-change`
- `roslyn-analyzer-doc-rule-change`
- `roslyn-analyzer-sample-app-change`
- `roslyn-analyzer-packaging-release-change`

# Fluxo minimo

1. Leia `AGENTS.md`.
2. Escolha a skill Roslyn mais especifica.
3. Trabalhe apenas nos arquivos relevantes ao projeto `Swa.Analyzers`.
4. Valide com os comandos da solucao `Swa.Analyzers.slnx`.
