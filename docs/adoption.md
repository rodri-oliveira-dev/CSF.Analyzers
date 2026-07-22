# Adoção gradual

Este guia ajuda a introduzir os pacotes `Swa.Analyzers.Reliability`, `Swa.Analyzers.Architecture` e `Swa.Analyzers.Testing` sem transformar a primeira execução em uma grande correção obrigatória.

## Princípios

- Instale somente os pacotes que representam políticas reais do projeto.
- Comece com inventário em `info` ou `suggestion` quando houver risco de legado.
- Promova poucas regras por vez para `warning` ou `error`.
- Trate suppressions como exceções documentadas, não como forma padrão de adoção.
- Diferencie prática geral de política organizacional. Regras opt-in existem porque dependem de contexto.

## Fase 1: visibilidade

```ini
[*.cs]
dotnet_diagnostic.REL001.severity = info
dotnet_diagnostic.REL002.severity = info
dotnet_diagnostic.ARC001.severity = info
dotnet_diagnostic.ARC002.severity = info
```

Use esta fase para medir volume, revisar falsos positivos e ajustar opções públicas como namespaces core de `ARC002` ou allowlists de `ARC001`.

## Fase 2: regras maduras como aviso

```ini
[*.cs]
dotnet_diagnostic.REL002.severity = warning
dotnet_diagnostic.ARC001.severity = warning
```

Boas candidatas são regras de baixo ruído e alto impacto operacional. Regras opt-in, como `ARC003`, `ARC004`, `TST001` e `TST002`, devem ser ativadas apenas quando o time aceita aquela política.

## Fase 3: bloqueio no CI

Promova para `error` somente regras já estabilizadas na base:

```ini
[*.cs]
dotnet_diagnostic.ARC001.severity = error

[src/Legacy/**/*.cs]
dotnet_diagnostic.ARC001.severity = info
```

Evite promover todas as regras de uma vez. O melhor bloqueio é incremental, revisável e sustentado por exemplos.

## Suppressions

Use `#pragma warning disable <ID>` para exceções pequenas e próximas do código. Use `GlobalSuppressions.cs` para exceções centralizadas com alvo específico. Use `NoWarn` apenas quando a decisão vale para o projeto inteiro.

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);ARC001</NoWarn>
</PropertyGroup>
```

Ao migrar da v1, suppressions e `NoWarn` com IDs da linha 1.x não afetam os novos diagnósticos `REL###`, `ARC###` e `TST###`. Veja [migração v2](migration-v2.md).

## Perfis prontos

Use [perfis de `.editorconfig`](editorconfig-profiles.md) como ponto de partida:

- `recommended`
- `strict`
- `reliability`
- `architecture`
- `testing`
- `ddd`
- `legacy-safe`
