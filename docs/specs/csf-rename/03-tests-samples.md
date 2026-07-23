# CSF rename - tests and samples

## Resultado

Esta etapa migrou testes, test support e samples para a identidade `CSF.Analyzers.*`.

## Escopo validado

- Os diretorios e projetos em `tests/` usam nomes `CSF.Analyzers.*`.
- Os diretorios e projetos em `samples/` usam nomes `CSF.Analyzers.*`.
- `CSF.Analyzers.slnx` referencia apenas os paths novos.
- Namespaces proprios de testes e samples usam `CSF.Analyzers.*`.
- `InternalsVisibleTo` aponta para assemblies de teste `CSF.Analyzers.*`.
- Includes de `Verifier.cs` apontam para `tests/CSF.Analyzers.TestSupport`.
- IDs `REL###`, `ARC###` e `TST###` permaneceram inalterados.

## Observacoes

As excecoes temporarias desta etapa foram encerradas por etapas posteriores. Este documento foi sanitizado durante a auditoria final para nao manter literais da identidade anterior no working tree.
