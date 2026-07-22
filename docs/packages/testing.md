# Swa.Analyzers.Testing

## Objetivo

`Swa.Analyzers.Testing` reúne regras opt-in para aumentar precisão de testes com NSubstitute e FluentAssertions.

## Público-alvo

Use em projetos de teste que adotam convenções explícitas para matchers de mocks e comparações de equivalência.

## Instalação

```powershell
dotnet add package Swa.Analyzers.Testing
```

## Regras

| ID | Categoria | Severidade | Estado |
| -- | --------- | ---------- | ------ |
| [`TST001`](../rules/testing/TST001.md) | TestQuality | `Info` | Opt-in |
| [`TST002`](../rules/testing/TST002.md) | TestQuality | `Info` | Opt-in |

## Configuração

```ini
[*.cs]
dotnet_diagnostic.TST001.severity = warning
dotnet_diagnostic.TST002.severity = warning
```

As regras deste pacote não têm opções públicas além da severidade.

## Limitações

As regras só rodam em contexto de teste reconhecido por atributos conhecidos. `TST001` exige referência a `NSubstitute.Arg`; `TST002` confirma que `BeEquivalentTo` vem de namespace `FluentAssertions` e que `Excluding*` vem de `FluentAssertions.Equivalency`.

## Relação com analyzers externos

Analyzers de teste externos podem complementar o pacote. Estas regras cobrem convenções específicas de uso de NSubstitute e FluentAssertions.

## Quando não usar

Não instale em projetos de produção. Não ative se o time aceita `Arg.Any()` amplo ou exclusões em equivalência como prática normal.
