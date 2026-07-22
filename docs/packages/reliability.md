# Swa.Analyzers.Reliability

## Objetivo

`Swa.Analyzers.Reliability` reúne regras de confiabilidade e performance operacional para fluxos ASP.NET e consultas EF Core.

## Público-alvo

Use em APIs, workers com endpoints HTTP, serviços ASP.NET Core e projetos que executam consultas EF Core em produção.

## Instalação

```powershell
dotnet add package Swa.Analyzers.Reliability
```

## Regras

| ID | Categoria | Severidade | Estado |
| -- | --------- | ---------- | ------ |
| [`REL001`](../rules/reliability/REL001.md) | Performance | `Warning` | Habilitada |
| [`REL002`](../rules/reliability/REL002.md) | Reliability | `Warning` | Habilitada |
| [`REL003`](../rules/reliability/REL003.md) | Performance | `Info` | Opt-in |
| [`REL004`](../rules/reliability/REL004.md) | Performance | `Warning` | Habilitada |

## Configuração

```ini
[*.cs]
dotnet_diagnostic.REL001.severity = warning
dotnet_diagnostic.REL002.severity = warning
dotnet_diagnostic.REL004.severity = warning

# Opt-in para política EF Core de leitura sem tracking.
dotnet_diagnostic.REL003.severity = info
```

As regras deste pacote não têm opções públicas além da severidade padrão de `.editorconfig`.

## Limitações

As regras usam reconhecimento semântico de tipos ASP.NET, EF Core, `Task` e `ValueTask`. Elas evitam código gerado, testes reconhecidos e hosted services quando aplicável. Projetos sem essas referências podem não emitir diagnósticos.

## Relação com analyzers externos

Analyzers genéricos de async, LINQ e performance podem complementar o pacote. A diferença deste pacote é o recorte contextual: request ASP.NET e consultas EF Core.

## Quando não usar

Não instale em bibliotecas puras sem ASP.NET ou EF Core se o pacote não representa uma política real. Para código que usa tracking intencionalmente em todas as consultas, mantenha `REL003` desabilitada.
