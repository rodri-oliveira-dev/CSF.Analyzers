# CSF rename - source projects

## Resultado

Esta etapa migrou a solucao e os projetos de producao para a identidade `CSF.Analyzers.*`.

## Escopo validado

- A solucao principal passou a ser `CSF.Analyzers.slnx`.
- Os projetos de producao passaram a viver em `src/CSF.Analyzers.*`.
- O shared source passou a viver em `src/CSF.Analyzers.Common`.
- Namespaces e usings de producao passaram a usar `CSF.Analyzers.*`.
- O target transitive de Architecture passou a ser `CSF.Analyzers.Architecture.targets`.
- A propriedade interna de pacote passou a ser `IsCsfAnalyzerPackage`.

## Observacoes

As excecoes temporarias desta etapa foram encerradas por etapas posteriores. Este documento foi sanitizado durante a auditoria final para nao manter literais da identidade anterior no working tree.
