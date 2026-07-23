---
name: roslyn-analyzer-test-change
description: Use esta skill ao criar, corrigir ou ampliar testes automatizados de analyzers Roslyn neste repositorio.
---

# Objetivo

Criar testes de analyzer claros, pequenos e confiaveis, cobrindo diagnosticos esperados, casos negativos, falsos positivos e configuracoes por `.editorconfig`.

# Quando usar

Use esta skill quando a tarefa envolver:

- testes em `tests/CSF.Analyzers.*.Tests`;
- novos cenarios para uma regra `REL###`, `ARC###` ou `TST###`;
- regressao de bug em analyzer;
- stubs para frameworks externos;
- testes com `.editorconfig`, multiplos arquivos ou `AdditionalFiles`.

# Arquivos relevantes

- `tests/CSF.Analyzers.TestSupport/Verifier.cs`
- `tests/CSF.Analyzers.<Pacote>.Tests/Rules/`
- `src/CSF.Analyzers.<Pacote>/Rules/`
- `docs/rules/<grupo>/<ID>.md`
- `.editorconfig`
- `Directory.Packages.props`

# Regras de teste

- Use `Verifier<TAnalyzer>`.
- Use `Diagnostic(...)` com o ID da regra.
- Use marcadores `{|#0:...|}` quando isso melhorar clareza.
- Teste argumentos do diagnostico com `WithArguments(...)` quando a mensagem usa argumentos.
- Prefira nomes de teste que descrevam comportamento.
- Use stubs minimos quando precisar simular frameworks externos.
- Para regras configuraveis, teste valor ausente, valor valido, valor invalido e escopo por arquivo quando relevante.

# Validacao

```bash
dotnet test ./CSF.Analyzers.slnx --configuration Release -m:1
dotnet test ./CSF.Analyzers.slnx --configuration Release --no-build -m:1
```
