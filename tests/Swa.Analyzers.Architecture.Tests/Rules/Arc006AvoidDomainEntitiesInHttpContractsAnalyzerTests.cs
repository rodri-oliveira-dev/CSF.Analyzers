using Microsoft.CodeAnalysis.Testing;

using Swa.Analyzers.Architecture.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arc006AvoidDomainEntitiesInHttpContractsAnalyzerTests
{
    [Fact]
    public async Task Reports_controller_action_entity_parameter()
    {
        const string source = """
using Microsoft.AspNetCore.Mvc;
using MyApp.Domain.Entities;

public sealed class OrdersController : ControllerBase
{
    [HttpPost("orders")]
    public IActionResult Create(Order {|#0:order|}) => null!;
}

namespace MyApp.Domain.Entities
{
    public sealed class Order
    {
    }
}
""";

        await VerifyMvcAsync(source, Expected(0, "OrdersController.Create parameter 'order'", "Order"));
    }

    [Fact]
    public async Task Reports_controller_action_entity_return()
    {
        const string source = """
using Microsoft.AspNetCore.Mvc;
using MyApp.Domain.Entities;

public sealed class OrdersController : ControllerBase
{
    [HttpGet("orders/{id}")]
    public {|#0:Order|} Get() => null!;
}

namespace MyApp.Domain.Entities
{
    public sealed class Order
    {
    }
}
""";

        await VerifyMvcAsync(source, Expected(0, "OrdersController.Get return value", "Order"));
    }

    [Fact]
    public async Task Reports_controller_action_entity_return_in_task()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyApp.Domain.Entities;

public sealed class OrdersController : ControllerBase
{
    [HttpGet("orders/{id}")]
    public {|#0:Task<Order>|} Get() => null!;
}

namespace MyApp.Domain.Entities
{
    public sealed class Order
    {
    }
}
""";

        await VerifyMvcAsync(source, Expected(0, "OrdersController.Get return value", "Order"));
    }

    [Fact]
    public async Task Reports_controller_action_entity_return_in_value_task_action_result()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyApp.Domain.Entities;

public sealed class OrdersController : ControllerBase
{
    [HttpGet("orders/{id}")]
    public {|#0:ValueTask<ActionResult<Order>>|} Get() => default;
}

namespace MyApp.Domain.Entities
{
    public sealed class Order
    {
    }
}
""";

        await VerifyMvcAsync(source, Expected(0, "OrdersController.Get return value", "Order"));
    }

    [Fact]
    public async Task Reports_controller_action_entity_collection_return()
    {
        const string source = """
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using MyApp.Domain.Entities;

public sealed class OrdersController : ControllerBase
{
    [HttpGet("orders")]
    public {|#0:IReadOnlyList<Order>|} List() => null!;
}

namespace MyApp.Domain.Entities
{
    public sealed class Order
    {
    }
}
""";

        await VerifyMvcAsync(source, Expected(0, "OrdersController.List return value", "Order"));
    }

    [Fact]
    public async Task Reports_minimal_api_entity_parameter()
    {
        const string source = """
using Microsoft.AspNetCore.Builder;
using MyApp.Domain.Entities;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", ({|#0:Order order|}) => Results.Ok());
    }
}

namespace MyApp.Domain.Entities
{
    public sealed class Order
    {
    }
}
""";

        await VerifyMinimalApiAsync(source, Expected(0, "MapPost handler parameter 'order'", "Order"));
    }

    [Fact]
    public async Task Reports_minimal_api_entity_return()
    {
        const string source = """
using Microsoft.AspNetCore.Builder;
using MyApp.Domain.Entities;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders/{id}", () => {|#0:new Order()|});
    }
}

namespace MyApp.Domain.Entities
{
    public sealed class Order
    {
    }
}
""";

        await VerifyMinimalApiAsync(source, Expected(0, "MapGet handler return value", "Order"));
    }

    [Fact]
    public async Task Reports_minimal_api_typed_results_entity_return()
    {
        const string source = """
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using MyApp.Domain.Entities;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders/{id}", () => {|#0:TypedResults.Ok(new Order())|});
    }
}

namespace MyApp.Domain.Entities
{
    public sealed class Order
    {
    }
}
""";

        await VerifyMinimalApiAsync(source, Expected(0, "MapGet handler return value", "Order"));
    }

    [Fact]
    public async Task Reports_minimal_api_results_union_entity_return()
    {
        const string source = """
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using MyApp.Domain.Entities;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders/{id}", () => {|#0:Get()|});
    }

    private static Results<Ok<Order>, NotFound> Get() => null!;
}

namespace MyApp.Domain.Entities
{
    public sealed class Order
    {
    }
}
""";

        await VerifyMinimalApiAsync(source, Expected(0, "MapGet handler return value", "Order"));
    }

    [Fact]
    public async Task Does_not_report_dto_contract()
    {
        const string source = """
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

public sealed class OrdersController : ControllerBase
{
    [HttpGet("orders/{id}")]
    public Task<OrderResponse> Get(Guid id) => null!;
}

public sealed record OrderResponse(Guid Id);
""";

        await VerifyMvcAsync(source);
    }

    [Fact]
    public async Task Reports_entity_identified_by_base_class()
    {
        const string source = """
using Microsoft.AspNetCore.Mvc;

public sealed class OrdersController : ControllerBase
{
    [HttpGet("orders/{id}")]
    public {|#0:Order|} Get() => null!;
}

public abstract class Entity
{
}

public sealed class Order : Entity
{
}
""";

        await VerifyMvcAsync(source, Expected(0, "OrdersController.Get return value", "Order"));
    }

    [Fact]
    public async Task Reports_entity_identified_by_interface()
    {
        const string source = """
using Microsoft.AspNetCore.Mvc;

public sealed class OrdersController : ControllerBase
{
    [HttpGet("orders/{id}")]
    public {|#0:Order|} Get() => null!;
}

public interface IEntity
{
}

public sealed class Order : IEntity
{
}
""";

        await VerifyMvcAsync(source, Expected(0, "OrdersController.Get return value", "Order"));
    }

    [Fact]
    public async Task Respects_arc004_entity_namespace_configuration()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARC006.severity = warning
dotnet_diagnostic.ARC004.entity_namespaces = ["MyApp.Model"]
""";

        const string source = """
using Microsoft.AspNetCore.Mvc;
using MyApp.Model;

public sealed class OrdersController : ControllerBase
{
    [HttpGet("orders/{id}")]
    public {|#0:Order|} Get() => null!;
}

namespace MyApp.Model
{
    public sealed class Order
    {
    }
}
""";

        await VerifyMvcAsync(source, editorConfig, Expected(0, "OrdersController.Get return value", "Order"));
    }

    [Fact]
    public async Task Does_not_report_similar_namespace_without_entity_marker_boundary()
    {
        const string source = """
using Microsoft.AspNetCore.Mvc;
using MyApp.Domain.EntitiesLike;

public sealed class OrdersController : ControllerBase
{
    [HttpGet("orders/{id}")]
    public Order Get() => null!;
}

namespace MyApp.Domain.EntitiesLike
{
    public sealed class Order
    {
    }
}
""";

        await VerifyMvcAsync(source);
    }

    [Fact]
    public async Task Does_not_report_framework_types()
    {
        const string source = """
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public sealed class FilesController : ControllerBase
{
    [HttpPost("files")]
    public IActionResult Upload(IFormFile file, HttpContext context, CancellationToken cancellationToken) => null!;
}
""";

        await VerifyMvcAsync(source);
    }

    [Fact]
    public async Task Does_not_report_minimal_api_parameter_from_services()
    {
        const string source = """
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using MyApp.Domain.Entities;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders", ([FromServices] Order service) => Results.Ok());
    }
}

namespace MyApp.Domain.Entities
{
    public sealed class Order
    {
    }
}
""";

        await VerifyMinimalApiAsync(source);
    }

    [Fact]
    public async Task Does_not_report_entity_used_only_inside_action_body()
    {
        const string source = """
using Microsoft.AspNetCore.Mvc;
using MyApp.Domain.Entities;

public sealed class OrdersController : ControllerBase
{
    [HttpGet("orders/{id}")]
    public OrderResponse Get()
    {
        var order = new Order();
        return new OrderResponse();
    }
}

public sealed class OrderResponse
{
}

namespace MyApp.Domain.Entities
{
    public sealed class Order
    {
    }
}
""";

        await VerifyMvcAsync(source);
    }

    [Fact]
    public async Task Does_not_report_entity_type_outside_http_endpoint()
    {
        const string source = """
using MyApp.Domain.Entities;

public sealed class OrderService
{
    public Order Get() => new Order();
}

namespace MyApp.Domain.Entities
{
    public sealed class Order
    {
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_unwrap_unknown_generic_wrappers()
    {
        const string source = """
using Microsoft.AspNetCore.Mvc;
using MyApp.Domain.Entities;

public sealed class OrdersController : ControllerBase
{
    [HttpGet("orders/{id}")]
    public Envelope<Order> Get() => null!;
}

public sealed class Envelope<T>
{
}

namespace MyApp.Domain.Entities
{
    public sealed class Order
    {
    }
}
""";

        await VerifyMvcAsync(source);
    }

    private static Task VerifyMvcAsync(string source, string? editorConfig = null, params DiagnosticResult[] expected)
    {
        return VerifyAsync(source + MvcStubs, editorConfig, expected);
    }

    private static Task VerifyMvcAsync(string source, params DiagnosticResult[] expected)
    {
        return VerifyMvcAsync(source, editorConfig: null, expected);
    }

    private static Task VerifyMinimalApiAsync(string source, string? editorConfig = null, params DiagnosticResult[] expected)
    {
        return VerifyAsync(source + MvcStubs + MinimalApiStubs, editorConfig, expected);
    }

    private static Task VerifyMinimalApiAsync(string source, params DiagnosticResult[] expected)
    {
        return VerifyMinimalApiAsync(source, editorConfig: null, expected);
    }

    private static Task VerifyAsync(string source, string? editorConfig = null, params DiagnosticResult[] expected)
    {
        return Verifier<Arc006AvoidDomainEntitiesInHttpContractsAnalyzer>.VerifyAnalyzerAsync(source, editorConfig, expected);
    }

    private static DiagnosticResult Expected(int location, string contract, string entityType)
    {
        return Verifier<Arc006AvoidDomainEntitiesInHttpContractsAnalyzer>.Diagnostic("ARC006")
            .WithLocation(location)
            .WithArguments(contract, entityType);
    }

    private const string MvcStubs = """

namespace Microsoft.AspNetCore.Mvc
{
    public abstract class ControllerBase
    {
    }

    public abstract class Controller : ControllerBase
    {
    }

    public interface IActionResult
    {
    }

    public class ActionResult<T>
    {
    }

    public sealed class FromServicesAttribute : System.Attribute
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
        public HttpGetAttribute(string template) { }
    }

    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    public class HttpPostAttribute : System.Attribute
    {
        public HttpPostAttribute(string template) { }
    }
}

namespace Microsoft.AspNetCore.Http
{
    public sealed class HttpContext
    {
    }

    public interface IFormFile
    {
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

        public static IEndpointConventionBuilder MapGet<TResponse>(this IEndpointRouteBuilder endpoints, string pattern, System.Func<TResponse> handler) => new EndpointConventionBuilder();

        public static IEndpointConventionBuilder MapGet<TRequest, TResponse>(this IEndpointRouteBuilder endpoints, string pattern, System.Func<TRequest, TResponse> handler) => new EndpointConventionBuilder();

        public static IEndpointConventionBuilder MapPost<TRequest, TResponse>(this IEndpointRouteBuilder endpoints, string pattern, System.Func<TRequest, TResponse> handler) => new EndpointConventionBuilder();

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

public static class Results
{
    public static Microsoft.AspNetCore.Http.HttpResults.Ok Ok() => new Microsoft.AspNetCore.Http.HttpResults.Ok();
}

public static class TypedResults
{
    public static Microsoft.AspNetCore.Http.HttpResults.Ok<T> Ok<T>(T value) => new Microsoft.AspNetCore.Http.HttpResults.Ok<T>();
}

namespace Microsoft.AspNetCore.Http.HttpResults
{
    public sealed class Ok
    {
    }

    public sealed class Ok<T>
    {
    }

    public sealed class Created<T>
    {
    }

    public sealed class Results<T1, T2>
    {
    }

    public sealed class NotFound
    {
    }
}
""";
}
