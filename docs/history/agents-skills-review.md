# Revisao das skills dos agentes

## Data
2026-05-04

## Versao da aplicaÃ§Ã£o identificada
Nota historica: quando estÃ¡ revisÃ£o foi escrita, a versÃ£o oficial do pacote era `1.1.0` e ficava em `src/Swa.Analyzers.Core/Swa.Analyzers.Core.csproj` no atributo `<VersionPrefix>`. O fluxo atual usa GitVersion, configurado em `GitVersion.yml`, como fonte da versÃ£o publicada.

SDK .NET fixado: `10.0.203`, identificado em `global.json` com `rollForward` para `latestFeature`.

Target frameworks identificados:

- `src/Swa.Analyzers.Core`: `netstandard2.0`
- `src/Swa.Analyzers.SampleApp`: `net10.0`
- `tests/Swa.Analyzers.Tests`: `net10.0`

## Stack principal identificada
- Projeto de analyzers Roslyn reutilizÃ¡veis para .NET.
- Pacote NuGet `Swa.Analyzers`, empacotado a partir de `src/Swa.Analyzers.Core`.
- Roslyn `Microsoft.CodeAnalysis.*` `5.3.0`.
- C# `13.0` no projeto Core.
- xUnit `2.9.3`, `Microsoft.NET.Test.Sdk` `18.5.1` e `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` `1.1.3` nos testes.
- Central Package Management via `Directory.Packages.props`.
- Lock file NuGet habilitado por `RestorePackagesWithLockFile`.
- Solucao principal `Swa.Analyzers.slnx`.
- SampleApp de console para validaÃ§Ã£o manual, referÃªnciando o Core como analyzer.
- GitHub Actions para CI, CodeQL, dependency review, release check e release.
- NÃ£o foram encontrados `.clinerules/`, `memory-bank/`, Dockerfile ou arquivos Docker Compose neste repositÃ³rio.

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
  problema: a skill ainda usava exemplos e linguagem centrados em fase `0.x`, apesar do pacote real estar em `1.1.0` e o projeto jÃ¡ seguir SemVer pÃ³s-`1.0.0`.
  impacto: poderia induzir o agente a escolher exemplos de versÃ£o antigos ou raciocinar como se o pacote ainda nÃ£o estivesse estÃ¡vel.
  decisÃ£o tomada: atualizar a skill para declarar a versÃ£o real identificada, usar exemplos `1.x` e priorizar a polÃ­tica pÃ³s-`1.0.0`.

- arquivo: `.agents/skills/semantic-versioning/SKILL.md`
  problema: a regra sobre documentaÃ§Ã£o era ampla demais e podia sugerir bump de `VersionPrefix`/`CHANGELOG.md` para alteraÃ§Ãµes internas de skills ou relatÃ³rios.
  impacto: poderia gerar mudanÃ§as de release desnecessÃ¡rias para tarefas que nÃ£o afetam consumidores do pacote.
  decisÃ£o tomada: limitar a obrigatoriedade de bump e changelog a documentaÃ§Ã£o pÃºblica de regra, README, release ou pacote, deixando claro que `.agents/skills` e `docs/reviews` nÃ£o exigem bump quando nÃ£o alteram comportamento pÃºblico.

- arquivo: `.agents/skills/dotnet-service-change/SKILL.md`
  problema: a lista de orientaÃ§Ãµes proibidas mencionava `autenticaÃ§Ã£o/autorizaÃ§Ã£o` de forma genÃ©rica, enquanto o repositÃ³rio possui regras de analyzer relacionadas a autorizaÃ§Ã£o.
  impacto: poderia criar ambiguidade entre nÃ£o implementar autenticaÃ§Ã£o de uma API e permitir alteraÃ§Ãµes legÃ­timas em regras de analyzer de seguranÃ§a.
  decisÃ£o tomada: especÃ­ficar que a restricao se refere a implementaÃ§Ã£o de autenticaÃ§Ã£o/autorizaÃ§Ã£o de uma aplicaÃ§Ã£o ou API, nÃ£o a regras de analyzer.

- arquivo: `.agents/skills/dotnet-service-change/SKILL.md`
  problema: a skill mencionava `LedgerService.slnx`, soluÃ§Ã£o que nÃ£o existe neste repositÃ³rio.
  impacto: poderia reforcar um contexto legado de serviÃ§o .NET que o proprio `AGENTS.md` manda evitar.
  decisÃ£o tomada: remover a referÃªncia legada e substituir o fluxo mÃ­nimo por dados reais de stack e versÃ£o do `Swa.Analyzers`.

## Ajustes realizados
- Atualizada `.agents/skills/semantic-versioning/SKILL.md` para refletir o fluxo de versionamento vigente na epoca da revisÃ£o.
- Normalizada `.agents/skills/semantic-versioning/SKILL.md` em ASCII para evitar ruÃ­do de encoding nas instrucoes consumidas por agentes.
- Atualizada `.agents/skills/dotnet-service-change/SKILL.md` para remover contexto legado, explicitar a stack real do repositÃ³rio e reduzir ambiguidade sobre autenticaÃ§Ã£o/autorizaÃ§Ã£o.
- Criado este relatorio em `docs/reviews/agents-skills-review.md`.

## Pontos de atencao
- `Directory.Build.props` nÃ£o existe atualmente; o repositÃ³rio usa `Directory.Build.targets`.
- `.clinerules/` e `memory-bank/` nÃ£o existem atualmente, entÃ£o nÃ£o houve comparaÃ§Ã£o com essas fontes.
- Dockerfile e arquivos Docker Compose nÃ£o existem atualmente, entÃ£o nenhuma skill deve orientar uso de Docker neste projeto.
- O README informa `.NET SDK 10.x`, enquanto `global.json` fixa `10.0.203`; essa diferenca foi considerada coerente porque o README descreve a familia suportada e o `global.json` fixa a versÃ£o local.
- O SampleApp contÃ©m stubs e exemplos de ASP.NET Core, EF Core, FluentAssertions, NSubstitute, Moq e xUnit apenas para demonstraÃ§Ã£o e reconhecimento simbÃ³lico dos analyzers; isso nÃ£o caracteriza o repositÃ³rio como aplicaÃ§Ã£o de serviÃ§o .NET.

## Validacoes executadas
- `git status --short` antes das alteraÃ§Ãµes: arvore limpa.
- `dotnet restore ./Swa.Analyzers.slnx`: sucesso.
- `dotnet build ./Swa.Analyzers.slnx --configuration Release --no-restore`: sucesso, com warnings esperados do SampleApp e exemplos invÃ¡lidos.
- `dotnet test ./Swa.Analyzers.slnx --configuration Release --no-build -m:1`: sucesso, 432 testes aprovados.

## Conclusao
As skills estÃ£o coerentes com o projeto apÃ³s a revisÃ£o. Elas agora refletem a versÃ£o real do pacote, a stack .NET/Roslyn atual, a separaÃ§Ã£o entre analyzer, testes, SampleApp, documentaÃ§Ã£o e empacotamento, e evitam orientar o Codex a aplicar padrÃµes de aplicaÃ§Ãµes de negÃ³cio neste repositÃ³rio.
