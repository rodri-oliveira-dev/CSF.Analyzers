using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Performance;

[Collection(PerformanceTestCollection.Name)]
public sealed class AnalyzerPerformanceTests
{
    private static readonly TimeSpan ConservativeLimit = TimeSpan.FromSeconds(20);

    [Fact]
    public async Task ARCH020_handles_many_asp_net_core_endpoints_within_guardrail()
    {
        var sources = new[]
            {
                ("AspNetCoreStubs.cs", AspNetCoreStubs),
            }
            .Concat(AnalyzerPerformanceRunner.CreateNumberedSources(
                "Controllers",
                24,
                CreateControllerSource))
            .Concat(AnalyzerPerformanceRunner.CreateNumberedSources(
                "MinimalApis",
                24,
                CreateMinimalApiSource));

        var result = await AnalyzerPerformanceRunner.MeasureAsync(
            new Arch020RequireExplicitAuthorizationOnHttpEndpointsAnalyzer(),
            sources);

        Assert.Equal(168, result.Diagnostics.Length);
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal("ARCH020", diagnostic.Id));
        Assert.True(
            result.Elapsed < ConservativeLimit,
            $"ARCH020 took {result.Elapsed.TotalSeconds:n2}s, above the {ConservativeLimit.TotalSeconds:n0}s guardrail.");
    }

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

    private static string CreateControllerSource(int index)
    {
        return $$"""
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Performance.AspNet.Controllers{{index}};

[Route("api/orders{{index}}")]
public sealed class Orders{{index}}Controller : ControllerBase
{
    [HttpGet("{id}")]
    public object Get(int id) => new { Id = id };

    [Authorize]
    [HttpGet("secure/{id}")]
    public object GetSecure(int id) => new { Id = id };

    [HttpPost]
    public void Create() { }

    [AllowAnonymous]
    [HttpGet("public")]
    public string Public() => "ok";

    [HttpGet("search")]
    public object Search() => new { Page = 1 };

    [HttpDelete("{id}")]
    public void Delete(int id) { }
}
""";
    }

    private static string CreateMinimalApiSource(int index)
    {
        return $$"""
using Microsoft.AspNetCore.Builder;

namespace Performance.AspNet.Minimal{{index}};

public static class Routes{{index}}
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders/{{index}}", () => "ok");
        app.MapPost("/orders/{{index}}", () => { });
        app.MapPut("/orders/{{index}}/{id}", (int id) => id);
        app.MapGet("/orders/{{index}}/secure", () => "ok").RequireAuthorization();
        app.MapGet("/orders/{{index}}/public", () => "ok").AllowAnonymous();

        var authorized = app.MapGroup("/admin/{{index}}").RequireAuthorization();
        authorized.MapGet("/orders", () => "ok");
    }
}
""";
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

    private const string AspNetCoreStubs = """
namespace Microsoft.AspNetCore.Authorization
{
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class AuthorizeAttribute : System.Attribute
    {
    }

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AllowAnonymousAttribute : System.Attribute
    {
    }
}

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
    public class HttpGetAttribute : System.Attribute
    {
        public HttpGetAttribute() { }
        public HttpGetAttribute(string template) { }
    }

    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    public class HttpPostAttribute : System.Attribute
    {
        public HttpPostAttribute() { }
        public HttpPostAttribute(string template) { }
    }

    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    public class HttpPutAttribute : System.Attribute
    {
        public HttpPutAttribute(string template) { }
    }

    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    public class HttpDeleteAttribute : System.Attribute
    {
        public HttpDeleteAttribute(string template) { }
    }
}

namespace Microsoft.AspNetCore.Builder
{
    public interface IEndpointRouteBuilder : Microsoft.AspNetCore.Routing.IEndpointRouteBuilder
    {
    }

    public interface IEndpointConventionBuilder
    {
    }

    public static class EndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapGet(this IEndpointRouteBuilder endpoints, string pattern, System.Func<string> handler) => new EndpointConventionBuilder();
        public static IEndpointConventionBuilder MapGet(this IEndpointRouteBuilder endpoints, string pattern, System.Func<int, int> handler) => new EndpointConventionBuilder();
        public static IEndpointConventionBuilder MapPost(this IEndpointRouteBuilder endpoints, string pattern, System.Action handler) => new EndpointConventionBuilder();
        public static IEndpointConventionBuilder MapPut(this IEndpointRouteBuilder endpoints, string pattern, System.Func<int, int> handler) => new EndpointConventionBuilder();
        public static RouteGroupBuilder MapGroup(this IEndpointRouteBuilder endpoints, string prefix) => new RouteGroupBuilder();

        public static TBuilder RequireAuthorization<TBuilder>(this TBuilder builder)
            where TBuilder : IEndpointConventionBuilder => builder;

        public static TBuilder AllowAnonymous<TBuilder>(this TBuilder builder)
            where TBuilder : IEndpointConventionBuilder => builder;

        public sealed class RouteGroupBuilder : IEndpointRouteBuilder, IEndpointConventionBuilder
        {
        }

        private sealed class EndpointConventionBuilder : IEndpointConventionBuilder
        {
        }
    }
}

namespace Microsoft.AspNetCore.Routing
{
    public interface IEndpointRouteBuilder
    {
    }
}
""";

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
