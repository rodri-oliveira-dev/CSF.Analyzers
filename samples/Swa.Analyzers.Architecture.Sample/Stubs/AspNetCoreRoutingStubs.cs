namespace Microsoft.AspNetCore.Mvc
{
    public abstract class ControllerBase
    {
    }

    public abstract class Controller : ControllerBase
    {
    }

    public interface IActionResult
    {
    }

    public class ActionResult<T>
    {
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class FromServicesAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RouteAttribute : Attribute
    {
        public RouteAttribute(string template)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class HttpGetAttribute : Attribute
    {
        public HttpGetAttribute()
        {
        }

        public HttpGetAttribute(string template)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class HttpPostAttribute : Attribute
    {
        public HttpPostAttribute()
        {
        }

        public HttpPostAttribute(string template)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class HttpPutAttribute : Attribute
    {
        public HttpPutAttribute()
        {
        }

        public HttpPutAttribute(string template)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class HttpPatchAttribute : Attribute
    {
        public HttpPatchAttribute()
        {
        }

        public HttpPatchAttribute(string template)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class HttpDeleteAttribute : Attribute
    {
        public HttpDeleteAttribute()
        {
        }

        public HttpDeleteAttribute(string template)
        {
        }
    }
}

namespace Microsoft.AspNetCore.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class AuthorizeAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AllowAnonymousAttribute : Attribute
    {
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
        public static IEndpointConventionBuilder MapGet(this IEndpointRouteBuilder endpoints, string pattern, Action handler) => new EndpointConventionBuilder();

        public static IEndpointConventionBuilder MapGet<TResponse>(this IEndpointRouteBuilder endpoints, string pattern, Func<TResponse> handler) => new EndpointConventionBuilder();

        public static IEndpointConventionBuilder MapGet<TRequest, TResponse>(this IEndpointRouteBuilder endpoints, string pattern, Func<TRequest, TResponse> handler) => new EndpointConventionBuilder();

        public static IEndpointConventionBuilder MapPost(this IEndpointRouteBuilder endpoints, string pattern, Action handler) => new EndpointConventionBuilder();

        public static IEndpointConventionBuilder MapPost<TRequest, TResponse>(this IEndpointRouteBuilder endpoints, string pattern, Func<TRequest, TResponse> handler) => new EndpointConventionBuilder();

        public static IEndpointConventionBuilder MapPut(this IEndpointRouteBuilder endpoints, string pattern, Action handler) => new EndpointConventionBuilder();

        public static IEndpointConventionBuilder MapPatch(this IEndpointRouteBuilder endpoints, string pattern, Action handler) => new EndpointConventionBuilder();

        public static IEndpointConventionBuilder MapDelete(this IEndpointRouteBuilder endpoints, string pattern, Action handler) => new EndpointConventionBuilder();

        public static IEndpointConventionBuilder MapMethods(this IEndpointRouteBuilder endpoints, string pattern, string[] methods, Action handler) => new EndpointConventionBuilder();

        public static IEndpointConventionBuilder RequireAuthorization(this IEndpointConventionBuilder builder) => builder;

        public static IEndpointConventionBuilder AllowAnonymous(this IEndpointConventionBuilder builder) => builder;

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

namespace Microsoft.AspNetCore.Http.HttpResults
{
    public sealed class Ok
    {
    }

    public sealed class Ok<T>
    {
    }

    public sealed class Created<T>
    {
    }
}

namespace Microsoft.AspNetCore.Http
{
    public static class Results
    {
        public static HttpResults.Ok Ok() => new();
    }

    public static class TypedResults
    {
        public static HttpResults.Ok<T> Ok<T>(T value) => new();
    }
}

namespace Microsoft.AspNetCore.Cors.Infrastructure
{
    public sealed class CorsPolicyBuilder
    {
        public CorsPolicyBuilder AllowAnyOrigin() => this;

        public CorsPolicyBuilder AllowCredentials() => this;

        public CorsPolicyBuilder AllowAnyHeader() => this;

        public CorsPolicyBuilder AllowAnyMethod() => this;

        public CorsPolicyBuilder WithOrigins(params string[] origins) => this;
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
        public virtual Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public virtual Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        protected abstract Task ExecuteAsync(CancellationToken stoppingToken);
    }
}
