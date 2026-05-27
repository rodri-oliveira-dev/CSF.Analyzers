---
name: roslyn-analyzer-doc-rule-change
description: Use esta skill ao criar ou atualizar documentação de regras ARCH### em docs/rules e referências públicas no README.
---

# Objetivo

Manter a documentação das regras consistente com o comportamento real dos analyzers, sem prometer heurísticas, configurações ou garantias que não existem no código.

# Quando usar

Use esta skill quando a tarefa envolver:

- novo arquivo `docs/rules/ARCH###.md`
- ajuste de comportamento documentado
- exemplos conformes e não conformes
- documentação de configurações por `.editorconfig`
- atualização da tabela de regras no `README.md`
- descrição de heurística ou limitações conhecidas

# Estrutura recomendada para docs/rules

Cada regra deve conter, quando aplicável:

1. Título com ID e resumo.
2. Objetivo.
3. Código não conforme.
4. Código conforme.
5. Configuração.
6. Heurística.
7. Limitações conhecidas.
8. Impacto esperado.

# Regras de escrita

- Documente apenas comportamento implementado.
- Seja claro sobre o que a regra não valida.
- Explique falsos positivos ou limitações esperadas.
- Mantenha exemplos pequenos e compiláveis quando possível.
- Use o mesmo ID, titulo e semântica do `DiagnosticDescriptor`.
- Quando a regra aceitar `.editorconfig`, mostre chaves e valores aceitos.
- Quando a regra tiver fallback de configuração, documente o fallback.
- Atualize o README se mudar lista de regras, severidade, categoria, nome público ou configuração pública.

# Checklist

Antes de concluir:

- O arquivo `docs/rules/ARCH###.md` bate com o analyzer?
- Os exemplos batem com os testes?
- O README precisa ser atualizado?
- A severidade e categoria documentadas estão corretas?
- As limitações conhecidas estão claras?
