using Microsoft.CodeAnalysis.Testing;

using Swa.Analyzers.Reliability.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch022AvoidPrematureQueryMaterializationAnalyzerTests
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
    }

    public abstract class DbSet<TEntity> : IQueryable<TEntity>
    {
        public Type ElementType => typeof(TEntity);
        public Expression Expression => throw new NotImplementedException();
        public IQueryProvider Provider => throw new NotImplementedException();
        public IEnumerator<TEntity> GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public static class EntityFrameworkQueryableExtensions
    {
        public static Task<List<TEntity>> ToListAsync<TEntity>(this IQueryable<TEntity> source, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<TEntity>());
        }
    }
}
""";

    [Fact]
    public async Task Reports_ToList_before_Where()
    {
        const string source = """
using System.Linq;
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

    public void Execute()
    {
        var orders = _db.Orders.{|#0:ToList|}().Where(order => order.IsOpen);
    }
}
""";

        await VerifyAsync(source, Expected(0, "ToList", "Where"));
    }

    [Fact]
    public async Task Reports_ToArray_before_Select()
    {
        const string source = """
using System.Linq;
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

    public void Execute()
    {
        var orderIds = _db.Orders.{|#0:ToArray|}().Select(order => order.Id);
    }
}
""";

        await VerifyAsync(source, Expected(0, "ToArray", "Select"));
    }

    [Fact]
    public async Task Reports_ToList_before_Skip_and_Take()
    {
        const string source = """
using System.Linq;
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

    public void Execute()
    {
        var page = _db.Orders.{|#0:ToList|}().Skip(10).Take(10);
    }
}
""";

        await VerifyAsync(source, Expected(0, "ToList", "Skip"));
    }

    [Fact]
    public async Task Reports_ToListAsync_followed_immediately_by_in_memory_filter()
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
        var orders = await _db.Orders.{|#0:ToListAsync|}();
        var openOrders = orders.Where(order => order.IsOpen);
    }
}
""";

        await VerifyAsync(source, Expected(0, "ToListAsync", "Where"));
    }

    [Fact]
    public async Task Does_not_report_when_Where_runs_before_ToList()
    {
        const string source = """
using System.Linq;
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

    public void Execute()
    {
        var orders = _db.Orders.Where(order => order.IsOpen).ToList();
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_ToList_is_final_materializer()
    {
        const string source = """
using System.Linq;
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

    public void Execute()
    {
        var orders = _db.Orders.ToList();
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_Linq_to_objects()
    {
        const string source = """
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class Order
{
    public bool IsOpen { get; set; }
}

public sealed class OrdersQuery
{
    public void Execute(List<Order> source)
    {
        var orders = source.ToList().Where(order => order.IsOpen);
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_OrderBy_with_custom_comparer()
    {
        const string source = """
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

public sealed class OrdersDbContext : DbContext
{
    public DbSet<Order> Orders => throw new System.NotImplementedException();
}

public sealed class Order
{
    public string Number { get; set; } = "";
}

public sealed class OrdersQuery
{
    private readonly OrdersDbContext _db;

    public OrdersQuery(OrdersDbContext db)
    {
        _db = db;
    }

    public void Execute()
    {
        var orders = _db.Orders.ToList().OrderBy(order => order.Number, StringComparer.OrdinalIgnoreCase);
    }
}
""";

        await VerifyAsync(source);
    }

    private static Task VerifyAsync(string source, params DiagnosticResult[] expected)
    {
        return Verifier<Arch022AvoidPrematureQueryMaterializationAnalyzer>.VerifyAnalyzerAsync(
            (new[]
            {
                ("EfCoreStubs.cs", EfCoreStubs),
                ("Test0.cs", source),
            }),
            Array.Empty<(string FileName, string Source)>(),
            expected);
    }

    private static DiagnosticResult Expected(int location, string materializerName, string nextOperatorName)
    {
        return Verifier<Arch022AvoidPrematureQueryMaterializationAnalyzer>.Diagnostic("ARCH022")
            .WithLocation(location)
            .WithArguments(materializerName, nextOperatorName);
    }
}
