using Microsoft.CodeAnalysis.Testing;

using CSF.Analyzers.Reliability.Rules;

namespace CSF.Analyzers.Tests.Rules;

public sealed class Rel001AvoidTaskRunInAspNetRequestFlowAnalyzerTests
{
    #region Invalid scenarios

    [Fact]
    public async Task Reports_await_Task_Run_inside_controller_action()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

public sealed class OrdersController : ControllerBase
{
    public async Task<int> Get()
    {
        return await {|#0:Task.Run|}(() => 42);
    }
}
""";

        await VerifyMvcAsync(source, Expected(0, "Task.Run"));
    }

    [Fact]
    public async Task Reports_return_Task_Run_inside_controller_action()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

public sealed class OrdersController : Controller
{
    public Task<int> Get()
    {
        return {|#0:Task.Run|}(() => 42);
    }
}
""";

        await VerifyMvcAsync(source, Expected(0, "Task.Run"));
    }

    [Fact]
    public async Task Reports_return_await_Task_Run_inside_http_attributed_method()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

public sealed class OrdersEndpoint
{
    [HttpPost("orders")]
    public async Task<int> Post()
    {
        return await {|#0:Task.Run|}(() => 42);
    }
}
""";

        await VerifyMvcAsync(source, Expected(0, "Task.Run"));
    }

    [Fact]
    public async Task Reports_discarded_Task_Run_inside_minimal_api_handler()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", () =>
        {
            _ = {|#0:Task.Run|}(() => Save());
        });
    }

    private static void Save() { }
}
""";

        await VerifyMinimalApiAsync(source, Expected(0, "Task.Run"));
    }

    [Fact]
    public async Task Reports_Task_Factory_StartNew_inside_minimal_api_handler()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders", () => {|#0:Task.Factory.StartNew|}(() => 42));
    }
}
""";

        await VerifyMinimalApiAsync(source, Expected(0, "Task.Factory.StartNew"));
    }

    #endregion

    #region Valid scenarios

    [Fact]
    public async Task Does_not_report_Task_Run_inside_background_service()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

public sealed class Worker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Run(() => DoWork(), stoppingToken);
    }

    private static void DoWork() { }
}
""";

        await VerifyHostingAsync(source);
    }

    [Fact]
    public async Task Does_not_report_Task_Run_inside_hosted_service()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

public sealed class Worker : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => DoWork(), cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static void DoWork() { }
}
""";

        await VerifyHostingAsync(source);
    }

    [Fact]
    public async Task Does_not_report_Task_Run_outside_request_flow()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class ConsoleJob
{
    public Task<int> Execute()
    {
        return Task.Run(() => 42);
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_Task_Run_inside_tests()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xunit;

public sealed class OrdersControllerTests
{
    [Fact]
    public async Task Get_uses_controller()
    {
        await Task.Run(() => { });
    }
}
""";

        await VerifyAsync(source + MvcStubs + XunitStubs);
    }

    [Fact]
    public async Task Does_not_report_custom_Task_type()
    {
        const string source = """
using Microsoft.AspNetCore.Mvc;

public sealed class OrdersController : ControllerBase
{
    public void Get()
    {
        Task.Run();
    }
}

public static class Task
{
    public static void Run() { }
}
""";

        await VerifyMvcAsync(source);
    }

    #endregion

    private static Task VerifyMvcAsync(string source, params DiagnosticResult[] expected)
    {
        return VerifyAsync(source + MvcStubs, expected);
    }

    private static Task VerifyMinimalApiAsync(string source, params DiagnosticResult[] expected)
    {
        return VerifyAsync(source + MinimalApiStubs, expected);
    }

    private static Task VerifyHostingAsync(string source, params DiagnosticResult[] expected)
    {
        return VerifyAsync(source + HostingStubs, expected);
    }

    private static Task VerifyAsync(string source, params DiagnosticResult[] expected)
    {
        return Verifier<Rel001AvoidTaskRunInAspNetRequestFlowAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    private static DiagnosticResult Expected(int location, string usage)
    {
        return Verifier<Rel001AvoidTaskRunInAspNetRequestFlowAnalyzer>.Diagnostic("REL001")
            .WithLocation(location)
            .WithArguments(usage);
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

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple = true)]
    public class RouteAttribute : System.Attribute
    {
        public RouteAttribute(string template) { }
    }

    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HttpGetAttribute : System.Attribute
    {
        public HttpGetAttribute(string template) { }
    }

    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HttpPostAttribute : System.Attribute
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

    public static class EndpointRouteBuilderExtensions
    {
        public static IEndpointRouteBuilder MapGet(this IEndpointRouteBuilder endpoints, string pattern, System.Func<object> handler) => endpoints;
        public static IEndpointRouteBuilder MapPost(this IEndpointRouteBuilder endpoints, string pattern, System.Action handler) => endpoints;
    }
}

namespace Microsoft.AspNetCore.Routing
{
    public interface IEndpointRouteBuilder
    {
    }
}
""";

    private const string HostingStubs = """

namespace Microsoft.Extensions.Hosting
{
    public interface IHostedService
    {
        System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken cancellationToken);
    }

    public abstract class BackgroundService : IHostedService
    {
        public virtual System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken cancellationToken) => System.Threading.Tasks.Task.CompletedTask;
        public virtual System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken cancellationToken) => System.Threading.Tasks.Task.CompletedTask;
        protected abstract System.Threading.Tasks.Task ExecuteAsync(System.Threading.CancellationToken stoppingToken);
    }
}
""";

    private const string XunitStubs = """

namespace Xunit
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class FactAttribute : System.Attribute
    {
    }
}
""";
}
