# ARCH016: Evite Task.Run em fluxo de request ASP.NET

## Objetivo

Detectar `Task.Run` e `Task.Factory.StartNew` usados dentro de fluxos de request ASP.NET, como controllers, actions com atributos HTTP e handlers inline de Minimal APIs.

Em aplicações ASP.NET, empacotar trabalho síncrono ou assíncrono em `Task.Run` durante o request normalmente apenas desloca o trabalho para outro thread do ThreadPool. Isso não aumenta a escalabilidade do endpoint e pode esconder a necessidade de usar APIs verdadeiramente assíncronas ou processamento em background.

## Código não conforme

```csharp
public sealed class OrdersController : ControllerBase
{
    public async Task<int> Get()
    {
        return await Task.Run(() => LoadOrder());
    }
}

public sealed class OrdersEndpoint
{
    [HttpPost("orders")]
    public Task<int> Post()
    {
        return Task.Run(() => SaveOrder());
    }
}

app.MapPost("/orders", () =>
{
    _ = Task.Run(() => SaveOrder());
});

app.MapGet("/orders", () => Task.Factory.StartNew(() => LoadOrder()));
```

## Código conforme

```csharp
public sealed class OrdersController : ControllerBase
{
    public async Task<int> Get(CancellationToken cancellationToken)
    {
        return await LoadOrderAsync(cancellationToken);
    }
}

public sealed class OrderWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Run(() => ProcessQueue(), stoppingToken);
    }
}

public sealed class ConsoleJob
{
    public Task<int> Execute()
    {
        return Task.Run(() => 42);
    }
}
```

## Heurística

O analyzer reporta invocações de:

- `System.Threading.Tasks.Task.Run(...)`;
- `System.Threading.Tasks.TaskFactory.StartNew(...)`, incluindo `Task.Factory.StartNew(...)`.

A regra só reporta quando a invocação aparece em um contexto de request ASP.NET reconhecido:

- tipo que herda de `Microsoft.AspNetCore.Mvc.ControllerBase` ou `Controller`;
- método com atributos MVC/Web API como `HttpGet`, `HttpPost`, `HttpPut`, `HttpPatch`, `HttpDelete`, `HttpHead`, `HttpOptions` ou `Route`;
- lambda inline passada diretamente para `MapGet`, `MapPost`, `MapPut`, `MapPatch`, `MapDelete` ou `MapMethods` de Minimal APIs.

Para reduzir falsos positivos, a regra ignora:

- tipos que herdam de `BackgroundService`;
- tipos que implementam `IHostedService`;
- métodos e classes de teste reconhecidos por atributos comuns de xUnit, NUnit ou MSTest;
- código comum fora de fluxo de request;
- tipos próprios que tenham métodos chamados `Task.Run`.

## Limitações conhecidas

- Handlers de Minimal API passados por method group ou variável não são inferidos nesta versão. A regra analisa apenas lambdas inline diretamente associadas à chamada `Map*`.
- A regra não tenta classificar se o trabalho dentro de `Task.Run` é CPU-bound, I/O-bound ou fire-and-forget. Em request ASP.NET, esses usos devem ser revisados explicitamente.
- Código ASP.NET escrito sobre abstrações customizadas pode não ser reconhecido se não usar os símbolos conhecidos de MVC ou Minimal APIs.

## Impacto esperado

- Menos consumo desnecessário de threads por request.
- Endpoints mais claros sobre quando usam I/O assíncrono real e quando precisam de processamento em background.
- Menos fire-and-forget acidental durante o request.
