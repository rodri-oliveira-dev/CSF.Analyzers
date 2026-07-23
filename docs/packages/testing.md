# Swa.Analyzers.Testing

## Objetivo

`Swa.Analyzers.Testing` reune regras opt-in para aumentar precisao de testes com NSubstitute e FluentAssertions.

## Publico-alvo

Use em projetos de teste que adotam convencoes explicitas para matchers de mocks e comparacoes de equivalencia.

## Instalacao

```powershell
dotnet add package Swa.Analyzers.Testing
```

## Regras

| ID | Categoria | Severidade | Estado |
| -- | --------- | ---------- | ------ |
| [`TST001`](../rules/testing/TST001.md) | TestQuality | `Info` | Opt-in |
| [`TST002`](../rules/testing/TST002.md) | TestQuality | `Info` | Opt-in |

## Configuracao

```ini
[*.cs]
dotnet_diagnostic.TST001.severity = warning
dotnet_diagnostic.TST002.severity = warning
```

As regras deste pacote nao tem opcoes publicas alem da severidade.

## Limitacoes

As regras so rodam em contexto de teste reconhecido por atributos conhecidos. `TST001` exige referencia a `NSubstitute.Arg` e resolve semanticamente `Arg.Any<T>()`, `ReturnsForAnyArgs`, `WhenForAnyArgs` e `ReceivedWithAnyArgs`; `TST002` confirma que `BeEquivalentTo` vem de namespace `FluentAssertions` e que `Excluding*` vem de `FluentAssertions.Equivalency`.

## Relacao com analyzers externos

Analyzers de teste externos podem complementar o pacote. `TST001` cobre a convencao local de evitar matching amplo em setups e expectativas positivas do NSubstitute, preservando verificacoes negativas deliberadamente permissivas. `TST002` cobre convencoes especificas de equivalencia com FluentAssertions.

## Quando nao usar

Nao instale em projetos de producao. Nao ative se o time aceita `Arg.Any()` ou APIs `*AnyArgs` amplas em setups e expectativas positivas, ou exclusoes em equivalencia como pratica normal.
