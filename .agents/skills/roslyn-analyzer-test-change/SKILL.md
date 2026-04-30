---
name: roslyn-analyzer-test-change
description: Use esta skill ao criar, corrigir ou ampliar testes automatizados de analyzers Roslyn neste repositorio.
---

# Objetivo

Criar testes de analyzer claros, pequenos e confiaveis, cobrindo diagnosticos esperados, casos negativos, falsos positivos e configuracoes por `.editorconfig`.

# Quando usar

Use esta skill quando a tarefa envolver:

- testes em `tests/Swa.Analyzers.Tests`
- novos cenarios para uma regra `ARCH###`
- regressao de bug em analyzer
- stubs para frameworks externos
- testes com `.editorconfig`
- testes com multiplos arquivos de origem
- validacao de localizacao e argumentos do diagnostico

# Arquivos relevantes

Leia quando aplicavel:

- `tests/Swa.Analyzers.Tests/Verifier.cs`
- `tests/Swa.Analyzers.Tests/Rules/`
- `src/Swa.Analyzers.Core/Rules/`
- `docs/rules/ARCH###.md`
- `.editorconfig`
- `Directory.Packages.props`

# Regras de teste

- Use `Verifier<TAnalyzer>`.
- Use `Diagnostic(...)` com o ID da regra.
- Use marcadores `{|#0:...|}` para localizacoes quando isso melhorar clareza.
- Teste argumentos do diagnostico com `WithArguments(...)` quando a mensagem usa argumentos.
- Prefira nomes de teste que descrevam comportamento.
- Mantenha cada teste focado.
- Use stubs minimos quando precisar simular ASP.NET, xUnit, FluentAssertions, NSubstitute ou outros tipos externos.
- Nao adicione pacotes externos apenas para simplificar stubs, salvo necessidade clara.
- Evite testes que dependam de ordem instavel de diagnosticos, exceto quando a regra define uma ordem clara.
- Para regras configuraveis, teste valor ausente, valor valido, valor invalido e escopo por arquivo quando relevante.

# Cobertura minima recomendada

Para alteracao de regra, verifique se ha testes para:

- caso invalido principal;
- caso valido principal;
- falso positivo conhecido;
- borda da heuristica;
- simbolo externo ou namespace correto, quando a regra depender de semantica;
- simbolo parecido ou namespace incorreto, quando houver risco de falso positivo;
- configuracao por `.editorconfig`, se existir.

# Validacao

Comandos recomendados:

```bash
dotnet test ./Swa.Analyzers.slnx --configuration Release -m:1
```

Para validacao rapida apos build:

```bash
dotnet test ./Swa.Analyzers.slnx --configuration Release --no-build -m:1
```

Se alterar apenas uma regra, rode pelo menos os testes focados dessa regra quando possivel e informe exatamente o comando usado.
