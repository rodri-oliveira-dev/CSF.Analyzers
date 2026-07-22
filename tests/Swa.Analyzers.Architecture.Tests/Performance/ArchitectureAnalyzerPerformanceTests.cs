using Swa.Analyzers.Architecture.Rules;

namespace Swa.Analyzers.Tests.Performance;

[Collection(PerformanceTestCollection.Name)]
public sealed class ArchitectureAnalyzerPerformanceTests
{
    private static readonly TimeSpan ConservativeLimit = TimeSpan.FromSeconds(20);

    [Fact]
    public async Task ARC001_handles_many_asp_net_core_endpoints_within_guardrail()
    {
        var sources = new[]
            {
                ("AspNetCoreStubs.cs", AspNetCoreStubs),
            }
            .Concat(AnalyzerPerformanceRunner.CreateNumberedSources(
                "Controllers",
                24,
                CreateControllerSource))
            .Concat(AnalyzerPerformanceRunner.CreateNumberedSources(
                "MinimalApis",
                24,
                CreateMinimalApiSource));

        var result = await AnalyzerPerformanceRunner.MeasureAsync(
            new Arc001RequireExplicitAuthorizationOnHttpEndpointsAnalyzer(),
            sources);

        Assert.Equal(168, result.Diagnostics.Length);
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal("ARC001", diagnostic.Id));
        Assert.True(
            result.Elapsed < ConservativeLimit,
            $"ARC001 took {result.Elapsed.TotalSeconds:n2}s, above the {ConservativeLimit.TotalSeconds:n0}s guardrail.");
    }

    private static string CreateControllerSource(int index)
    {
        return $$"""
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Performance.AspNet.Controllers{{index}};

[Route("api/orders{{index}}")]
public sealed class Orders{{index}}Controller : ControllerBase
{
    [HttpGet("{id}")]
    public object Get(int id) => new { Id = id };

    [Authorize]
    [HttpGet("secure/{id}")]
    public object GetSecure(int id) => new { Id = id };

    [HttpPost]
    public void Create() { }

    [AllowAnonymous]
    [HttpGet("public")]
    public string Public() => "ok";

    [HttpGet("search")]
    public object Search() => new { Page = 1 };

    [HttpDelete("{id}")]
    public void Delete(int id) { }
}
""";
    }

    private static string CreateMinimalApiSource(int index)
    {
        return $$"""
using Microsoft.AspNetCore.Builder;

namespace Performance.AspNet.Minimal{{index}};

public static class Routes{{index}}
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders/{{index}}", () => "ok");
        app.MapPost("/orders/{{index}}", () => { });
        app.MapPut("/orders/{{index}}/{id}", (int id) => id);
        app.MapGet("/orders/{{index}}/secure", () => "ok").RequireAuthorization();
        app.MapGet("/orders/{{index}}/public", () => "ok").AllowAnonymous();

        var authorized = app.MapGroup("/admin/{{index}}").RequireAuthorization();
        authorized.MapGet("/orders", () => "ok");
    }
}
""";
    }

    private const string AspNetCoreStubs = """
namespace Microsoft.AspNetCore.Authorization
{
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class AuthorizeAttribute : System.Attribute
    {
    }

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AllowAnonymousAttribute : System.Attribute
    {
    }
}

namespace Microsoft.AspNetCore.Mvc
{
    public abstract class ControllerBase
    {
    }

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple = true)]
    public class RouteAttribute : System.Attribute
    {
        public RouteAttribute(string template) { }
    }

    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    public class HttpGetAttribute : System.Attribute
    {
        public HttpGetAttribute() { }
        public HttpGetAttribute(string template) { }
    }

    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    public class HttpPostAttribute : System.Attribute
    {
    }

    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    public class HttpDeleteAttribute : System.Attribute
    {
        public HttpDeleteAttribute(string template) { }
    }
}

namespace Microsoft.AspNetCore.Builder
{
    public interface IEndpointRouteBuilder : Microsoft.AspNetCore.Routing.IEndpointRouteBuilder
    {
    }

    public interface IEndpointConventionBuilder
    {
    }

    public static class EndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapGet(this IEndpointRouteBuilder endpoints, string pattern, System.Func<string> handler) => new EndpointConventionBuilder();
        public static IEndpointConventionBuilder MapGet(this IEndpointRouteBuilder endpoints, string pattern, System.Func<int, int> handler) => new EndpointConventionBuilder();
        public static IEndpointConventionBuilder MapPost(this IEndpointRouteBuilder endpoints, string pattern, System.Action handler) => new EndpointConventionBuilder();
        public static IEndpointConventionBuilder MapPut(this IEndpointRouteBuilder endpoints, string pattern, System.Func<int, int> handler) => new EndpointConventionBuilder();
        public static RouteGroupBuilder MapGroup(this IEndpointRouteBuilder endpoints, string prefix) => new RouteGroupBuilder();

        public static TBuilder RequireAuthorization<TBuilder>(this TBuilder builder)
            where TBuilder : IEndpointConventionBuilder => builder;

        public static TBuilder AllowAnonymous<TBuilder>(this TBuilder builder)
            where TBuilder : IEndpointConventionBuilder => builder;

        public sealed class RouteGroupBuilder : IEndpointRouteBuilder, IEndpointConventionBuilder
        {
        }

        private sealed class EndpointConventionBuilder : IEndpointConventionBuilder
        {
        }
    }
}

namespace Microsoft.AspNetCore.Routing
{
    public interface IEndpointRouteBuilder
    {
    }
}
""";
}
