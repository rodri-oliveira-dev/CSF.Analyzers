# ARCH017: Evite fire-and-forget em fluxo de request

## Objetivo

Detectar fire-and-forget explícito dentro de fluxos de request ASP.NET, como controllers, actions com atributos HTTP e handlers inline de Minimal APIs.

Em endpoints, descartar uma `Task` ou `ValueTask` pode fazer o trabalho continuar depois da resposta, perder exceções, ignorar cancelamento do request e esconder dependências de ciclo de vida. Prefira aguardar a operação ou mover o trabalho para uma fila/background worker explícito.

## Código não conforme

```csharp
public sealed class OrdersController : ControllerBase
{
    public void Post()
    {
        _ = SaveAsync();
    }

    private static Task SaveAsync() => Task.CompletedTask;
}

app.MapPost("/orders", () =>
{
    _ = PublishAsync();
});

app.MapPost("/orders", () =>
{
    Task.Run(() => Save());
});
```

## Código conforme

```csharp
public sealed class OrdersController : ControllerBase
{
    public async Task Post()
    {
        await SaveAsync();
    }

    public Task Put()
    {
        return SaveAsync();
    }

    private static Task SaveAsync() => Task.CompletedTask;
}

app.MapPost("/orders", async (IBackgroundQueue queue) =>
{
    await queue.EnqueueAsync();
});

public sealed class OrderWorker : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = ProcessQueueAsync(stoppingToken);
        return Task.CompletedTask;
    }
}
```

## Heurística

O analyzer reporta:

- atribuições para discard (`_ = ...`) cujo valor é uma chamada que retorna `System.Threading.Tasks.Task`, `Task<T>`, `ValueTask` ou `ValueTask<T>`;
- `Task.Run(...)` usado como statement sem `await` ou `return`.

A regra só reporta quando o código aparece em um contexto de request ASP.NET reconhecido:

- tipo que herda de `Microsoft.AspNetCore.Mvc.ControllerBase` ou `Controller`;
- método com atributos MVC/Web API como `HttpGet`, `HttpPost`, `HttpPut`, `HttpPatch`, `HttpDelete`, `HttpHead`, `HttpOptions` ou `Route`;
- lambda inline passada diretamente para `MapGet`, `MapPost`, `MapPut`, `MapPatch`, `MapDelete` ou `MapMethods` de Minimal APIs.

Para reduzir falsos positivos, a regra ignora:

- tipos que herdam de `BackgroundService`;
- tipos que implementam `IHostedService`;
- métodos e classes de teste reconhecidos por atributos comuns de xUnit, NUnit ou MSTest;
- código comum fora de fluxo de request;
- chamadas aguardadas com `await`;
- chamadas retornadas com `return`.

## Configuração

Esta regra não expõe opções customizadas de `.editorconfig` na primeira versão.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH017.severity = warning
```

## Limitações conhecidas

- A regra mira apenas `Task`, `Task<T>`, `ValueTask` e `ValueTask<T>` da BCL. Awaitables customizados não são sinalizados.
- Handlers de Minimal API passados por method group ou variável não são inferidos nesta versão. A regra analisa apenas lambdas inline diretamente associadas à chamada `Map*`.
- A regra não faz análise interprocedural para marcar services de aplicação chamados por endpoints. Ela reporta o fire-and-forget quando o descarte ocorre no fluxo de request reconhecido.
- Enfileiramento explícito só é considerado seguro quando a chamada é aguardada. Um discard para uma fila customizada que retorna `Task` ainda será reportado porque a publicação pode falhar silenciosamente.
- `Task.Run(...)` em fluxo de request também pode ser reportado por [ARCH016](ARCH016.md), que cobre o uso de `Task.Run` nesse contexto mesmo quando ele é aguardado ou retornado.

## Impacto esperado

- Menos trabalho assíncrono invisível durante requests.
- Exceções e cancelamento fluindo pelo pipeline normal de ASP.NET.
- Separação mais clara entre processamento do request e processamento em background.
