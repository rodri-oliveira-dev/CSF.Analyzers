---
name: roslyn-analyzer-sample-app-change
description: Use esta skill ao criar ou ajustar exemplos manuais no Swa.Analyzers.SampleApp.
---

# Objetivo

Manter o SampleApp util para validacao manual e demonstracao dos analyzers, sem transformar exemplos didaticos em dependencias pesadas ou quebrar o build de forma desnecessaria.

# Quando usar

Use esta skill quando a tarefa envolver:

- `src/Swa.Analyzers.SampleApp`
- exemplos validos ou invalidos de regras `ARCH###`
- stubs para frameworks externos
- ajustes em `src/Swa.Analyzers.SampleApp/.editorconfig`
- validacao manual por build do SampleApp

# Organizacao

- Exemplos devem ficar em `src/Swa.Analyzers.SampleApp/Arch###/`.
- Use `*_Invalid.cs` para codigo intencionalmente nao conforme.
- Use `*_Valid.cs` para codigo conforme.
- Use `Stubs/` apenas quando necessario para habilitar reconhecimento simbolico.
- Mantenha exemplos pequenos e diretamente ligados a regra.

# Regras

- Nao adicione pacotes reais quando stubs minimos forem suficientes.
- Nao deixe exemplos invalidos quebrarem a compilacao como erro, salvo quando isso for o objetivo explicito.
- Ajuste `.editorconfig` do SampleApp para reduzir ruido de warnings que nao fazem parte da regra demonstrada.
- Nao use o SampleApp como suite principal de testes. A fonte de verdade para verificacao automatizada deve continuar em `tests/Swa.Analyzers.Tests`.
- Se criar exemplos para nova regra, atualize o README do SampleApp apenas quando a organizacao ou instrucao de uso mudar.

# Validacao manual

Comando recomendado:

```bash
dotnet build src/Swa.Analyzers.SampleApp/Swa.Analyzers.SampleApp.csproj
```

Tambem valide a solucao completa quando a alteracao afetar o analyzer ou configuracao compartilhada.
