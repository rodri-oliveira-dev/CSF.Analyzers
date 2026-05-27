# ARCH018: Evite instanciacao direta de HttpClient

## Objetivo

Detectar criação direta de `System.Net.Http.HttpClient` em código de aplicação.

Criar `HttpClient` diretamente em cada fluxo pode dificultar o controle de lifetime, renovação de DNS e reutilização de conexões. Prefira `IHttpClientFactory`, typed clients ou uma abstração equivalente registrada no container de DI.

## Código não conforme

```csharp
using System.Net.Http;

public sealed class OrdersGateway
{
    public HttpClient Create()
    {
        return new HttpClient();
    }

    public HttpClient CreateWithHandler(HttpMessageHandler handler)
    {
        return new HttpClient(handler);
    }

    public void Send()
    {
        using var client = new HttpClient();
    }
}
```

## Código conforme

```csharp
using System.Net.Http;

public sealed class OrdersGateway
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OrdersGateway(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public HttpClient Create()
    {
        return _httpClientFactory.CreateClient("orders");
    }
}

public sealed class OrdersClient
{
    private readonly HttpClient _httpClient;

    public OrdersClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
}
```

## Heurística

O analyzer reporta expressoes `new` cujo construtor pertence ao símbolo `System.Net.Http.HttpClient`.

Isso inclui:

- `new HttpClient()`;
- `new HttpClient(handler)`;
- declarações `using var client = new HttpClient()`;
- `return new HttpClient()`;
- inicializadores de campos ou propriedades.

Para reduzir falsos positivos, a regra ignora:

- uso de `IHttpClientFactory`;
- typed clients que recebem `HttpClient` por construtor;
- tipos chamados `HttpClient` em outro namespace;
- métodos e classes de teste reconhecidos por atributos comuns de xUnit, NUnit ou MSTest.

## Configuração

Esta regra não expõe opções customizadas de `.editorconfig` na primeira versão.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH018.severity = warning
```

## Limitações conhecidas

- A regra não tenta inferir se uma classe de infraestrutura específica está autorizada a criar `HttpClient`; use configuração de severidade por arquivo quando precisar de uma exceção local.
- A regra não valida se `IHttpClientFactory` ou typed clients foram registrados corretamente no container.
- A regra reporta somente criação direta de `System.Net.Http.HttpClient`; factories customizadas e wrappers não são analisados.

## Impacto esperado

- Menos risco de esgotamento de sockets e problemas de DNS em chamadas HTTP.
- Ciclo de vida de clientes HTTP mais claro e centralizado.
- Incentivo ao uso de `IHttpClientFactory`, typed clients ou abstrações registradas no DI.
