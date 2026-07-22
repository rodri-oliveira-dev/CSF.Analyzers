using Microsoft.CodeAnalysis.Testing;

using Swa.Analyzers.Reliability.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch017ProhibitFireAndForgetInRequestFlowAnalyzerTests
{
    #region Invalid scenarios

    [Fact]
    public async Task Reports_discarded_Task_inside_controller_action()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

public sealed class OrdersController : ControllerBase
{
    public void Post()
    {
        _ = {|#0:SaveAsync|}();
    }

    private static Task SaveAsync() => Task.CompletedTask;
}
""";

        await VerifyMvcAsync(source, Expected(0, "SaveAsync"));
    }

    [Fact]
    public async Task Reports_discarded_Task_inside_minimal_api_handler()
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
            _ = {|#0:SaveAsync|}();
        });
    }

    private static Task SaveAsync() => Task.CompletedTask;
}
""";

        await VerifyMinimalApiAsync(source, Expected(0, "SaveAsync"));
    }

    [Fact]
    public async Task Reports_unawaited_Task_Run_inside_minimal_api_handler()
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
            {|#0:Task.Run|}(() => Save());
        });
    }

    private static void Save() { }
}
""";

        await VerifyMinimalApiAsync(source, Expected(0, "Task.Run"));
    }

    [Fact]
    public async Task Reports_discarded_ValueTask_inside_http_attributed_method()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

public sealed class OrdersEndpoint
{
    [HttpPost("orders")]
    public void Post()
    {
        _ = {|#0:PublishAsync|}();
    }

    private static ValueTask PublishAsync() => ValueTask.CompletedTask;
}
""";

        await VerifyMvcAsync(source, Expected(0, "PublishAsync"));
    }

    #endregion

    #region Valid scenarios

    [Fact]
    public async Task Does_not_report_awaited_Task_inside_controller_action()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

public sealed class OrdersController : ControllerBase
{
    public async Task Post()
    {
        await SaveAsync();
    }

    private static Task SaveAsync() => Task.CompletedTask;
}
""";

        await VerifyMvcAsync(source);
    }

    [Fact]
    public async Task Does_not_report_returned_Task_inside_controller_action()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

public sealed class OrdersController : ControllerBase
{
    public Task Post()
    {
        return SaveAsync();
    }

    private static Task SaveAsync() => Task.CompletedTask;
}
""";

        await VerifyMvcAsync(source);
    }

    [Fact]
    public async Task Does_not_report_awaited_queue_enqueue_inside_minimal_api_handler()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

public static class Routes
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", async (IBackgroundQueue queue) =>
        {
            await queue.EnqueueAsync();
        });
    }
}

public interface IBackgroundQueue
{
    Task EnqueueAsync();
}
""";

        await VerifyMinimalApiAsync(source);
    }

    [Fact]
    public async Task Does_not_report_discarded_Task_inside_background_service()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

public sealed class Worker : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = SaveAsync();
        return Task.CompletedTask;
    }

    private static Task SaveAsync() => Task.CompletedTask;
}
""";

        await VerifyHostingAsync(source);
    }

    [Fact]
    public async Task Does_not_report_unawaited_Task_Run_inside_hosted_service()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

public sealed class Worker : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Run(() => Save());
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static void Save() { }
}
""";

        await VerifyHostingAsync(source);
    }

    [Fact]
    public async Task Does_not_report_discarded_Task_inside_tests()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xunit;

public sealed class OrdersControllerTests
{
    [Fact]
    public void Post_starts_work()
    {
        _ = SaveAsync();
    }

    private static Task SaveAsync() => Task.CompletedTask;
}
""";

        await VerifyAsync(source + MvcStubs + XunitStubs);
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
        return Verifier<Arch017ProhibitFireAndForgetInRequestFlowAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    private static DiagnosticResult Expected(int location, string usage)
    {
        return Verifier<Arch017ProhibitFireAndForgetInRequestFlowAnalyzer>.Diagnostic("ARCH017")
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
        public static IEndpointRouteBuilder MapPost(this IEndpointRouteBuilder endpoints, string pattern, System.Action handler) => endpoints;
        public static IEndpointRouteBuilder MapPost<T1>(this IEndpointRouteBuilder endpoints, string pattern, System.Func<T1, System.Threading.Tasks.Task> handler) => endpoints;
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
