---
name: roslyn-analyzer-sample-app-change
description: Use esta skill ao criar ou ajustar exemplos manuais no Swa.Analyzers.SampleApp.
---

# Objetivo

Manter o SampleApp útil para validação manual e demonstração dos analyzers, sem transformar exemplos didáticos em dependências pesadas ou quebrar o build de forma desnecessária.

# Quando usar

Use esta skill quando a tarefa envolver:

- `src/Swa.Analyzers.SampleApp`
- exemplos válidos ou inválidos de regras `ARCH###`
- stubs para frameworks externos
- ajustes em `src/Swa.Analyzers.SampleApp/.editorconfig`
- validação manual por build do SampleApp

# Organizacao

- Exemplos devem ficar em `src/Swa.Analyzers.SampleApp/Arch###/`.
- Use `*_Invalid.cs` para código intencionalmente não conforme.
- Use `*_Valid.cs` para código conforme.
- Use `Stubs/` apenas quando necessário para habilitar reconhecimento simbolico.
- Mantenha exemplos pequenos e diretamente ligados a regra.

# Regras

- Não adicione pacotes reais quando stubs mínimos forem suficientes.
- Não deixe exemplos inválidos quebrarem a compilação como erro, salvo quando isso for o objetivo explícito.
- Ajuste `.editorconfig` do SampleApp para reduzir ruído de warnings que não fazem parte da regra demonstrada.
- Não use o SampleApp como suíte principal de testes. A fonte de verdade para verificação automatizada deve continuar em `tests/Swa.Analyzers.Tests`.
- Se criar exemplos para nova regra, atualize o README do SampleApp apenas quando a organização ou instrução de uso mudar.

# Validacao manual

Comando recomendado:

```bash
dotnet build src/Swa.Analyzers.SampleApp/Swa.Analyzers.SampleApp.csproj
```

Também valide a solução completa quando a alteração afetar o analyzer ou configuração compartilhada.
