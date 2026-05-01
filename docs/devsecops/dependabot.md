# Dependabot

Este repositorio usa Dependabot para manter atualizacoes previsiveis sem reduzir a cobertura de seguranca.

## Ecossistemas monitorados

- `github-actions`: workflows em `.github/workflows`.
- `nuget`: dependencias .NET centralizadas em `Directory.Packages.props` e refletidas nos `packages.lock.json`.
- `dotnet-sdk`: SDK fixado em `global.json`.

Nao ha `registries` configurados porque o repositorio usa apenas fontes publicas. Nao adicione credenciais diretamente ao YAML; use secrets do GitHub se um feed privado for necessario no futuro.

## Agenda e limite de PRs

- GitHub Actions: semanal, segunda-feira, 09:00, `America/Sao_Paulo`, ate 5 PRs abertos.
- NuGet: semanal, segunda-feira, 09:30, `America/Sao_Paulo`, ate 5 PRs abertos.
- .NET SDK: mensal, 10:00, `America/Sao_Paulo`, ate 2 PRs abertos.

Version updates usam cooldown moderado para evitar PRs logo apos publicacoes recentes. Security updates nao sao atrasados pelo cooldown do Dependabot.

## Grupos

- `github-actions`: agrupa atualizacoes de actions.
- `test-packages`: agrupa xUnit, Microsoft.NET.Test.Sdk e pacotes de teste de analyzers.
- `roslyn-and-analyzer-packages`: agrupa pacotes Microsoft.CodeAnalysis usados pelo analyzer.
- `coverage-and-tools`: agrupa ferramentas de cobertura e relatorio.

Se um pacote novo nao se encaixar nesses grupos, deixe o PR individual ate haver um padrao claro.

## Revisao de PRs

Revise release notes, confira se `packages.lock.json` foi atualizado quando NuGet mudar e aguarde os checks de restore, build e test. Major updates devem receber revisao manual mais cuidadosa; se houver breaking change conhecido, prefira fechar o PR com justificativa ou adicionar um `ignore` especifico e temporario.

As GitHub Actions deste repositorio sao pinadas por SHA completo. Mantenha esse padrao: revise a release/tag correspondente e aceite o PR do Dependabot apenas quando o novo SHA estiver coerente com a versao esperada. Se o SHA atual nao estiver associado a uma tag, o Dependabot pode sugerir o commit mais recente em vez da ultima release. Nao troque SHA por tag apenas para facilitar updates.
