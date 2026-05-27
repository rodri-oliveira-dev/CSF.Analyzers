---
name: roslyn-analyzer-test-change
description: Use esta skill ao criar, corrigir ou ampliar testes automatizados de analyzers Roslyn neste repositório.
---

# Objetivo

Criar testes de analyzer claros, pequenos e confiáveis, cobrindo diagnósticos esperados, casos negativos, falsos positivos e configurações por `.editorconfig`.

# Quando usar

Use esta skill quando a tarefa envolver:

- testes em `tests/Swa.Analyzers.Tests`
- novos cenários para uma regra `ARCH###`
- regressão de bug em analyzer
- stubs para frameworks externos
- testes com `.editorconfig`
- testes com múltiplos arquivos de origem
- validação de localização e argumentos do diagnóstico

# Arquivos relevantes

Leia quando aplicável:

- `tests/Swa.Analyzers.Tests/Verifier.cs`
- `tests/Swa.Analyzers.Tests/Rules/`
- `src/Swa.Analyzers.Core/Rules/`
- `docs/rules/ARCH###.md`
- `.editorconfig`
- `Directory.Packages.props`

# Regras de teste

- Use `Verifier<TAnalyzer>`.
- Use `Diagnostic(...)` com o ID da regra.
- Use marcadores `{|#0:...|}` para localizações quando isso melhorar clareza.
- Teste argumentos do diagnóstico com `WithArguments(...)` quando a mensagem usa argumentos.
- Prefira nomes de teste que descrevam comportamento.
- Mantenha cada teste focado.
- Use stubs mínimos quando precisar simular ASP.NET, xUnit, FluentAssertions, NSubstitute ou outros tipos externos.
- Não adicione pacotes externos apenas para simplificar stubs, salvo necessidade clara.
- Evite testes que dependam de ordem instavel de diagnósticos, exceto quando a regra define uma ordem clara.
- Para regras configuráveis, teste valor ausente, valor válido, valor inválido e escopo por arquivo quando relevante.

# Cobertura mínima recomendada

Para alteração de regra, verifique se há testes para:

- caso inválido principal;
- caso válido principal;
- falso positivo conhecido;
- borda da heurística;
- símbolo externo ou namespace correto, quando a regra depender de semântica;
- símbolo parecido ou namespace incorreto, quando houver risco de falso positivo;
- configuração por `.editorconfig`, se existir.

# Validacao

Comandos recomendados:

```bash
dotnet test ./Swa.Analyzers.slnx --configuration Release -m:1
```

Para validação rápida após build:

```bash
dotnet test ./Swa.Analyzers.slnx --configuration Release --no-build -m:1
```

Se alterar apenas uma regra, rode pelo menos os testes focados dessa regra quando possível e informe exatamente o comando usado.
