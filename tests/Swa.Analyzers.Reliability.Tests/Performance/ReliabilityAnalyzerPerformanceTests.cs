using Microsoft.CodeAnalysis;

using Swa.Analyzers.Reliability.Rules;

namespace Swa.Analyzers.Tests.Performance;

[Collection(PerformanceTestCollection.Name)]
public sealed class ReliabilityAnalyzerPerformanceTests
{
    private static readonly TimeSpan ConservativeLimit = TimeSpan.FromSeconds(20);

    [Fact]
    public async Task REL003_handles_many_ef_core_query_symbols_within_guardrail()
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
            new Rel003PreferAsNoTrackingForReadOnlyQueriesAnalyzer(),
            sources,
            new Dictionary<string, ReportDiagnostic>
            {
                ["REL003"] = ReportDiagnostic.Warn,
            });

        Assert.Equal(72, result.Diagnostics.Length);
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal("REL003", diagnostic.Id));
        Assert.True(
            result.Elapsed < ConservativeLimit,
            $"REL003 took {result.Elapsed.TotalSeconds:n2}s, above the {ConservativeLimit.TotalSeconds:n0}s guardrail.");
    }

    [Fact]
    public async Task REL005_handles_many_local_concurrency_patterns_within_guardrail()
    {
        var sources = new[]
            {
                ("EfCoreStubs.cs", EfCoreStubs),
            }
            .Concat(AnalyzerPerformanceRunner.CreateNumberedSources(
                "ConcurrentEfQueries",
                36,
                CreateConcurrentEfQuerySource));

        var result = await AnalyzerPerformanceRunner.MeasureAsync(
            new Rel005AvoidConcurrentDbContextOperationsAnalyzer(),
            sources,
            new Dictionary<string, ReportDiagnostic>
            {
                ["REL005"] = ReportDiagnostic.Warn,
            });

        Assert.Equal(36, result.Diagnostics.Length);
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal("REL005", diagnostic.Id));
        Assert.True(
            result.Elapsed < ConservativeLimit,
            $"REL005 took {result.Elapsed.TotalSeconds:n2}s, above the {ConservativeLimit.TotalSeconds:n0}s guardrail.");
    }

    [Fact]
    public async Task REL006_handles_many_hosted_services_within_guardrail()
    {
        var sources = new[]
            {
                ("EfCoreAndHostingStubs.cs", EfCoreAndHostingStubs),
            }
            .Concat(AnalyzerPerformanceRunner.CreateNumberedSources(
                "HostedServices",
                36,
                CreateHostedServiceSource));

        var result = await AnalyzerPerformanceRunner.MeasureAsync(
            new Rel006AvoidScopedDependencyCaptureInHostedServicesAnalyzer(),
            sources,
            new Dictionary<string, ReportDiagnostic>
            {
                ["REL006"] = ReportDiagnostic.Warn,
            });

        Assert.Equal(36, result.Diagnostics.Length);
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal("REL006", diagnostic.Id));
        Assert.True(
            result.Elapsed < ConservativeLimit,
            $"REL006 took {result.Elapsed.TotalSeconds:n2}s, above the {ConservativeLimit.TotalSeconds:n0}s guardrail.");
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

    private static string CreateConcurrentEfQuerySource(int index)
    {
        return $$"""
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Performance.EfCore.ConcurrentQueries{{index}};

public sealed class OrdersDbContext{{index}} : DbContext
{
    public DbSet<Order{{index}}> Orders => throw new System.NotImplementedException();
    public DbSet<Customer{{index}}> Customers => throw new System.NotImplementedException();
}

public sealed class Order{{index}}
{
}

public sealed class Customer{{index}}
{
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
        var firstTask = _db.Orders.ToListAsync();
        var secondTask = _db.Customers.ToListAsync();

        await Task.WhenAll(firstTask, secondTask);
    }
}
""";
    }

    private static string CreateHostedServiceSource(int index)
    {
        return $$"""
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Performance.HostedServices{{index}};

public sealed class OrdersDbContext{{index}} : DbContext
{
}

public sealed class WorkerOptions{{index}}
{
}

public sealed class OrdersWorker{{index}} : BackgroundService
{
    private readonly OrdersDbContext{{index}} _db;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<WorkerOptions{{index}}> _monitor;

    public OrdersWorker{{index}}(
        OrdersDbContext{{index}} db,
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<WorkerOptions{{index}}> monitor)
    {
        _db = db;
        _scopeFactory = scopeFactory;
        _monitor = monitor;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
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

    private const string EfCoreAndHostingStubs = """
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore
{
    public abstract class DbContext
    {
    }

    public interface IDbContextFactory<TContext>
        where TContext : DbContext
    {
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public interface IServiceScopeFactory
    {
    }
}

namespace Microsoft.Extensions.Hosting
{
    public interface IHostedService
    {
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }

    public abstract class BackgroundService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        protected abstract Task ExecuteAsync(CancellationToken stoppingToken);
    }
}

namespace Microsoft.Extensions.Options
{
    public interface IOptions<TOptions>
    {
    }

    public interface IOptionsMonitor<TOptions>
    {
    }

    public interface IOptionsSnapshot<TOptions> : IOptions<TOptions>
    {
    }
}
""";
}
