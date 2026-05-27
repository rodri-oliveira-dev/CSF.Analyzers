# Active Context

## Current Focus

Implementação da regra `ARCH015 - Proíba verbos em rotas HTTP` no projeto de analyzers Roslyn.

## Notas

- A regra analisa somente paths literais de rotas HTTP em attributes MVC/Web API e Minimal APIs.
- `route_language` aceita `en-US` e `pt-BR`; valor ausente ou inválido usa `en-US`.
- `additional_verbs` é interpretado como JSON array de strings; valores malformados são ignorados.
- A heurística é conservadora para reduzir falsos positivos e não tenta validar REST completo.
