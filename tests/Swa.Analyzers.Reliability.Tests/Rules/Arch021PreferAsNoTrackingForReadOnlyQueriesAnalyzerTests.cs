using Microsoft.CodeAnalysis.Testing;

using Swa.Analyzers.Reliability.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch021PreferAsNoTrackingForReadOnlyQueriesAnalyzerTests
{
    private const string EfCoreStubs = """
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore
{
    public abstract class DbContext
    {
        public int SaveChanges() => 0;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }

        public void Attach<TEntity>(TEntity entity) { }

        public void Update<TEntity>(TEntity entity) { }

        public void Remove<TEntity>(TEntity entity) { }
    }

    public abstract class DbSet<TEntity> : IQueryable<TEntity>
    {
        public Type ElementType => typeof(TEntity);
        public Expression Expression => throw new NotImplementedException();
        public IQueryProvider Provider => throw new NotImplementedException();
        public IEnumerator<TEntity> GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public enum QueryTrackingBehavior
    {
        TrackAll,
        NoTracking
    }

    public static class EntityFrameworkQueryableExtensions
    {
        public static IQueryable<TEntity> AsNoTracking<TEntity>(this IQueryable<TEntity> source) => source;
        public static IQueryable<TEntity> AsTracking<TEntity>(this IQueryable<TEntity> source) => source;
        public static Task<List<TEntity>> ToListAsync<TEntity>(this IQueryable<TEntity> source, CancellationToken cancellationToken = default) => Task.FromResult(new List<TEntity>());
        public static Task<TEntity?> FirstOrDefaultAsync<TEntity>(this IQueryable<TEntity> source, CancellationToken cancellationToken = default) => Task.FromResult(default(TEntity));
        public static Task<TEntity?> SingleOrDefaultAsync<TEntity>(this IQueryable<TEntity> source, CancellationToken cancellationToken = default) => Task.FromResult(default(TEntity));
    }
}

namespace Microsoft.EntityFrameworkCore.Infrastructure
{
    using Microsoft.EntityFrameworkCore;

    public sealed class DbContextOptionsBuilder
    {
        public DbContextOptionsBuilder UseQueryTrackingBehavior(QueryTrackingBehavior queryTrackingBehavior) => this;
    }
}
""";

    [Fact]
    public async Task Reports_ToListAsync_query_without_AsNoTracking()
    {
        const string source = """
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class OrdersDbContext : DbContext
{
    public DbSet<Order> Orders => throw new System.NotImplementedException();
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

        await VerifyAsync(source, Expected(0, "ToListAsync"));
    }

    [Fact]
    public async Task Reports_FirstOrDefaultAsync_query_without_AsNoTracking()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class OrdersDbContext : DbContext
{
    public DbSet<Order> Orders => throw new System.NotImplementedException();
}

public sealed class Order
{
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
        var order = await _db.Orders.{|#0:FirstOrDefaultAsync|}();
    }
}
""";

        await VerifyAsync(source, Expected(0, "FirstOrDefaultAsync"));
    }

    [Fact]
    public async Task Does_not_report_simple_projection_ToListAsync_query_without_AsNoTracking()
    {
        const string source = """
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class OrdersDbContext : DbContext
{
    public DbSet<Order> Orders => throw new System.NotImplementedException();
}

public sealed class Order
{
    public int Id { get; set; }
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
        var orderIds = await _db.Orders.Select(order => order.Id).ToListAsync();
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Reports_identity_projection_ToListAsync_query_without_AsNoTracking()
    {
        const string source = """
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class OrdersDbContext : DbContext
{
    public DbSet<Order> Orders => throw new System.NotImplementedException();
}

public sealed class Order
{
    public int Id { get; set; }
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
        var orders = await _db.Orders.Select(order => order).{|#0:ToListAsync|}();
    }
}
""";

        await VerifyAsync(source, Expected(0, "ToListAsync"));
    }

    [Fact]
    public async Task Does_not_report_query_with_AsNoTracking()
    {
        const string source = """
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class OrdersDbContext : DbContext
{
    public DbSet<Order> Orders => throw new System.NotImplementedException();
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
        var orders = await _db.Orders.AsNoTracking().Where(order => order.IsOpen).ToListAsync();
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_query_with_AsTracking()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class OrdersDbContext : DbContext
{
    public DbSet<Order> Orders => throw new System.NotImplementedException();
}

public sealed class Order
{
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
        var order = await _db.Orders.AsTracking().FirstOrDefaultAsync();
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_query_when_entity_is_changed_and_persisted()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class OrdersDbContext : DbContext
{
    public DbSet<Order> Orders => throw new System.NotImplementedException();
}

public sealed class Order
{
    public string Status { get; set; } = "";
}

public sealed class OrdersCommand
{
    private readonly OrdersDbContext _db;

    public OrdersCommand(OrdersDbContext db)
    {
        _db = db;
    }

    public async Task ExecuteAsync()
    {
        var order = await _db.Orders.FirstOrDefaultAsync();
        order!.Status = "Processed";
        await _db.SaveChangesAsync();
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_query_when_entity_is_attached_updated_or_removed_and_persisted()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class OrdersDbContext : DbContext
{
    public DbSet<Order> Orders => throw new System.NotImplementedException();
}

public sealed class Order
{
}

public sealed class OrdersCommand
{
    private readonly OrdersDbContext _db;

    public OrdersCommand(OrdersDbContext db)
    {
        _db = db;
    }

    public async Task ExecuteAsync()
    {
        var order = await _db.Orders.FirstOrDefaultAsync();
        _db.Update(order!);
        await _db.SaveChangesAsync();
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_query_when_global_NoTracking_is_configured_in_same_type()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

public sealed class OrdersDbContext : DbContext
{
    public DbSet<Order> Orders => throw new System.NotImplementedException();
}

public sealed class Order
{
}

public sealed class OrdersQuery
{
    private readonly OrdersDbContext _db;

    public OrdersQuery(OrdersDbContext db, DbContextOptionsBuilder options)
    {
        _db = db;
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    public async Task ExecuteAsync()
    {
        var order = await _db.Orders.FirstOrDefaultAsync();
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_non_EF_query_with_same_method_name()
    {
        const string source = """
using System.Collections.Generic;
using System.Threading.Tasks;

public sealed class Order
{
}

public sealed class CustomQuery<T>
{
    public Task<List<T>> ToListAsync()
    {
        return Task.FromResult(new List<T>());
    }
}

public sealed class OrdersQuery
{
    private readonly CustomQuery<Order> _orders = new();

    public async Task ExecuteAsync()
    {
        var orders = await _orders.ToListAsync();
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_inside_tests()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class OrdersDbContext : DbContext
{
    public DbSet<Order> Orders => throw new System.NotImplementedException();
}

public sealed class Order
{
}

public sealed class OrdersQueryTests
{
    private readonly OrdersDbContext _db = null!;

    [Fact]
    public async Task Reads_seeded_orders()
    {
        var order = await _db.Orders.FirstOrDefaultAsync();
    }
}

namespace Xunit
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class FactAttribute : System.Attribute
    {
    }
}
""";

        await VerifyAsync(source);
    }

    private static Task VerifyAsync(string source, params DiagnosticResult[] expected)
    {
        return Verifier<Arch021PreferAsNoTrackingForReadOnlyQueriesAnalyzer>.VerifyAnalyzerAsync(
            (new[]
            {
                ("EfCoreStubs.cs", EfCoreStubs),
                ("Test0.cs", source),
            }),
            Array.Empty<(string FileName, string Source)>(),
            expected);
    }

    private static DiagnosticResult Expected(int location, string methodName)
    {
        return Verifier<Arch021PreferAsNoTrackingForReadOnlyQueriesAnalyzer>.Diagnostic("ARCH021")
            .WithLocation(location)
            .WithArguments(methodName);
    }
}
