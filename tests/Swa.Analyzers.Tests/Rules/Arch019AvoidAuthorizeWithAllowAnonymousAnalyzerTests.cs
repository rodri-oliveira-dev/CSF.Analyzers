using Microsoft.CodeAnalysis.Testing;

using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch019AvoidAuthorizeWithAllowAnonymousAnalyzerTests
{
    [Fact]
    public async Task Reports_method_with_authorize_and_allow_anonymous()
    {
        const string source = """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public sealed class OrdersController : ControllerBase
{
    [Authorize]
    [{|#0:AllowAnonymous|}]
    [HttpGet("orders")]
    public void Get() { }
}
""";

        await VerifyMvcAsync(source, Expected(0, "AllowAnonymous", "Authorize"));
    }

    [Fact]
    public async Task Reports_controller_allow_anonymous_with_action_authorize()
    {
        const string source = """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[AllowAnonymous]
public sealed class OrdersController : ControllerBase
{
    [{|#0:Authorize|}]
    [HttpGet("orders")]
    public void Get() { }
}
""";

        await VerifyMvcAsync(source, Expected(0, "Authorize", "AllowAnonymous"));
    }

    [Fact]
    public async Task Reports_controller_authorize_with_action_allow_anonymous()
    {
        const string source = """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public sealed class OrdersController : ControllerBase
{
    [{|#0:AllowAnonymous|}]
    [HttpGet("orders")]
    public void Public() { }
}
""";

        await VerifyMvcAsync(source, Expected(0, "AllowAnonymous", "Authorize"));
    }

    [Fact]
    public async Task Reports_minimal_api_chain_with_require_authorization_and_allow_anonymous()
    {
        const string source = """
using Microsoft.AspNetCore.Builder;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders", () => { })
            .RequireAuthorization()
            .{|#0:AllowAnonymous|}();
    }
}
""";

        await VerifyMinimalApiAsync(source, Expected(0, "AllowAnonymous", "RequireAuthorization"));
    }

    [Fact]
    public async Task Reports_minimal_api_chain_with_allow_anonymous_and_require_authorization()
    {
        const string source = """
using Microsoft.AspNetCore.Builder;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders", () => { })
            .AllowAnonymous()
            .{|#0:RequireAuthorization|}();
    }
}
""";

        await VerifyMinimalApiAsync(source, Expected(0, "RequireAuthorization", "AllowAnonymous"));
    }

    [Fact]
    public async Task Does_not_report_valid_authorization_metadata()
    {
        const string source = """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public sealed class ProtectedController : ControllerBase
{
    [HttpGet("orders")]
    public void InheritsProtection() { }
}

public sealed class ExplicitProtectedController : ControllerBase
{
    [Authorize]
    [HttpGet("orders/{id}")]
    public void Get() { }
}

public sealed class AnonymousController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("health")]
    public void Health() { }
}
""";

        await VerifyMvcAsync(source);
    }

    [Fact]
    public async Task Does_not_report_custom_attributes_named_like_authorization_attributes()
    {
        const string source = """
using CustomAuthorization;
using Microsoft.AspNetCore.Mvc;

[AllowAnonymous]
public sealed class OrdersController : ControllerBase
{
    [Authorize]
    [HttpGet("orders")]
    public void Get() { }
}

namespace CustomAuthorization
{
    public sealed class AuthorizeAttribute : System.Attribute
    {
    }

    public sealed class AllowAnonymousAttribute : System.Attribute
    {
    }
}
""";

        await VerifyAsync(source + MvcStubs);
    }

    [Fact]
    public async Task Does_not_report_minimal_api_with_single_authorization_metadata()
    {
        const string source = """
using Microsoft.AspNetCore.Builder;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders", () => { }).RequireAuthorization();
        app.MapGet("/health", () => { }).AllowAnonymous();
    }
}
""";

        await VerifyMinimalApiAsync(source);
    }

    [Fact]
    public async Task Does_not_report_custom_minimal_api_methods_with_same_names()
    {
        const string source = """
public sealed class Endpoint
{
    public Endpoint RequireAuthorization() => this;
    public Endpoint AllowAnonymous() => this;
}

public static class Routes
{
    public static void Map(Endpoint endpoint)
    {
        endpoint.RequireAuthorization().AllowAnonymous();
    }
}
""";

        await VerifyAsync(source);
    }

    private static Task VerifyMvcAsync(string source, params DiagnosticResult[] expected)
    {
        return VerifyAsync(source + MvcStubs + AuthorizationStubs, expected);
    }

    private static Task VerifyMinimalApiAsync(string source, params DiagnosticResult[] expected)
    {
        return VerifyAsync(source + MinimalApiStubs, expected);
    }

    private static Task VerifyAsync(string source, params DiagnosticResult[] expected)
    {
        return Verifier<Arch019AvoidAuthorizeWithAllowAnonymousAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    private static DiagnosticResult Expected(int location, string currentMetadata, string conflictingMetadata)
    {
        return Verifier<Arch019AvoidAuthorizeWithAllowAnonymousAnalyzer>.Diagnostic("ARCH019")
            .WithLocation(location)
            .WithArguments(currentMetadata, conflictingMetadata);
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

    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    public class HttpGetAttribute : System.Attribute
    {
        public HttpGetAttribute(string template) { }
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
