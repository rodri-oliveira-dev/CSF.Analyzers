# Active Context

## Current Focus

Implementacao da regra `ARCH015 - Prohibit verbs in HTTP routes` no projeto de analyzers Roslyn.

## Notas

- A regra analisa somente paths literais de rotas HTTP em attributes MVC/Web API e Minimal APIs.
- `route_language` aceita `en-US` e `pt-BR`; valor ausente ou invalido usa `en-US`.
- `additional_verbs` e interpretado como JSON array de strings; valores malformados sao ignorados.
- A heuristica e conservadora para reduzir falsos positivos e nao tenta validar REST completo.
