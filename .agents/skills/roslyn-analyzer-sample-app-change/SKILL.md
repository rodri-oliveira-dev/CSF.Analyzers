---
name: roslyn-analyzer-sample-app-change
description: Use esta skill ao criar ou ajustar exemplos manuais nos samples por pacote.
---

# Objetivo

Manter os samples uteis para validacao manual e demonstracao dos analyzers, sem transformar exemplos didaticos em dependencias pesadas.

# Quando usar

Use esta skill quando a tarefa envolver:

- `samples/CSF.Analyzers.*.Sample`;
- exemplos validos ou invalidos de regras `REL###`, `ARC###` ou `TST###`;
- stubs para frameworks externos;
- ajustes de `.editorconfig` dos samples.

# Organizacao

- Exemplos devem ficar em `samples/CSF.Analyzers.<Pacote>.Sample/<Prefixo><Numero>/`.
- Use `*_Invalid.cs` para codigo intencionalmente nao conforme.
- Use `*_Valid.cs` para codigo conforme.
- Use `Stubs/` apenas quando necessario para habilitar reconhecimento simbolico.

# Regras

- Nao adicione pacotes reais quando stubs minimos forem suficientes.
- Nao deixe exemplos invalidos quebrarem a compilacao como erro, salvo quando isso for explicito.
- Nao use samples como suite principal de testes; a fonte de verdade fica em `tests/CSF.Analyzers.*.Tests`.

# Validacao manual

```bash
dotnet build samples/CSF.Analyzers.<Pacote>.Sample/CSF.Analyzers.<Pacote>.Sample.csproj
```
