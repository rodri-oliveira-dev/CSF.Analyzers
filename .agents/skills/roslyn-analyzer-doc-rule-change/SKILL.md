---
name: roslyn-analyzer-doc-rule-change
description: Use esta skill ao criar ou atualizar documentacao de regras REL###, ARC### ou TST### em docs/rules e referencias publicas no README.
---

# Objetivo

Manter a documentacao das regras consistente com o comportamento real dos analyzers, sem prometer heuristicas, configuracoes ou garantias que nao existem no codigo.

# Quando usar

Use esta skill quando a tarefa envolver:

- novo arquivo `docs/rules/<grupo>/<ID>.md`;
- ajuste de comportamento documentado;
- exemplos conformes e nao conformes;
- documentacao de configuracoes por `.editorconfig`;
- atualizacao da tabela de regras no `README.md`.

# Estrutura recomendada

Cada regra deve conter, quando aplicavel:

1. Titulo com ID e resumo.
2. Objetivo.
3. Codigo nao conforme.
4. Codigo conforme.
5. Configuracao.
6. Heuristica.
7. Limitacoes conhecidas.
8. Impacto esperado.

# Regras de escrita

- Documente apenas comportamento implementado.
- Seja claro sobre o que a regra nao valida.
- Use o mesmo ID, titulo e semantica do `DiagnosticDescriptor`.
- Quando a regra aceitar `.editorconfig`, mostre chaves e valores aceitos.
- Quando houver fallback de configuracao, documente o fallback.
- Atualize o README se mudar lista de regras, severidade, categoria, nome publico ou configuracao publica.
