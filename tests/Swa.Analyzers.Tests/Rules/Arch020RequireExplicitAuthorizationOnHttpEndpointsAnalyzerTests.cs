using Microsoft.CodeAnalysis.Testing;

using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch020RequireExplicitAuthorizationOnHttpEndpointsAnalyzerTests
{
    [Fact]
    public async Task Reports_controller_action_without_authorization_decision()
    {
        const string source = """
using Microsoft.AspNetCore.Mvc;

public sealed class OrdersController : ControllerBase
{
    [{|#0:HttpGet|}("orders")]
    public void Get() { }
}
""";

        await VerifyMvcAsync(source, Expected(0, "OrdersController.Get"));
    }

    [Fact]
    public async Task Does_not_report_action_with_authorize()
    {
        const string source = """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public sealed class OrdersController : ControllerBase
{
    [Authorize]
    [HttpGet("orders")]
    public void Get() { }
}
""";

        await VerifyMvcAsync(source);
    }

    [Fact]
    public async Task Does_not_report_action_with_allow_anonymous()
    {
        const string source = """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public sealed class HealthController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("health")]
    public void Get() { }
}
""";

        await VerifyMvcAsync(source);
    }

    [Fact]
    public async Task Does_not_report_action_when_controller_declares_authorization_decision()
    {
        const string source = """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public sealed class OrdersController : ControllerBase
{
    [HttpGet("orders")]
    public void Get() { }
}

[AllowAnonymous]
public sealed class PublicStatusController : ControllerBase
{
    [HttpGet("status")]
    public void Status() { }
}
""";

        await VerifyMvcAsync(source);
    }

    [Fact]
    public async Task Does_not_report_abstract_controller_base_class()
    {
        const string source = """
using Microsoft.AspNetCore.Mvc;

public abstract class OrdersControllerBase : ControllerBase
{
    [HttpGet("orders")]
    public abstract void Get();
}
""";

        await VerifyMvcAsync(source);
    }

    [Fact]
    public async Task Reports_minimal_api_without_authorization_decision()
    {
        const string source = """
using Microsoft.AspNetCore.Builder;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.{|#0:MapGet|}("/orders", () => { });
    }
}
""";

        await VerifyMinimalApiAsync(source, Expected(0, "MapGet /orders"));
    }

    [Fact]
    public async Task Does_not_report_minimal_api_with_require_authorization_or_allow_anonymous()
    {
        const string source = """
using Microsoft.AspNetCore.Builder;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders", () => { }).RequireAuthorization();
        app.MapPost("/login", () => { }).AllowAnonymous();
    }
}
""";

        await VerifyMinimalApiAsync(source);
    }

    [Fact]
    public async Task Does_not_report_default_technical_routes()
    {
        const string source = """
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

public sealed class HealthController : ControllerBase
{
    [HttpGet("health")]
    public void Get() { }
}

[Route("healthz")]
public sealed class HealthzController : ControllerBase
{
    [HttpGet]
    public void Get() { }
}

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/metrics", () => { });
        app.MapGet("/swagger/v1/swagger.json", () => { });
        app.MapGet("/ready", () => { });
        app.MapGet("/live", () => { });
    }
}
""";

        await VerifyMvcAndMinimalApiAsync(source);
    }

    [Fact]
    public async Task Respects_allowed_routes_configuration()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH020.allowed_routes = ["/internal/status", "/diagnostics/*"]
""";

        const string source = """
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

public sealed class StatusController : ControllerBase
{
    [HttpGet("/internal/status")]
    public void Get() { }
}

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/diagnostics/ping", () => { });
        app.{|#0:MapGet|}("/orders", () => { });
    }
}
""";

        await VerifyMvcAndMinimalApiAsync(source, editorConfig, Expected(0, "MapGet /orders"));
    }

    [Fact]
    public async Task Respects_allowed_methods_configuration()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH020.allowed_methods = ["Ping"]
""";

        const string source = """
using Microsoft.AspNetCore.Mvc;

public sealed class DiagnosticsController : ControllerBase
{
    [HttpGet("diagnostics/ping")]
    public void Ping() { }

    [{|#0:HttpGet|}("orders")]
    public void Get() { }
}
""";

        await VerifyMvcAsync(source, editorConfig, Expected(0, "DiagnosticsController.Get"));
    }

    [Fact]
    public async Task Respects_ignored_namespaces_configuration()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH020.ignored_namespaces = ["Sample.Public"]
""";

        const string source = """
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

namespace Sample.Public
{
    public sealed class StatusController : ControllerBase
    {
        [HttpGet("status")]
        public void Get() { }
    }

    public static class Routes
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("/status", () => { });
        }
    }
}
""";

        await VerifyMvcAndMinimalApiAsync(source, editorConfig);
    }

    [Fact]
    public async Task Ignores_custom_symbols_named_like_asp_net_core()
    {
        const string source = """
using CustomRouting;

public sealed class OrdersController
{
    [HttpGet("orders")]
    public void Get() { }
}

public sealed class CustomBuilder
{
    public void MapGet(string route) { }
}

public static class Routes
{
    public static void Map(CustomBuilder app)
    {
        app.MapGet("/orders");
    }
}

namespace CustomRouting
{
    public sealed class HttpGetAttribute : System.Attribute
    {
        public HttpGetAttribute(string template) { }
    }
}
""";

        await VerifyAsync(source);
    }

    private static Task VerifyMvcAsync(string source, string? editorConfig = null, params DiagnosticResult[] expected)
    {
        return VerifyAsync(source + MvcStubs + AuthorizationStubs, editorConfig, expected);
    }

    private static Task VerifyMvcAsync(string source, params DiagnosticResult[] expected)
    {
        return VerifyMvcAsync(source, editorConfig: null, expected);
    }

    private static Task VerifyMinimalApiAsync(string source, string? editorConfig = null, params DiagnosticResult[] expected)
    {
        return VerifyAsync(source + MinimalApiStubs, editorConfig, expected);
    }

    private static Task VerifyMinimalApiAsync(string source, params DiagnosticResult[] expected)
    {
        return VerifyMinimalApiAsync(source, editorConfig: null, expected);
    }

    private static Task VerifyMvcAndMinimalApiAsync(string source, string? editorConfig = null, params DiagnosticResult[] expected)
    {
        return VerifyAsync(source + MvcStubs + AuthorizationStubs + MinimalApiStubs, editorConfig, expected);
    }

    private static Task VerifyAsync(string source, string? editorConfig = null, params DiagnosticResult[] expected)
    {
        return Verifier<Arch020RequireExplicitAuthorizationOnHttpEndpointsAnalyzer>.VerifyAnalyzerAsync(source, editorConfig, expected);
    }

    private static DiagnosticResult Expected(int location, string endpoint)
    {
        return Verifier<Arch020RequireExplicitAuthorizationOnHttpEndpointsAnalyzer>.Diagnostic("ARCH020")
            .WithLocation(location)
            .WithArguments(endpoint);
    }

    private const string AuthorizationStubs = """

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
""";

    private const string MvcStubs = """

namespace Microsoft.AspNetCore.Mvc
{
    public abstract class ControllerBase
    {
    }

    public abstract class Controller : ControllerBase
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
        public HttpPostAttribute(string template) { }
    }
}
""";

    private const string MinimalApiStubs = """

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
        public static IEndpointConventionBuilder MapGet(this IEndpointRouteBuilder endpoints, string pattern, System.Action handler) => new EndpointConventionBuilder();

        public static IEndpointConventionBuilder MapPost(this IEndpointRouteBuilder endpoints, string pattern, System.Action handler) => new EndpointConventionBuilder();

        public static IEndpointConventionBuilder RequireAuthorization(this IEndpointConventionBuilder builder) => builder;

        public static IEndpointConventionBuilder AllowAnonymous(this IEndpointConventionBuilder builder) => builder;

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
