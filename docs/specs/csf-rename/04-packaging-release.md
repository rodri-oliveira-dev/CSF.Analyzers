# CSF rename - packaging and release

## Resultado

Esta etapa migrou PackageIds, metadados NuGet, scripts, workflows, hooks e validacoes de release para `CSF.Analyzers.*`.

## Escopo validado

- PackageIds dos analyzers:
  - `CSF.Analyzers.Reliability`
  - `CSF.Analyzers.Architecture`
  - `CSF.Analyzers.Testing`
- Assemblies e PDBs gerados usam `CSF.Analyzers.*`.
- `RepositoryUrl` e `PackageProjectUrl` apontam para `https://github.com/rodri-oliveira-dev/CSF.Analyzers`.
- Help links de diagnosticos apontam para `https://github.com/rodri-oliveira-dev/CSF.Analyzers/blob/main/docs/rules/`.
- O pacote Architecture inclui `buildTransitive/CSF.Analyzers.Architecture.targets`.
- Scripts de release, inspecao e isolamento validam a identidade `CSF.Analyzers.*`.
- Workflows e hooks usam `CSF.Analyzers.slnx`.

## Observacoes

As validacoes falham se pacotes ou arquivos de analyzer com identidade anterior forem gerados. Este documento foi sanitizado durante a auditoria final para nao manter literais da identidade anterior no working tree.
