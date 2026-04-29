# ARCH018: Evite instanciacao direta de HttpClient

## Objetivo

Detectar criacao direta de `System.Net.Http.HttpClient` em codigo de aplicacao.

Criar `HttpClient` diretamente em cada fluxo pode dificultar o controle de lifetime, renovacao de DNS e reutilizacao de conexoes. Prefira `IHttpClientFactory`, typed clients ou uma abstracao equivalente registrada no container de DI.

## Codigo nao conforme

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

## Codigo conforme

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

## Heuristica

O analyzer reporta expressoes `new` cujo construtor pertence ao simbolo `System.Net.Http.HttpClient`.

Isso inclui:

- `new HttpClient()`;
- `new HttpClient(handler)`;
- declaracoes `using var client = new HttpClient()`;
- `return new HttpClient()`;
- inicializadores de campos ou propriedades.

Para reduzir falsos positivos, a regra ignora:

- uso de `IHttpClientFactory`;
- typed clients que recebem `HttpClient` por construtor;
- tipos chamados `HttpClient` em outro namespace;
- metodos e classes de teste reconhecidos por atributos comuns de xUnit, NUnit ou MSTest.

## Configuracao

Esta regra nao expoe opcoes customizadas de `.editorconfig` na primeira versao.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH018.severity = warning
```

## Limitacoes conhecidas

- A regra nao tenta inferir se uma classe de infraestrutura especifica esta autorizada a criar `HttpClient`; use configuracao de severidade por arquivo quando precisar de uma excecao local.
- A regra nao valida se `IHttpClientFactory` ou typed clients foram registrados corretamente no container.
- A regra reporta somente criacao direta de `System.Net.Http.HttpClient`; factories customizadas e wrappers nao sao analisados.

## Impacto esperado

- Menos risco de esgotamento de sockets e problemas de DNS em chamadas HTTP.
- Ciclo de vida de clientes HTTP mais claro e centralizado.
- Incentivo ao uso de `IHttpClientFactory`, typed clients ou abstracoes registradas no DI.
