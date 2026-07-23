# CSF.Analyzers.Testing

## Objetivo

`CSF.Analyzers.Testing` reúne regras opt-in para aumentar a precisão de testes com NSubstitute e FluentAssertions.

## Público-alvo

Use em projetos de teste que adotam convenções explícitas para matchers de mocks e comparações de equivalência.

## Instalação

Use este comando quando o pacote estiver publicado no NuGet.org ou disponível no feed privado/local configurado no projeto. A publicação no NuGet.org ainda não está habilitada no workflow de release.

```powershell
dotnet add package CSF.Analyzers.Testing
```

Em projetos com Central Package Management, declare a versão em `Directory.Packages.props` e mantenha o `PackageReference` sem `Version`.

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

As regras deste pacote não têm opções públicas além da severidade. Ambas são opt-in porque representam convenções de precisão de teste, não erros universais de NSubstitute ou FluentAssertions.

## Quando instalar

Instale em projetos de teste que usam NSubstitute ou FluentAssertions e querem tratar matchers amplos ou exclusões de equivalência como política revisável.

## Limitações

As regras só rodam em contexto de teste reconhecido por atributos conhecidos. `TST001` exige referência a `NSubstitute.Arg` e resolve semanticamente `Arg.Any<T>()`, `ReturnsForAnyArgs`, `WhenForAnyArgs` e `ReceivedWithAnyArgs`; `TST002` confirma que `BeEquivalentTo` vem de namespace `FluentAssertions` e que `Excluding*` vem de `FluentAssertions.Equivalency`.

## Relação com analyzers externos

Analyzers de teste externos podem complementar o pacote. `TST001` cobre a convenção local de evitar matching amplo em setups e expectativas positivas do NSubstitute, preservando verificações negativas deliberadamente permissivas. `TST002` cobre convenções específicas de equivalência com FluentAssertions.

## Quando não usar

Não instale em projetos de produção. Não ative se o time aceita `Arg.Any()` ou APIs `*AnyArgs` amplas em setups e expectativas positivas, ou exclusões em equivalência como prática normal.
