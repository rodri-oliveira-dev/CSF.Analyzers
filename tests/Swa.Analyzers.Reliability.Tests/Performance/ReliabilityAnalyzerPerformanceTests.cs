using Swa.Analyzers.Reliability.Rules;

namespace Swa.Analyzers.Tests.Performance;

[Collection(PerformanceTestCollection.Name)]
public sealed class ReliabilityAnalyzerPerformanceTests
{
    private static readonly TimeSpan ConservativeLimit = TimeSpan.FromSeconds(20);

    [Fact]
    public async Task ARCH021_handles_many_ef_core_query_symbols_within_guardrail()
    {
        var sources = new[]
            {
                ("EfCoreStubs.cs", EfCoreStubs),
            }
            .Concat(AnalyzerPerformanceRunner.CreateNumberedSources(
                "EfQueries",
                36,
                CreateEfQuerySource));

        var result = await AnalyzerPerformanceRunner.MeasureAsync(
            new Arch021PreferAsNoTrackingForReadOnlyQueriesAnalyzer(),
            sources);

        Assert.Equal(72, result.Diagnostics.Length);
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal("ARCH021", diagnostic.Id));
        Assert.True(
            result.Elapsed < ConservativeLimit,
            $"ARCH021 took {result.Elapsed.TotalSeconds:n2}s, above the {ConservativeLimit.TotalSeconds:n0}s guardrail.");
    }

    private static string CreateEfQuerySource(int index)
    {
        return $$"""
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Performance.EfCore.Queries{{index}};

public sealed class OrdersDbContext{{index}} : DbContext
{
    public DbSet<Order{{index}}> Orders => throw new System.NotImplementedException();
    public DbSet<Customer{{index}}> Customers => throw new System.NotImplementedException();
}

public sealed class Order{{index}}
{
    public int Id { get; set; }
    public bool IsOpen { get; set; }
    public string Status { get; set; } = "";
}

public sealed class Customer{{index}}
{
    public int Id { get; set; }
}

public sealed class OrdersQuery{{index}}
{
    private readonly OrdersDbContext{{index}} _db;

    public OrdersQuery{{index}}(OrdersDbContext{{index}} db)
    {
        _db = db;
    }

    public async Task ExecuteAsync()
    {
        var openOrders = await _db.Orders.Where(order => order.IsOpen).ToListAsync();
        var firstOrder = await _db.Orders.FirstOrDefaultAsync();
        var singleOrder = await _db.Orders.Where(order => order.Id == 1).SingleOrDefaultAsync();
        var tracked = await _db.Orders.AsTracking().FirstOrDefaultAsync();
        var noTracking = await _db.Orders.AsNoTracking().ToListAsync();
        var customer = await _db.Customers.FirstOrDefaultAsync();
        var customerIds = await _db.Customers.Select(customer => customer.Id).ToListAsync();
        var projectedOrders = await _db.Orders.Select(order => order).ToListAsync();
    }
}
""";
    }

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
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
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
""";
}
