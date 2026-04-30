---
name: roslyn-analyzer-doc-rule-change
description: Use esta skill ao criar ou atualizar documentacao de regras ARCH### em docs/rules e referencias publicas no README.
---

# Objetivo

Manter a documentacao das regras consistente com o comportamento real dos analyzers, sem prometer heuristicas, configuracoes ou garantias que nao existem no codigo.

# Quando usar

Use esta skill quando a tarefa envolver:

- novo arquivo `docs/rules/ARCH###.md`
- ajuste de comportamento documentado
- exemplos conformes e nao conformes
- documentacao de configuracoes por `.editorconfig`
- atualizacao da tabela de regras no `README.md`
- descricao de heuristica ou limitacoes conhecidas

# Estrutura recomendada para docs/rules

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
- Explique falsos positivos ou limitacoes esperadas.
- Mantenha exemplos pequenos e compilaveis quando possivel.
- Use o mesmo ID, titulo e semantica do `DiagnosticDescriptor`.
- Quando a regra aceitar `.editorconfig`, mostre chaves e valores aceitos.
- Quando a regra tiver fallback de configuracao, documente o fallback.
- Atualize o README se mudar lista de regras, severidade, categoria, nome publico ou configuracao publica.

# Checklist

Antes de concluir:

- O arquivo `docs/rules/ARCH###.md` bate com o analyzer?
- Os exemplos batem com os testes?
- O README precisa ser atualizado?
- A severidade e categoria documentadas estao corretas?
- As limitacoes conhecidas estao claras?
