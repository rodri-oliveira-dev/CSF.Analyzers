using System.Reflection;

using Microsoft.CodeAnalysis.Testing;
using Microsoft.EntityFrameworkCore;

using Swa.Analyzers.Reliability.Rules;

namespace Swa.Analyzers.Tests.FrameworkReferences;

public sealed class RealFrameworkReferenceAnalyzerTests
{
    [Fact]
    public async Task Rel003_reports_real_EF_Core_materializer_without_AsNoTracking()
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

        await RealFrameworkVerifier<Rel003PreferAsNoTrackingForReadOnlyQueriesAnalyzer>.VerifyAnalyzerAsync(
            source,
            EfCoreAssemblies,
            Expected(0, "ToListAsync"));
    }

    [Fact]
    public async Task Rel006_reports_real_hosting_EF_Core_and_options_symbols()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

public sealed class OrdersDbContext : DbContext
{
}

public sealed class WorkerOptions
{
}

public sealed class OrdersWorker : BackgroundService
{
    private readonly OrdersDbContext _db;
    private readonly IOptionsSnapshot<WorkerOptions> _options;

    public OrdersWorker(OrdersDbContext {|#0:db|}, IOptionsSnapshot<WorkerOptions> {|#1:options|})
    {
        _db = db;
        _options = options;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}
""";

        await RealFrameworkVerifier<Rel006AvoidScopedDependencyCaptureInHostedServicesAnalyzer>.VerifyAnalyzerAsync(
            source,
            EfCoreAssemblies,
            AspNetCoreAppReferenceAssemblyPaths,
            RealFrameworkVerifier<Rel006AvoidScopedDependencyCaptureInHostedServicesAnalyzer>.Diagnostic("REL006")
                .WithLocation(0)
                .WithArguments("OrdersDbContext", "OrdersWorker"),
            RealFrameworkVerifier<Rel006AvoidScopedDependencyCaptureInHostedServicesAnalyzer>.Diagnostic("REL006")
                .WithLocation(1)
                .WithArguments("Microsoft.Extensions.Options.IOptionsSnapshot<WorkerOptions>", "OrdersWorker"));
    }

    private static DiagnosticResult Expected(int location, string methodName)
    {
        return RealFrameworkVerifier<Rel003PreferAsNoTrackingForReadOnlyQueriesAnalyzer>.Diagnostic("REL003")
            .WithLocation(location)
            .WithArguments(methodName);
    }

    private static readonly Assembly[] EfCoreAssemblies =
    [
        typeof(DbContext).Assembly,
        typeof(DbSet<>).Assembly,
        typeof(EntityFrameworkQueryableExtensions).Assembly,
    ];

    private static readonly string[] AspNetCoreAppReferenceAssemblyPaths = RealFrameworkVerifier<Rel006AvoidScopedDependencyCaptureInHostedServicesAnalyzer>
        .GetPackageReferenceAssemblyPaths("Microsoft.AspNetCore.App.Ref", "9.0.18", "net9.0")
        .ToArray();
}
