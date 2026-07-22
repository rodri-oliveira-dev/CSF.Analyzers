using Microsoft.CodeAnalysis.Testing;

using Swa.Analyzers.Architecture.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch015ProhibitVerbsInHttpRoutesAnalyzerTests
{
    private const string EnglishEditorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH015.route_language = en-US
""";

    private const string PortugueseEditorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH015.route_language = pt-BR
""";

    [Fact]
    public async Task Reports_invalid_attribute_routes()
    {
        const string source = """
using Microsoft.AspNetCore.Mvc;

[Route("customers")]
public sealed class CustomersController
{
    [HttpGet({|#0:"get/{id}"|})]
    public void GetById() { }

    [HttpPost({|#1:"create"|})]
    public void Create() { }

    [Route({|#2:"customers/create"|})]
    public void CreateRoute() { }

    [HttpPut({|#3:"orders/{id}/cancel"|})]
    public void CancelOrder() { }
}
""";

        await VerifyMvcAsync(
            source,
            EnglishEditorConfig,
            Expected(0, "get", "get", "en-US"),
            Expected(1, "create", "create", "en-US"),
            Expected(2, "create", "create", "en-US"),
            Expected(3, "cancel", "cancel", "en-US"));
    }

    [Fact]
    public async Task Does_not_report_valid_attribute_routes()
    {
        const string source = """
using Microsoft.AspNetCore.Mvc;

[Route("customers")]
public sealed class CustomersController
{
    [HttpGet("{id}")]
    public void GetById() { }

    [HttpPost("")]
    public void Create() { }

    [HttpPut("orders/{id}")]
    public void UpdateOrder() { }
}
""";

        await VerifyMvcAsync(source, EnglishEditorConfig);
    }

    [Fact]
    public async Task Reports_invalid_real_asp_net_core_mvc_route_attributes()
    {
        const string source = """
using Microsoft.AspNetCore.Mvc;

[Route({|#0:"orders/create"|})]
public sealed class OrdersController
{
    [HttpGet({|#1:"orders/create"|})]
    public void Create() { }
}
""";

        await VerifyMvcAsync(
            source,
            EnglishEditorConfig,
            Expected(0, "create", "create", "en-US"),
            Expected(1, "create", "create", "en-US"));
    }

    [Fact]
    public async Task Does_not_report_custom_route_attributes_named_like_asp_net_core_mvc()
    {
        const string source = """
using CustomRouting;

[Route("orders/create")]
public sealed class OrdersController
{
    [HttpGet("orders/create")]
    public void Create() { }
}

namespace CustomRouting
{
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple = true)]
    public sealed class RouteAttribute : System.Attribute
    {
        public RouteAttribute(string template) { }
    }

    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HttpGetAttribute : System.Attribute
    {
        public HttpGetAttribute(string template) { }
    }
}
""";

        await VerifyAsync(source, EnglishEditorConfig);
    }

    [Fact]
    public async Task Does_not_report_custom_route_attribute_derived_from_custom_attribute()
    {
        const string source = """
using CustomRouting;

public sealed class CommandRouteAttribute : RouteAttribute
{
    public CommandRouteAttribute(string template) : base(template) { }
}

public sealed class OrdersController
{
    [CommandRoute("orders/create")]
    public void Create() { }
}

namespace CustomRouting
{
    public class RouteAttribute : System.Attribute
    {
        public RouteAttribute(string template) { }
    }
}
""";

        await VerifyAsync(source, EnglishEditorConfig);
    }

    [Fact]
    public async Task Does_not_report_custom_attribute_with_similar_http_route_name()
    {
        const string source = """
using CustomRouting;

public sealed class OrdersController
{
    [HttpPostRoute("orders/create")]
    public void Create() { }
}

namespace CustomRouting
{
    public sealed class HttpPostRouteAttribute : System.Attribute
    {
        public HttpPostRouteAttribute(string template) { }
    }
}
""";

        await VerifyAsync(source, EnglishEditorConfig);
    }

    [Fact]
    public async Task Reports_invalid_minimal_api_routes()
    {
        const string source = """
using Microsoft.AspNetCore.Builder;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost({|#0:"/customers/create"|}, () => { });
        app.MapGet({|#1:"/orders/get/{id}"|}, () => { });
        app.MapMethods({|#2:"/orders/recalculate"|}, new[] { "POST" }, () => { });
    }
}
""";

        await VerifyMinimalApiAsync(
            source,
            EnglishEditorConfig,
            Expected(0, "create", "create", "en-US"),
            Expected(1, "get", "get", "en-US"),
            Expected(2, "recalculate", "recalculate", "en-US"));
    }

    [Fact]
    public async Task Does_not_report_valid_minimal_api_routes()
    {
        const string source = """
using Microsoft.AspNetCore.Builder;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/customers/{id}", () => { });
        app.MapPost("/orders", () => { });
        app.MapGet("/orders/{id}/items", () => { });
        app.MapGet("/posts/{id}", () => { });
    }
}
""";

        await VerifyMinimalApiAsync(source, EnglishEditorConfig);
    }

    [Fact]
    public async Task Does_not_report_internal_method_named_map_get()
    {
        const string source = """
public sealed class InternalRouter
{
    public void Map()
    {
        MapGet("/orders/create");
    }

    private static void MapGet(string route) { }
}
""";

        await VerifyAsync(source, EnglishEditorConfig);
    }

    [Fact]
    public async Task Does_not_report_custom_class_method_named_map_post()
    {
        const string source = """
public sealed class CustomRouter
{
    public void MapPost(string route) { }
}

public static class Routes
{
    public static void Map(CustomRouter router)
    {
        router.MapPost("/orders/create");
    }
}
""";

        await VerifyAsync(source, EnglishEditorConfig);
    }

    [Fact]
    public async Task Does_not_report_custom_extension_named_map_delete()
    {
        const string source = """
using CustomRouting;

namespace CustomRouting
{
    public sealed class CustomEndpointRouteBuilder
    {
    }

    public static class CustomEndpointRouteBuilderExtensions
    {
        public static CustomEndpointRouteBuilder MapDelete(this CustomEndpointRouteBuilder endpoints, string pattern, System.Action handler) => endpoints;
    }
}

public static class Routes
{
    public static void Map(CustomEndpointRouteBuilder app)
    {
        app.MapDelete("/orders/delete", () => { });
    }
}
""";

        await VerifyAsync(source, EnglishEditorConfig);
    }

    [Fact]
    public async Task Reports_invalid_route_group_minimal_api_routes()
    {
        const string source = """
using Microsoft.AspNetCore.Builder;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders");
        group.MapGet({|#0:"/create"|}, () => { });
    }
}
""";

        await VerifyMinimalApiAsync(source, EnglishEditorConfig, Expected(0, "create", "create", "en-US"));
    }

    [Fact]
    public async Task Reports_portuguese_verbs_when_language_is_pt_br()
    {
        const string source = """
using Microsoft.AspNetCore.Builder;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost({|#0:"/apolices/emitir"|}, () => { });
    }
}
""";

        await VerifyMinimalApiAsync(
            source,
            PortugueseEditorConfig,
            Expected(0, "emitir", "emitir", "pt-BR"));
    }

    [Fact]
    public async Task Reports_english_verbs_when_language_is_en_us()
    {
        const string source = """
using Microsoft.AspNetCore.Builder;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost({|#0:"/policies/issue"|}, () => { });
    }
}
""";

        await VerifyMinimalApiAsync(
            source,
            EnglishEditorConfig,
            Expected(0, "issue", "issue", "en-US"));
    }

    [Fact]
    public async Task Respects_file_specific_editorconfig_options()
    {
        const string englishEditorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH015.route_language = en-US
""";

        const string portugueseEditorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH015.route_language = pt-BR
dotnet_diagnostic.ARCH015.additional_verbs = ["arquivar"]
""";

        const string englishSource = """
using Microsoft.AspNetCore.Builder;

public static class EnglishRoutes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost({|#0:"/orders/create"|}, () => { });
        app.MapPost("/apolices/emitir", () => { });
    }
}
""";

        const string portugueseSource = """
using Microsoft.AspNetCore.Builder;

public static class PortugueseRoutes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost({|#1:"/apolices/emitir"|}, () => { });
        app.MapPost({|#2:"/apolices/arquivar"|}, () => { });
        app.MapPost("/orders/create", () => { });
    }
}
""";

        await Verifier<Arch015ProhibitVerbsInHttpRoutesAnalyzer>.VerifyAnalyzerAsync(
            [
                ("/en/EnglishRoutes.cs", englishSource),
                ("/pt/PortugueseRoutes.cs", portugueseSource),
                ("MinimalApiStubs.cs", MinimalApiStubs),
            ],
            [
                ("/en/.editorconfig", englishEditorConfig),
                ("/pt/.editorconfig", portugueseEditorConfig),
            ],
            Expected(0, "create", "create", "en-US"),
            Expected(1, "emitir", "emitir", "pt-BR"),
            Expected(2, "arquivar", "arquivar", "pt-BR"));
    }

    [Fact]
    public async Task Falls_back_to_english_when_language_is_missing()
    {
        const string source = """
using Microsoft.AspNetCore.Builder;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost({|#0:"/orders/create"|}, () => { });
        app.MapPost("/apolices/emitir", () => { });
    }
}
""";

        await VerifyMinimalApiAsync(source, editorConfig: null, Expected(0, "create", "create", "en-US"));
    }

    [Fact]
    public async Task Falls_back_to_english_when_language_is_invalid()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH015.route_language = es-ES
""";

        const string source = """
using Microsoft.AspNetCore.Builder;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost({|#0:"/orders/create"|}, () => { });
        app.MapPost("/apolices/emitir", () => { });
    }
}
""";

        await VerifyMinimalApiAsync(source, editorConfig, Expected(0, "create", "create", "en-US"));
    }

    [Fact]
    public async Task Adds_additional_verbs_from_json_array()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH015.route_language = en-US
dotnet_diagnostic.ARCH015.additional_verbs = ["archive", "  publish  ", ""]
""";

        const string source = """
using Microsoft.AspNetCore.Builder;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost({|#0:"/orders/archive"|}, () => { });
        app.MapPost({|#1:"/orders/publish"|}, () => { });
        app.MapPost({|#2:"/orders/create"|}, () => { });
    }
}
""";

        await VerifyMinimalApiAsync(
            source,
            editorConfig,
            Expected(0, "archive", "archive", "en-US"),
            Expected(1, "publish", "publish", "en-US"),
            Expected(2, "create", "create", "en-US"));
    }

    [Fact]
    public async Task Uses_native_verbs_when_additional_verbs_is_absent()
    {
        const string source = """
using Microsoft.AspNetCore.Builder;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost({|#0:"/orders/create"|}, () => { });
        app.MapPost("/orders/archive", () => { });
    }
}
""";

        await VerifyMinimalApiAsync(source, EnglishEditorConfig, Expected(0, "create", "create", "en-US"));
    }

    [Fact]
    public async Task Ignores_malformed_additional_verbs()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH015.route_language = en-US
dotnet_diagnostic.ARCH015.additional_verbs = ["archive",
""";

        const string source = """
using Microsoft.AspNetCore.Builder;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/orders/archive", () => { });
        app.MapPost({|#0:"/orders/create"|}, () => { });
    }
}
""";

        await VerifyMinimalApiAsync(source, editorConfig, Expected(0, "create", "create", "en-US"));
    }

    [Fact]
    public async Task Does_not_report_ambiguous_or_substring_segments()
    {
        const string source = """
using Microsoft.AspNetCore.Builder;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/approval-status/{id}", () => { });
        app.MapGet("/created-at/{id}", () => { });
        app.MapGet("/orderProcessingStatus/{id}", () => { });
        app.MapGet("/getaway/{id}", () => { });
    }
}
""";

        await VerifyMinimalApiAsync(source, EnglishEditorConfig);
    }

    [Fact]
    public async Task Ignores_route_parameters_constraints_tokens_versions_and_query_string()
    {
        const string source = """
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

[Route("api/v1/[controller]")]
public sealed class OrdersController
{
    [HttpGet("{id:int}")]
    public void GetById() { }

    [HttpGet("[action]")]
    public void ActionToken() { }

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders/{*path}", () => { });
        app.MapGet("/orders?operation=create", () => { });
    }
}
""";

        await VerifyMvcAndMinimalApiAsync(source, EnglishEditorConfig);
    }

    [Fact]
    public async Task Reports_kebab_snake_and_camel_case_command_like_segments()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH015.route_language = pt-BR
dotnet_diagnostic.ARCH015.additional_verbs = ["create"]
""";

        const string source = """
using Microsoft.AspNetCore.Builder;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost({|#0:"/orders/create-order"|}, () => { });
        app.MapPost({|#1:"/orders/create_order"|}, () => { });
        app.MapPost({|#2:"/orders/createOrder"|}, () => { });
        app.MapPost({|#3:"/apolices/emitir-apolice"|}, () => { });
    }
}
""";

        await VerifyMinimalApiAsync(
            source,
            editorConfig,
            Expected(0, "create-order", "create", "pt-BR"),
            Expected(1, "create_order", "create", "pt-BR"),
            Expected(2, "createOrder", "create", "pt-BR"),
            Expected(3, "emitir-apolice", "emitir", "pt-BR"));
    }

    [Fact]
    public async Task Reports_derived_route_attributes()
    {
        const string source = """
using Microsoft.AspNetCore.Mvc;

public sealed class CommandRouteAttribute : RouteAttribute
{
    public CommandRouteAttribute(string template) : base(template) { }
}

public sealed class OrdersController
{
    [CommandRoute({|#0:"orders/cancel"|})]
    public void Cancel() { }
}
""";

        await VerifyMvcAsync(source, EnglishEditorConfig, Expected(0, "cancel", "cancel", "en-US"));
    }

    [Fact]
    public async Task Respects_valid_additional_verbs_configuration()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH015.route_language = en-US
dotnet_diagnostic.ARCH015.additional_verbs = ["\u0061rchive"]
""";

        const string source = """
using Microsoft.AspNetCore.Mvc;

public sealed class CustomersController
{
    [HttpPost({|#0:"customers/archive"|})]
    public void Archive() { }
}
""";

        await VerifyMvcAsync(source, editorConfig, Expected(0, "archive", "archive", "en-US"));
    }

    [Fact]
    public async Task Ignores_invalid_additional_verbs_configuration()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH015.route_language = en-US
dotnet_diagnostic.ARCH015.additional_verbs = [archive]
""";

        const string source = """
using Microsoft.AspNetCore.Mvc;

public sealed class CustomersController
{
    [HttpPost("customers/archive")]
    public void Archive() { }
}
""";

        await VerifyMvcAsync(source, editorConfig);
    }

    private static Task VerifyMvcAsync(string source, string? editorConfig, params DiagnosticResult[] expected)
    {
        return VerifyAsync(source + MvcStubs, editorConfig, expected);
    }

    private static Task VerifyMinimalApiAsync(string source, string? editorConfig, params DiagnosticResult[] expected)
    {
        return VerifyAsync(source + MinimalApiStubs, editorConfig, expected);
    }

    private static Task VerifyMvcAndMinimalApiAsync(string source, string? editorConfig, params DiagnosticResult[] expected)
    {
        return VerifyAsync(source + MvcStubs + MinimalApiStubs, editorConfig, expected);
    }

    private static Task VerifyAsync(string source, string? editorConfig, params DiagnosticResult[] expected)
    {
        return Verifier<Arch015ProhibitVerbsInHttpRoutesAnalyzer>.VerifyAnalyzerAsync(source, editorConfig, expected);
    }

    private static DiagnosticResult Expected(int location, string segment, string verb, string language)
    {
        return Verifier<Arch015ProhibitVerbsInHttpRoutesAnalyzer>.Diagnostic("ARCH015")
            .WithLocation(location)
            .WithArguments(segment, verb, language);
    }

    private const string MvcStubs = """

namespace Microsoft.AspNetCore.Mvc
{
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
        public HttpPostAttribute() { }
        public HttpPostAttribute(string template) { }
    }

    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    public class HttpPutAttribute : System.Attribute
    {
        public HttpPutAttribute() { }
        public HttpPutAttribute(string template) { }
    }

    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    public class HttpDeleteAttribute : System.Attribute
    {
        public HttpDeleteAttribute() { }
        public HttpDeleteAttribute(string template) { }
    }

    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    public class HttpPatchAttribute : System.Attribute
    {
        public HttpPatchAttribute() { }
        public HttpPatchAttribute(string template) { }
    }

    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    public class HttpHeadAttribute : System.Attribute
    {
        public HttpHeadAttribute() { }
        public HttpHeadAttribute(string template) { }
    }

    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    public class HttpOptionsAttribute : System.Attribute
    {
        public HttpOptionsAttribute() { }
        public HttpOptionsAttribute(string template) { }
    }
}
""";

    private const string MinimalApiStubs = """

namespace Microsoft.AspNetCore.Builder
{
    public interface IEndpointRouteBuilder : Microsoft.AspNetCore.Routing.IEndpointRouteBuilder
    {
    }

    public sealed class RouteGroupBuilder : IEndpointRouteBuilder
    {
    }

    public static class EndpointRouteBuilderExtensions
    {
        public static IEndpointRouteBuilder MapGet(this IEndpointRouteBuilder endpoints, string pattern, System.Action handler) => endpoints;
        public static IEndpointRouteBuilder MapPost(this IEndpointRouteBuilder endpoints, string pattern, System.Action handler) => endpoints;
        public static IEndpointRouteBuilder MapPut(this IEndpointRouteBuilder endpoints, string pattern, System.Action handler) => endpoints;
        public static IEndpointRouteBuilder MapPatch(this IEndpointRouteBuilder endpoints, string pattern, System.Action handler) => endpoints;
        public static IEndpointRouteBuilder MapDelete(this IEndpointRouteBuilder endpoints, string pattern, System.Action handler) => endpoints;
        public static IEndpointRouteBuilder MapMethods(this IEndpointRouteBuilder endpoints, string pattern, string[] methods, System.Action handler) => endpoints;
        public static RouteGroupBuilder MapGroup(this IEndpointRouteBuilder endpoints, string prefix) => new RouteGroupBuilder();
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
