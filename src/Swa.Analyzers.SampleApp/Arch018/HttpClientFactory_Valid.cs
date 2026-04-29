using System.Net.Http;

namespace Swa.Analyzers.SampleApp.Arch018;

public sealed class HttpClientFactoryValid
{
    private readonly IHttpClientFactoryExample _httpClientFactory;

    public HttpClientFactoryValid(IHttpClientFactoryExample httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public HttpClient Create()
    {
        return _httpClientFactory.CreateClient("orders");
    }
}

public sealed class TypedOrdersClient
{
    private readonly HttpClient _httpClient;

    public TypedOrdersClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
}

public interface IHttpClientFactoryExample
{
    HttpClient CreateClient(string name);
}
