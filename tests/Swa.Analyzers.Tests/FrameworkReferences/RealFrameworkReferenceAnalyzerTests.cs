using System.Reflection;

using Microsoft.CodeAnalysis.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.FrameworkReferences;

public sealed class RealFrameworkReferenceAnalyzerTests
{
    [Fact]
    public async Task Arch020_reports_real_MVC_action_without_authorization_decision()
    {
        const string source = """
using Microsoft.AspNetCore.Mvc;

public sealed class OrdersController : ControllerBase
{
    [{|#0:HttpGet|}("orders")]
    public void Get()
    {
    }
}
""";

        await VerifyArch020Async(source, Arch020Expected(0, "OrdersController.Get"));
    }

    [Fact]
    public async Task Arch021_reports_real_EF_Core_materializer_without_AsNoTracking()
    {
        const string source = """
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class OrdersDbContext : DbContext
{
    public DbSet<Order> Orders => Set<Order>();
}

public sealed class Order
{
    public bool IsOpen { get; set; }
}

public sealed class OrdersQuery
{
    private readonly OrdersDbContext _db;

    public OrdersQuery(OrdersDbContext db)
    {
        _db = db;
    }

    public async Task ExecuteAsync()
    {
        var orders = await _db.Orders.Where(order => order.IsOpen).{|#0:ToListAsync|}();
    }
}
""";

        await VerifyArch021Async(source, Arch021Expected(0, "ToListAsync"));
    }

    [Fact]
    public async Task Arch024_reports_real_ILogger_exception_overload_interpolated_message()
    {
        const string source = """
using System;
using Microsoft.Extensions.Logging;

public sealed class CustomerService
{
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(ILogger<CustomerService> logger)
    {
        _logger = logger;
    }

    public void Execute(Exception exception, int customerId)
    {
        _logger.LogError(exception, {|#0:$"Customer {customerId} failed"|});
    }
}
""";

        await VerifyArch024Async(source, Arch024Expected(0, "LogError"));
    }

    private static Task VerifyArch020Async(string source, params DiagnosticResult[] expected)
    {
        return RealFrameworkVerifier<Arch020RequireExplicitAuthorizationOnHttpEndpointsAnalyzer>.VerifyAnalyzerAsync(
            source,
            Array.Empty<Assembly>(),
            AspNetCoreReferenceAssemblyPaths,
            expected);
    }

    private static Task VerifyArch021Async(string source, params DiagnosticResult[] expected)
    {
        return RealFrameworkVerifier<Arch021PreferAsNoTrackingForReadOnlyQueriesAnalyzer>.VerifyAnalyzerAsync(
            source,
            EfCoreAssemblies,
            expected);
    }

    private static Task VerifyArch024Async(string source, params DiagnosticResult[] expected)
    {
        return RealFrameworkVerifier<Arch024AvoidInterpolatedStringsInLoggerAnalyzer>.VerifyAnalyzerAsync(
            source,
            LoggingAssemblies,
            expected);
    }

    private static DiagnosticResult Arch020Expected(int location, string endpoint)
    {
        return RealFrameworkVerifier<Arch020RequireExplicitAuthorizationOnHttpEndpointsAnalyzer>.Diagnostic("ARCH020")
            .WithLocation(location)
            .WithArguments(endpoint);
    }

    private static DiagnosticResult Arch021Expected(int location, string methodName)
    {
        return RealFrameworkVerifier<Arch021PreferAsNoTrackingForReadOnlyQueriesAnalyzer>.Diagnostic("ARCH021")
            .WithLocation(location)
            .WithArguments(methodName);
    }

    private static DiagnosticResult Arch024Expected(int location, string methodName)
    {
        return RealFrameworkVerifier<Arch024AvoidInterpolatedStringsInLoggerAnalyzer>.Diagnostic("ARCH024")
            .WithLocation(location)
            .WithArguments(methodName);
    }

    private static readonly IEnumerable<string> AspNetCoreReferenceAssemblyPaths =
        RealFrameworkVerifier<Arch020RequireExplicitAuthorizationOnHttpEndpointsAnalyzer>
            .GetPackageReferenceAssemblyPaths("Microsoft.AspNetCore.App.Ref", "9.0.16", "net9.0");

    private static readonly Assembly[] EfCoreAssemblies =
    {
        typeof(DbContext).Assembly,
        typeof(DbSet<>).Assembly,
        typeof(EntityFrameworkQueryableExtensions).Assembly,
    };

    private static readonly Assembly[] LoggingAssemblies =
    {
        typeof(ILogger).Assembly,
        typeof(ILogger<>).Assembly,
        typeof(LoggerExtensions).Assembly,
    };
}
