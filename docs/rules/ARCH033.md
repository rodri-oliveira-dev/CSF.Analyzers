# ARCH033: Evite BuildServiceProvider durante registro de serviços

## Objetivo

Detectar chamadas a `BuildServiceProvider()` feitas sobre `Microsoft.Extensions.DependencyInjection.IServiceCollection` durante configuração de dependency injection.

Chamar `BuildServiceProvider()` manualmente enquanto os serviços ainda estão sendo registrados pode criar um provider paralelo ao provider real da aplicação. Isso pode duplicar singletons, resolver serviços em escopos incorretos e produzir comportamento diferente entre startup, testes e runtime.

## Código não conforme

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

## Código conforme

Use factories, overloads de registro que recebem `IServiceProvider`, ou resolva dependências no runtime pelo container principal:

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

## Configuração

A regra aceita uma opção booleana para ignorar contextos de teste reconhecidos por atributos comuns de xUnit, NUnit ou MSTest:

```ini
[*.cs]
dotnet_diagnostic.ARCH033.ignore_tests = true
```

### Fallback das opções

- `ignore_tests`: booleano; default `true`. Valores booleanos aceitam casing variado; valor ausente, vazio ou inválido usa `true`.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH033.severity = warning
```

## Heurística

O analyzer reporta invocações de extensao chamadas `BuildServiceProvider` quando:

- a chamada é feita em sintaxe de membro, como `services.BuildServiceProvider()`;
- o receiver implementa `Microsoft.Extensions.DependencyInjection.IServiceCollection`;
- o método resolvido semanticamente é uma extension method cujo primeiro parâmetro e `IServiceCollection`.

Isso cobre `services.BuildServiceProvider()` e `builder.Services.BuildServiceProvider()`, incluindo overloads com argumentos.

Para reduzir falsos positivos, a regra não reporta:

- métodos de instancia chamados `BuildServiceProvider` em tipos customizados;
- extension methods chamadas `BuildServiceProvider` cujo receiver não é `IServiceCollection`;
- código que recebe `IServiceProvider` legitimamente em runtime;
- namespaces, tipos ou paths de tooling/design-time reconhecidos por segmentos `Tooling` ou `DesignTime`;
- contextos de teste reconhecidos quando `ignore_tests = true`.

## Limitações conhecidas

- A regra não tenta provar que a chamada acontece exclusivamente durante startup; ela foca chamadas feitas sobre `IServiceCollection`, que normalmente representam registro de serviços.
- A detecção de tooling/design-time é baseada em nomes de namespace, tipo ou pasta; use suppressions pontuais para outros utilitários controlados.
- Testes são ignorados apenas quando o analyzer consegue reconhecer atributos xUnit, NUnit ou MSTest disponíveis na compilação.
- Chamadas dinâmicas ou via reflection não são analisadas.

## Impacto esperado

- Evita providers paralelos e singletons duplicados.
- Reduz risco de escopos incorretos durante configuração de DI.
- Mantem a composition root dependente do provider principal criado pelo host da aplicação.
