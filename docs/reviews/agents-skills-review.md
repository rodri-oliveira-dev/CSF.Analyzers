# Revisao das skills dos agentes

## Data
2026-05-04

## Versao da aplicação identificada
Nota historica: quando está revisão foi escrita, a versão oficial do pacote era `1.1.0` e ficava em `src/Swa.Analyzers.Core/Swa.Analyzers.Core.csproj` no atributo `<VersionPrefix>`. O fluxo atual usa GitVersion, configurado em `GitVersion.yml`, como fonte da versão publicada.

SDK .NET fixado: `10.0.203`, identificado em `global.json` com `rollForward` para `latestFeature`.

Target frameworks identificados:

- `src/Swa.Analyzers.Core`: `netstandard2.0`
- `src/Swa.Analyzers.SampleApp`: `net10.0`
- `tests/Swa.Analyzers.Tests`: `net10.0`

## Stack principal identificada
- Projeto de analyzers Roslyn reutilizáveis para .NET.
- Pacote NuGet `Swa.Analyzers`, empacotado a partir de `src/Swa.Analyzers.Core`.
- Roslyn `Microsoft.CodeAnalysis.*` `5.3.0`.
- C# `13.0` no projeto Core.
- xUnit `2.9.3`, `Microsoft.NET.Test.Sdk` `18.5.1` e `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` `1.1.3` nos testes.
- Central Package Management via `Directory.Packages.props`.
- Lock file NuGet habilitado por `RestorePackagesWithLockFile`.
- Solucao principal `Swa.Analyzers.slnx`.
- SampleApp de console para validação manual, referênciando o Core como analyzer.
- GitHub Actions para CI, CodeQL, dependency review, release check e release.
- Não foram encontrados `.clinerules/`, `memory-bank/`, Dockerfile ou arquivos Docker Compose neste repositório.

## Arquivos analisados
- `AGENTS.md`
- `README.md`
- `.editorconfig`
- `global.json`
- `Directory.Packages.props`
- `Directory.Build.targets`
- `Swa.Analyzers.slnx`
- `CHANGELOG.md`
- `docs/adoption.md`
- `docs/release.md`
- `src/Swa.Analyzers.Core/Swa.Analyzers.Core.csproj`
- `src/Swa.Analyzers.SampleApp/Swa.Analyzers.SampleApp.csproj`
- `tests/Swa.Analyzers.Tests/Swa.Analyzers.Tests.csproj`
- `src/Swa.Analyzers.SampleApp/README.md`
- `.github/dependabot.yml`
- `.github/workflows/codeql.yml`
- `.github/workflows/dependency-review.yml`
- `.github/workflows/dotnet.yml`
- `.github/workflows/release-check.yml`
- `.github/workflows/release.yml`
- `.agents/skills/dotnet-service-change/SKILL.md`
- `.agents/skills/roslyn-analyzer-doc-rule-change/SKILL.md`
- `.agents/skills/roslyn-analyzer-packaging-release-change/SKILL.md`
- `.agents/skills/roslyn-analyzer-rule-change/SKILL.md`
- `.agents/skills/roslyn-analyzer-sample-app-change/SKILL.md`
- `.agents/skills/roslyn-analyzer-test-change/SKILL.md`
- `.agents/skills/semantic-versioning/SKILL.md`

## Inconsistencias encontradas
- arquivo: `.agents/skills/semantic-versioning/SKILL.md`
  problema: a skill ainda usava exemplos e linguagem centrados em fase `0.x`, apesar do pacote real estar em `1.1.0` e o projeto já seguir SemVer pós-`1.0.0`.
  impacto: poderia induzir o agente a escolher exemplos de versão antigos ou raciocinar como se o pacote ainda não estivesse estável.
  decisão tomada: atualizar a skill para declarar a versão real identificada, usar exemplos `1.x` e priorizar a política pós-`1.0.0`.

- arquivo: `.agents/skills/semantic-versioning/SKILL.md`
  problema: a regra sobre documentação era ampla demais e podia sugerir bump de `VersionPrefix`/`CHANGELOG.md` para alterações internas de skills ou relatórios.
  impacto: poderia gerar mudanças de release desnecessárias para tarefas que não afetam consumidores do pacote.
  decisão tomada: limitar a obrigatoriedade de bump e changelog a documentação pública de regra, README, release ou pacote, deixando claro que `.agents/skills` e `docs/reviews` não exigem bump quando não alteram comportamento público.

- arquivo: `.agents/skills/dotnet-service-change/SKILL.md`
  problema: a lista de orientações proibidas mencionava `autenticação/autorização` de forma genérica, enquanto o repositório possui regras de analyzer relacionadas a autorização.
  impacto: poderia criar ambiguidade entre não implementar autenticação de uma API e permitir alterações legítimas em regras ARCH de segurança.
  decisão tomada: específicar que a restricao se refere a implementação de autenticação/autorização de uma aplicação ou API, não a regras de analyzer.

- arquivo: `.agents/skills/dotnet-service-change/SKILL.md`
  problema: a skill mencionava `LedgerService.slnx`, solução que não existe neste repositório.
  impacto: poderia reforcar um contexto legado de serviço .NET que o proprio `AGENTS.md` manda evitar.
  decisão tomada: remover a referência legada e substituir o fluxo mínimo por dados reais de stack e versão do `Swa.Analyzers`.

## Ajustes realizados
- Atualizada `.agents/skills/semantic-versioning/SKILL.md` para refletir o fluxo de versionamento vigente na epoca da revisão.
- Normalizada `.agents/skills/semantic-versioning/SKILL.md` em ASCII para evitar ruído de encoding nas instrucoes consumidas por agentes.
- Atualizada `.agents/skills/dotnet-service-change/SKILL.md` para remover contexto legado, explicitar a stack real do repositório e reduzir ambiguidade sobre autenticação/autorização.
- Criado este relatorio em `docs/reviews/agents-skills-review.md`.

## Pontos de atencao
- `Directory.Build.props` não existe atualmente; o repositório usa `Directory.Build.targets`.
- `.clinerules/` e `memory-bank/` não existem atualmente, então não houve comparação com essas fontes.
- Dockerfile e arquivos Docker Compose não existem atualmente, então nenhuma skill deve orientar uso de Docker neste projeto.
- O README informa `.NET SDK 10.x`, enquanto `global.json` fixa `10.0.203`; essa diferenca foi considerada coerente porque o README descreve a familia suportada e o `global.json` fixa a versão local.
- O SampleApp contém stubs e exemplos de ASP.NET Core, EF Core, FluentAssertions, NSubstitute, Moq e xUnit apenas para demonstração e reconhecimento simbólico dos analyzers; isso não caracteriza o repositório como aplicação de serviço .NET.

## Validacoes executadas
- `git status --short` antes das alterações: arvore limpa.
- `dotnet restore ./Swa.Analyzers.slnx`: sucesso.
- `dotnet build ./Swa.Analyzers.slnx --configuration Release --no-restore`: sucesso, com warnings esperados do SampleApp e exemplos inválidos.
- `dotnet test ./Swa.Analyzers.slnx --configuration Release --no-build -m:1`: sucesso, 432 testes aprovados.

## Conclusao
As skills estão coerentes com o projeto após a revisão. Elas agora refletem a versão real do pacote, a stack .NET/Roslyn atual, a separação entre analyzer, testes, SampleApp, documentação e empacotamento, e evitam orientar o Codex a aplicar padrões de aplicações de negócio neste repositório.
