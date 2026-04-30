using Microsoft.CodeAnalysis.Testing;

using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch018AvoidDirectHttpClientInstantiationAnalyzerTests
{
    #region Invalid scenarios

    [Fact]
    public async Task Reports_direct_HttpClient_instantiation_inside_method()
    {
        const string source = """
using System.Net.Http;

public sealed class OrdersGateway
{
    public HttpClient Create()
    {
        return {|#0:new HttpClient()|};
    }
}
""";

        await VerifyAsync(source, Expected(0));
    }

    [Fact]
    public async Task Reports_direct_HttpClient_instantiation_with_handler()
    {
        const string source = """
using System.Net.Http;

public sealed class OrdersGateway
{
    public HttpClient Create(HttpMessageHandler handler)
    {
        return {|#0:new HttpClient(handler)|};
    }
}
""";

        await VerifyAsync(source, Expected(0));
    }

    [Fact]
    public async Task Reports_using_var_HttpClient_instantiation()
    {
        const string source = """
using System.Net.Http;

public sealed class OrdersGateway
{
    public void Send()
    {
        using var client = {|#0:new HttpClient()|};
    }
}
""";

        await VerifyAsync(source, Expected(0));
    }

    [Fact]
    public async Task Reports_field_initializer_HttpClient_instantiation()
    {
        const string source = """
using System.Net.Http;

public sealed class OrdersGateway
{
    private readonly HttpClient _client = {|#0:new HttpClient()|};
}
""";

        await VerifyAsync(source, Expected(0));
    }

    [Fact]
    public async Task Reports_property_initializer_HttpClient_instantiation()
    {
        const string source = """
using System.Net.Http;

public sealed class OrdersGateway
{
    public HttpClient Client { get; } = {|#0:new HttpClient()|};
}
""";

        await VerifyAsync(source, Expected(0));
    }

    #endregion

    #region Valid scenarios

    [Fact]
    public async Task Does_not_report_IHttpClientFactory_usage()
    {
        const string source = """
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

public interface IHttpClientFactory
{
    HttpClient CreateClient(string name);
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_typed_client_constructor_injection()
    {
        const string source = """
using System.Net.Http;

public sealed class OrdersClient
{
    private readonly HttpClient _httpClient;

    public OrdersClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_custom_HttpClient_type()
    {
        const string source = """
namespace CustomNetworking
{
    public sealed class HttpClient
    {
    }
}

public sealed class OrdersGateway
{
    public CustomNetworking.HttpClient Create()
    {
        return new CustomNetworking.HttpClient();
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_HttpClient_instantiation_inside_tests()
    {
        const string source = """
using System.Net.Http;
using Xunit;

public sealed class OrdersGatewayTests
{
    [Fact]
    public void Creates_client_for_test_double()
    {
        using var client = new HttpClient();
    }
}

namespace Xunit
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class FactAttribute : System.Attribute
    {
    }
}
""";

        await VerifyAsync(source);
    }

    #endregion

    private static Task VerifyAsync(string source, params DiagnosticResult[] expected)
    {
        return Verifier<Arch018AvoidDirectHttpClientInstantiationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    private static DiagnosticResult Expected(int location)
    {
        return Verifier<Arch018AvoidDirectHttpClientInstantiationAnalyzer>.Diagnostic("ARCH018")
            .WithLocation(location);
    }
}
