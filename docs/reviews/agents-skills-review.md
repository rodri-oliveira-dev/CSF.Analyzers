# Revisao das skills dos agentes

## Data
2026-05-04

## Versao da aplicacao identificada
Nota historica: quando esta revisao foi escrita, a versao oficial do pacote era `1.1.0` e ficava em `src/Swa.Analyzers.Core/Swa.Analyzers.Core.csproj` no atributo `<VersionPrefix>`. O fluxo atual usa GitVersion, configurado em `GitVersion.yml`, como fonte da versao publicada.

SDK .NET fixado: `10.0.203`, identificado em `global.json` com `rollForward` para `latestFeature`.

Target frameworks identificados:

- `src/Swa.Analyzers.Core`: `netstandard2.0`
- `src/Swa.Analyzers.SampleApp`: `net10.0`
- `tests/Swa.Analyzers.Tests`: `net10.0`

## Stack principal identificada
- Projeto de analyzers Roslyn reutilizaveis para .NET.
- Pacote NuGet `Swa.Analyzers`, empacotado a partir de `src/Swa.Analyzers.Core`.
- Roslyn `Microsoft.CodeAnalysis.*` `5.3.0`.
- C# `13.0` no projeto Core.
- xUnit `2.9.3`, `Microsoft.NET.Test.Sdk` `18.5.1` e `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` `1.1.3` nos testes.
- Central Package Management via `Directory.Packages.props`.
- Lock file NuGet habilitado por `RestorePackagesWithLockFile`.
- Solucao principal `Swa.Analyzers.slnx`.
- SampleApp de console para validacao manual, referenciando o Core como analyzer.
- GitHub Actions para CI, CodeQL, dependency review, release check e release.
- Nao foram encontrados `.clinerules/`, `memory-bank/`, Dockerfile ou arquivos Docker Compose neste repositorio.

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
  problema: a skill ainda usava exemplos e linguagem centrados em fase `0.x`, apesar do pacote real estar em `1.1.0` e o projeto ja seguir SemVer pos-`1.0.0`.
  impacto: poderia induzir o agente a escolher exemplos de versao antigos ou raciocinar como se o pacote ainda nao estivesse estavel.
  decisao tomada: atualizar a skill para declarar a versao real identificada, usar exemplos `1.x` e priorizar a politica pos-`1.0.0`.

- arquivo: `.agents/skills/semantic-versioning/SKILL.md`
  problema: a regra sobre documentacao era ampla demais e podia sugerir bump de `VersionPrefix`/`CHANGELOG.md` para alteracoes internas de skills ou relatorios.
  impacto: poderia gerar mudancas de release desnecessarias para tarefas que nao afetam consumidores do pacote.
  decisao tomada: limitar a obrigatoriedade de bump e changelog a documentacao publica de regra, README, release ou pacote, deixando claro que `.agents/skills` e `docs/reviews` nao exigem bump quando nao alteram comportamento publico.

- arquivo: `.agents/skills/dotnet-service-change/SKILL.md`
  problema: a lista de orientacoes proibidas mencionava `autenticacao/autorizacao` de forma generica, enquanto o repositorio possui regras de analyzer relacionadas a autorizacao.
  impacto: poderia criar ambiguidade entre nao implementar autenticacao de uma API e permitir alteracoes legitimas em regras ARCH de seguranca.
  decisao tomada: especificar que a restricao se refere a implementacao de autenticacao/autorizacao de uma aplicacao ou API, nao a regras de analyzer.

- arquivo: `.agents/skills/dotnet-service-change/SKILL.md`
  problema: a skill mencionava `LedgerService.slnx`, solucao que nao existe neste repositorio.
  impacto: poderia reforcar um contexto legado de servico .NET que o proprio `AGENTS.md` manda evitar.
  decisao tomada: remover a referencia legada e substituir o fluxo minimo por dados reais de stack e versao do `Swa.Analyzers`.

## Ajustes realizados
- Atualizada `.agents/skills/semantic-versioning/SKILL.md` para refletir o fluxo de versionamento vigente na epoca da revisao.
- Normalizada `.agents/skills/semantic-versioning/SKILL.md` em ASCII para evitar ruido de encoding nas instrucoes consumidas por agentes.
- Atualizada `.agents/skills/dotnet-service-change/SKILL.md` para remover contexto legado, explicitar a stack real do repositorio e reduzir ambiguidade sobre autenticacao/autorizacao.
- Criado este relatorio em `docs/reviews/agents-skills-review.md`.

## Pontos de atencao
- `Directory.Build.props` nao existe atualmente; o repositorio usa `Directory.Build.targets`.
- `.clinerules/` e `memory-bank/` nao existem atualmente, entao nao houve comparacao com essas fontes.
- Dockerfile e arquivos Docker Compose nao existem atualmente, entao nenhuma skill deve orientar uso de Docker neste projeto.
- O README informa `.NET SDK 10.x`, enquanto `global.json` fixa `10.0.203`; essa diferenca foi considerada coerente porque o README descreve a familia suportada e o `global.json` fixa a versao local.
- O SampleApp contem stubs e exemplos de ASP.NET Core, EF Core, FluentAssertions, NSubstitute, Moq e xUnit apenas para demonstracao e reconhecimento simbolico dos analyzers; isso nao caracteriza o repositorio como aplicacao de servico .NET.

## Validacoes executadas
- `git status --short` antes das alteracoes: arvore limpa.
- `dotnet restore ./Swa.Analyzers.slnx`: sucesso.
- `dotnet build ./Swa.Analyzers.slnx --configuration Release --no-restore`: sucesso, com warnings esperados do SampleApp e exemplos invalidos.
- `dotnet test ./Swa.Analyzers.slnx --configuration Release --no-build -m:1`: sucesso, 432 testes aprovados.

## Conclusao
As skills estao coerentes com o projeto apos a revisao. Elas agora refletem a versao real do pacote, a stack .NET/Roslyn atual, a separacao entre analyzer, testes, SampleApp, documentacao e empacotamento, e evitam orientar o Codex a aplicar padroes de aplicacoes de negocio neste repositorio.
