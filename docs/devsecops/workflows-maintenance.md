# Manutencao dos workflows

O workflow `.github/workflows/release.yml` valida, empacota e cria a GitHub Release do pacote `Swa.Analyzers` a partir do `VersionPrefix`, usando a tag `v{VersionPrefix}`.

A release deve ser imutavel: a mesma versao nao deve recriar release nem sobrescrever assets existentes. O upload dos pacotes deve falhar se um asset com o mesmo nome ja existir.

Mantenha `permissions: {}` no topo do workflow. Permissoes elevadas devem ser declaradas apenas por job, e `contents: write` deve permanecer somente no job de release.

## Pacotes e provenance

- O workflow gera `.nupkg` e `.snupkg`.
- Os artifacts intermediarios possuem retencao curta.
- Os pacotes devem ter artifact attestation.
- A publicacao NuGet permanece desativada ate existir environment protegido e secret apropriado.

## Pinning de actions

As actions devem permanecer pinadas por SHA completo. Ao atualizar uma action, revisar release notes e substituir o SHA pelo commit correspondente.

## Nomes dos workflows

Use nomes no formato `Dominio - Objetivo`, por exemplo `CI - Build and Test` ou `Security - CodeQL`, para facilitar leitura dos checks no PR.

## Cache e validacao .NET

O cache NuGet deve continuar baseado nos lock files. Evite chaves amplas que ignorem `packages.lock.json`. A criacao de reusable workflow deve ser considerada apenas quando houver drift real entre CI e release.

## Filtros de paths

Evite filtros agressivos. Nao ignore mudancas em codigo, testes, regras, workflows, scripts, release metadata, changelog ou arquivos MSBuild. Filtros so devem ser usados quando houver ganho claro e sem reduzir validacoes de release e seguranca.
