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
}
