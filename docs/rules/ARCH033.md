# ARCH033: Evite BuildServiceProvider durante registro de servicos

## Objetivo

Detectar chamadas a `BuildServiceProvider()` feitas sobre `Microsoft.Extensions.DependencyInjection.IServiceCollection` durante configuracao de dependency injection.

Chamar `BuildServiceProvider()` manualmente enquanto os servicos ainda estao sendo registrados pode criar um provider paralelo ao provider real da aplicacao. Isso pode duplicar singletons, resolver servicos em escopos incorretos e produzir comportamento diferente entre startup, testes e runtime.

## Codigo nao conforme

```csharp
using Microsoft.Extensions.DependencyInjection;

public static class ServiceRegistration
{
    public static void Configure(IServiceCollection services)
    {
        services.BuildServiceProvider();
    }
}
```

```csharp
using Microsoft.Extensions.DependencyInjection;

public static class ServiceRegistration
{
    public static void Configure(WebApplicationBuilder builder)
    {
        builder.Services.BuildServiceProvider();
    }
}
```

## Codigo conforme

Use factories, overloads de registro que recebem `IServiceProvider`, ou resolva dependencias no runtime pelo container principal:

```csharp
using Microsoft.Extensions.DependencyInjection;

public static class ServiceRegistration
{
    public static void Configure(IServiceCollection services)
    {
        services.AddSingleton<OrdersCache>();
        services.AddSingleton<OrdersWorker>();
    }
}
```

```csharp
using System;

public sealed class RuntimeHandler
{
    public object? Handle(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetService(typeof(RuntimeHandler));
    }
}
```

## Configuracao

A regra aceita uma opcao booleana para ignorar contextos de teste reconhecidos por atributos comuns de xUnit, NUnit ou MSTest:

```ini
[*.cs]
dotnet_diagnostic.ARCH033.ignore_tests = true
```

### Fallback das opcoes

- `ignore_tests`: booleano; default `true`. Valores booleanos aceitam casing variado; valor ausente, vazio ou invalido usa `true`.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH033.severity = warning
```

## Heuristica

O analyzer reporta invocacoes de extensao chamadas `BuildServiceProvider` quando:

- a chamada e feita em sintaxe de membro, como `services.BuildServiceProvider()`;
- o receiver implementa `Microsoft.Extensions.DependencyInjection.IServiceCollection`;
- o metodo resolvido semanticamente e uma extension method cujo primeiro parametro e `IServiceCollection`.

Isso cobre `services.BuildServiceProvider()` e `builder.Services.BuildServiceProvider()`, incluindo overloads com argumentos.

Para reduzir falsos positivos, a regra nao reporta:

- metodos de instancia chamados `BuildServiceProvider` em tipos customizados;
- extension methods chamadas `BuildServiceProvider` cujo receiver nao e `IServiceCollection`;
- codigo que recebe `IServiceProvider` legitimamente em runtime;
- contextos de teste reconhecidos quando `ignore_tests = true`.

## Limitacoes conhecidas

- A regra nao tenta provar que a chamada acontece exclusivamente durante startup; ela foca chamadas feitas sobre `IServiceCollection`, que normalmente representam registro de servicos.
- Testes sao ignorados apenas quando o analyzer consegue reconhecer atributos xUnit, NUnit ou MSTest disponiveis na compilacao.
- Chamadas dinamicas ou via reflection nao sao analisadas.

## Impacto esperado

- Evita providers paralelos e singletons duplicados.
- Reduz risco de escopos incorretos durante configuracao de DI.
- Mantem a composition root dependente do provider principal criado pelo host da aplicacao.
