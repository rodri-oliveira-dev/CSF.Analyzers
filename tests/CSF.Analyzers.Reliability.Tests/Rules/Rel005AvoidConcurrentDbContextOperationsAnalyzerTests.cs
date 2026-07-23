using Microsoft.CodeAnalysis.Testing;

using CSF.Analyzers.Reliability.Rules;

namespace CSF.Analyzers.Tests.Rules;

public sealed class Rel005AvoidConcurrentDbContextOperationsAnalyzerTests
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
        public DbSet<TEntity> Set<TEntity>() where TEntity : class => throw new NotImplementedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    public abstract class DbSet<TEntity> : IQueryable<TEntity>
    {
        public Type ElementType => typeof(TEntity);
        public Expression Expression => throw new NotImplementedException();
        public IQueryProvider Provider => throw new NotImplementedException();
        public IEnumerator<TEntity> GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public interface IDbContextFactory<TContext>
        where TContext : DbContext
    {
        TContext CreateDbContext();
        ValueTask<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default);
    }

    public static class EntityFrameworkQueryableExtensions
    {
        public static Task<List<TEntity>> ToListAsync<TEntity>(this IQueryable<TEntity> source, CancellationToken cancellationToken = default) => Task.FromResult(new List<TEntity>());
        public static Task<TEntity[]> ToArrayAsync<TEntity>(this IQueryable<TEntity> source, CancellationToken cancellationToken = default) => Task.FromResult(Array.Empty<TEntity>());
        public static Task<TEntity> FirstAsync<TEntity>(this IQueryable<TEntity> source, CancellationToken cancellationToken = default) => Task.FromResult(default(TEntity)!);
        public static Task<TEntity?> FirstOrDefaultAsync<TEntity>(this IQueryable<TEntity> source, CancellationToken cancellationToken = default) => Task.FromResult(default(TEntity));
        public static Task<TEntity> SingleAsync<TEntity>(this IQueryable<TEntity> source, CancellationToken cancellationToken = default) => Task.FromResult(default(TEntity)!);
        public static Task<TEntity?> SingleOrDefaultAsync<TEntity>(this IQueryable<TEntity> source, CancellationToken cancellationToken = default) => Task.FromResult(default(TEntity));
        public static Task<bool> AnyAsync<TEntity>(this IQueryable<TEntity> source, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public static Task<bool> AllAsync<TEntity>(this IQueryable<TEntity> source, Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public static Task<int> CountAsync<TEntity>(this IQueryable<TEntity> source, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public static Task<long> LongCountAsync<TEntity>(this IQueryable<TEntity> source, CancellationToken cancellationToken = default) => Task.FromResult(0L);
        public static Task<int> SumAsync<TEntity>(this IQueryable<TEntity> source, Expression<Func<TEntity, int>> selector, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public static Task<double> AverageAsync<TEntity>(this IQueryable<TEntity> source, Expression<Func<TEntity, int>> selector, CancellationToken cancellationToken = default) => Task.FromResult(0d);
        public static Task<int> MinAsync<TEntity>(this IQueryable<TEntity> source, Expression<Func<TEntity, int>> selector, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public static Task<int> MaxAsync<TEntity>(this IQueryable<TEntity> source, Expression<Func<TEntity, int>> selector, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public static Task ForEachAsync<TEntity>(this IQueryable<TEntity> source, Action<TEntity> action, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public static Task LoadAsync<TEntity>(this IQueryable<TEntity> source, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
""";

    [Fact]
    public async Task Reports_TaskWhenAll_inline_operations_on_same_parameter_context()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContext
{
    public DbSet<Customer> Customers => throw new System.NotImplementedException();
    public DbSet<Order> Orders => throw new System.NotImplementedException();
}

public sealed class Customer { }
public sealed class Order { }

public sealed class Handler
{
    public async Task ExecuteAsync(AppDbContext db)
    {
        await Task.{|#0:WhenAll|}(
            db.Customers.ToListAsync(),
            db.Orders.ToListAsync());
    }
}
""";

        await VerifyAsync(source, Expected(0, "db"));
    }

    [Fact]
    public async Task Reports_TaskWhenAll_inline_operations_on_same_field_context()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContext
{
    public DbSet<Customer> Customers => throw new System.NotImplementedException();
    public DbSet<Order> Orders => throw new System.NotImplementedException();
}

public sealed class Customer { }
public sealed class Order { }

public sealed class Handler
{
    private readonly AppDbContext _db;

    public Handler(AppDbContext db)
    {
        _db = db;
    }

    public async Task ExecuteAsync()
    {
        await Task.{|#0:WhenAll|}(
            _db.Customers.ToListAsync(),
            _db.Orders.ToListAsync());
    }
}
""";

        await VerifyAsync(source, Expected(0, "_db"));
    }

    [Fact]
    public async Task Reports_TaskWhenAll_inline_operations_on_same_local_context()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContext
{
    public DbSet<Customer> Customers => throw new System.NotImplementedException();
    public DbSet<Order> Orders => throw new System.NotImplementedException();
}

public sealed class Customer { }
public sealed class Order { }

public sealed class Handler
{
    public async Task ExecuteAsync(AppDbContext source)
    {
        var db = source;

        await Task.{|#0:WhenAll|}(
            db.Customers.ToListAsync(),
            db.Orders.ToListAsync());
    }
}
""";

        await VerifyAsync(source, Expected(0, "db"));
    }

    [Fact]
    public async Task Reports_TaskWhenAll_local_tasks_started_before_await()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContext
{
    public DbSet<Customer> Customers => throw new System.NotImplementedException();
    public DbSet<Order> Orders => throw new System.NotImplementedException();
}

public sealed class Customer { }
public sealed class Order { }

public sealed class Handler
{
    public async Task ExecuteAsync(AppDbContext db)
    {
        var firstTask = db.Customers.ToListAsync();
        var secondTask = db.Orders.ToListAsync();

        await Task.{|#0:WhenAll|}(firstTask, secondTask);
    }
}
""";

        await VerifyAsync(source, Expected(0, "db"));
    }

    [Fact]
    public async Task Reports_TaskWhenAll_array_local_with_tracked_operations()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContext
{
    public DbSet<Customer> Customers => throw new System.NotImplementedException();
    public DbSet<Order> Orders => throw new System.NotImplementedException();
}

public sealed class Customer { }
public sealed class Order { }

public sealed class Handler
{
    public async Task ExecuteAsync(AppDbContext db)
    {
        Task[] tasks =
        {
            db.Customers.ToListAsync(),
            db.Orders.ToListAsync(),
        };

        await Task.{|#0:WhenAll|}(tasks);
    }
}
""";

        await VerifyAsync(source, Expected(0, "db"));
    }

    [Fact]
    public async Task Reports_FullyQualified_TaskWhenAll_and_EF_extensions()
    {
        const string source = """
public sealed class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public Microsoft.EntityFrameworkCore.DbSet<Customer> Customers => throw new System.NotImplementedException();
    public Microsoft.EntityFrameworkCore.DbSet<Order> Orders => throw new System.NotImplementedException();
}

public sealed class Customer { }
public sealed class Order { }

public sealed class Handler
{
    public async System.Threading.Tasks.Task ExecuteAsync(AppDbContext db)
    {
        await System.Threading.Tasks.Task.{|#0:WhenAll|}(
            Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstAsync(db.Customers),
            Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(db.Orders));
    }
}
""";

        await VerifyAsync(source, Expected(0, "db"));
    }

    [Fact]
    public async Task Reports_SaveChangesAsync_concurrently_with_query_on_same_context()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContext
{
    public DbSet<Order> Orders => throw new System.NotImplementedException();
}

public sealed class Order { }

public sealed class Handler
{
    public async Task ExecuteAsync(AppDbContext db)
    {
        await Task.{|#0:WhenAll|}(
            db.Orders.AnyAsync(),
            db.SaveChangesAsync());
    }
}
""";

        await VerifyAsync(source, Expected(0, "db"));
    }

    [Fact]
    public async Task Reports_ParallelForEachAsync_capturing_external_context()
    {
        const string source = """
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContext
{
    public DbSet<Order> Orders => throw new System.NotImplementedException();
}

public sealed class Order
{
    public int CustomerId { get; set; }
}

public sealed class Handler
{
    public async Task ExecuteAsync(AppDbContext db, IEnumerable<int> ids)
    {
        await Parallel.ForEachAsync(ids, async (id, ct) =>
        {
            await db.Orders
                .Where(order => order.CustomerId == id)
                .{|#0:ToListAsync|}(ct);
        });
    }
}
""";

        await VerifyAsync(source, Expected(0, "db"));
    }

    [Fact]
    public async Task Does_not_report_contexts_with_different_roots()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContext
{
    public DbSet<Customer> Customers => throw new System.NotImplementedException();
    public DbSet<Order> Orders => throw new System.NotImplementedException();
}

public sealed class Customer { }
public sealed class Order { }

public sealed class Handler
{
    public async Task ExecuteAsync(AppDbContext firstDb, AppDbContext secondDb)
    {
        await Task.WhenAll(
            firstDb.Customers.ToListAsync(),
            secondDb.Orders.ToListAsync());
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_sequential_operations()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContext
{
    public DbSet<Customer> Customers => throw new System.NotImplementedException();
    public DbSet<Order> Orders => throw new System.NotImplementedException();
}

public sealed class Customer { }
public sealed class Order { }

public sealed class Handler
{
    public async Task ExecuteAsync(AppDbContext db)
    {
        var customers = await db.Customers.ToListAsync();
        var orders = await db.Orders.ToListAsync();
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_context_created_inside_parallel_iteration_from_factory()
    {
        const string source = """
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContext, System.IAsyncDisposable
{
    public DbSet<Order> Orders => throw new System.NotImplementedException();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public sealed class Order
{
    public int CustomerId { get; set; }
}

public sealed class Handler
{
    public async Task ExecuteAsync(IDbContextFactory<AppDbContext> factory, IEnumerable<int> ids)
    {
        await Parallel.ForEachAsync(ids, async (id, ct) =>
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            await db.Orders.Where(order => order.CustomerId == id).ToListAsync(ct);
        });
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_factory_helper_methods_without_local_EF_operations()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContext
{
}

public sealed class Handler
{
    public async Task ExecuteAsync(IDbContextFactory<AppDbContext> factory)
    {
        await Task.WhenAll(
            QueryCustomersAsync(factory),
            QueryOrdersAsync(factory));
    }

    private static Task QueryCustomersAsync(IDbContextFactory<AppDbContext> factory) => Task.CompletedTask;
    private static Task QueryOrdersAsync(IDbContextFactory<AppDbContext> factory) => Task.CompletedTask;
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_custom_methods_with_EF_like_names()
    {
        const string source = """
using System.Collections.Generic;
using System.Threading.Tasks;

public sealed class CustomQuery<T>
{
    public Task<List<T>> ToListAsync() => Task.FromResult(new List<T>());
}

public sealed class Customer { }
public sealed class Order { }

public sealed class Handler
{
    public async Task ExecuteAsync(CustomQuery<Customer> customers, CustomQuery<Order> orders)
    {
        await Task.WhenAll(
            customers.ToListAsync(),
            orders.ToListAsync());
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_EF_Core_reference_is_missing()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class DbContext
{
    public CustomQuery<Customer> Customers { get; } = new();
    public CustomQuery<Order> Orders { get; } = new();
}

public sealed class CustomQuery<T>
{
    public Task<T[]> ToArrayAsync() => Task.FromResult(System.Array.Empty<T>());
}

public sealed class Customer { }
public sealed class Order { }

public sealed class Handler
{
    public async Task ExecuteAsync(DbContext db)
    {
        await Task.WhenAll(
            db.Customers.ToArrayAsync(),
            db.Orders.ToArrayAsync());
    }
}
""";

        await Verifier<Rel005AvoidConcurrentDbContextOperationsAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_aliases_to_same_context_in_first_version()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContext
{
    public DbSet<Customer> Customers => throw new System.NotImplementedException();
    public DbSet<Order> Orders => throw new System.NotImplementedException();
}

public sealed class Customer { }
public sealed class Order { }

public sealed class Handler
{
    public async Task ExecuteAsync(AppDbContext db)
    {
        var first = db;
        var second = db;

        await Task.WhenAll(
            first.Customers.ToListAsync(),
            second.Orders.ToListAsync());
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

public sealed class AppDbContext : DbContext
{
    public DbSet<Customer> Customers => throw new System.NotImplementedException();
    public DbSet<Order> Orders => throw new System.NotImplementedException();
}

public sealed class Customer { }
public sealed class Order { }

public sealed class HandlerTests
{
    private readonly AppDbContext _db = null!;

    [Fact]
    public async Task Reads_seeded_data()
    {
        await Task.WhenAll(
            _db.Customers.ToListAsync(),
            _db.Orders.ToListAsync());
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

    [Fact]
    public async Task Supports_editorconfig_warning_severity()
    {
        const string source = """
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContext
{
    public DbSet<Customer> Customers => throw new System.NotImplementedException();
    public DbSet<Order> Orders => throw new System.NotImplementedException();
}

public sealed class Customer { }
public sealed class Order { }

public sealed class Handler
{
    public async Task ExecuteAsync(AppDbContext db)
    {
        await Task.{|#0:WhenAll|}(
            db.Customers.ToListAsync(),
            db.Orders.ToListAsync());
    }
}
""";

        const string editorConfig = """
root = true

[*]
dotnet_diagnostic.REL005.severity = warning
""";

        await VerifyAsync(source, editorConfig, Expected(0, "db"));
    }

    private static Task VerifyAsync(string source, params DiagnosticResult[] expected)
    {
        return VerifyAsync(source, editorConfig: null, expected);
    }

    private static Task VerifyAsync(string source, string? editorConfig, params DiagnosticResult[] expected)
    {
        return Verifier<Rel005AvoidConcurrentDbContextOperationsAnalyzer>.VerifyAnalyzerAsync(
            (new[]
            {
                ("EfCoreStubs.cs", EfCoreStubs),
                ("Test0.cs", source),
            }),
            editorConfig is null
                ? Array.Empty<(string FileName, string Source)>()
                : new[] { ("/.editorconfig", editorConfig) },
            expected);
    }

    private static DiagnosticResult Expected(int location, string rootName)
    {
        return Verifier<Rel005AvoidConcurrentDbContextOperationsAnalyzer>.Diagnostic("REL005")
            .WithLocation(location)
            .WithArguments(rootName);
    }
}
