---
name: dotnet-service-change
description: Compatibilidade temporária. Este repositório não é um serviço .NET; para alterações neste projeto, use as skills de Roslyn analyzer.
---

# Skill substituída para este repositório

Este repositório é o `CSF.Analyzers`, um projeto de Roslyn analyzers reutilizáveis para .NET.

Não aplique orientações de serviços .NET neste repositório, como:

- Clean Architecture de aplicação
- DDD de domínio de negócio
- controllers
- middlewares
- EF Core
- migrations
- Kafka
- Outbox
- implementação de autenticação/autorização de uma aplicação ou API
- ADRs de serviço

# Skill correta

Use uma das skills abaixo conforme a tarefa:

- `roslyn-analyzer-rule-change`
- `roslyn-analyzer-test-change`
- `roslyn-analyzer-doc-rule-change`
- `roslyn-analyzer-sample-app-change`
- `roslyn-analyzer-packaging-release-change`

# Fluxo mínimo

1. Leia `AGENTS.md`.
2. Escolha a skill Roslyn mais específica.
3. Trabalhe apenas nos arquivos relevantes ao projeto `CSF.Analyzers`.
4. Considere a stack real do repositório: pacote `CSF.Analyzers` em versão `1.1.0`, SDK .NET `10.0.203`, analyzers em `netstandard2.0`, SampleApp e testes em `net10.0`.
5. Valide com os comandos da solução `CSF.Analyzers.slnx`.
