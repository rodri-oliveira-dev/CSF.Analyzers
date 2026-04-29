namespace Microsoft.AspNetCore.Mvc
{
    public abstract class ControllerBase
    {
    }

    public abstract class Controller : ControllerBase
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

namespace Microsoft.AspNetCore.Builder
{
    public interface IEndpointRouteBuilder : Microsoft.AspNetCore.Routing.IEndpointRouteBuilder
    {
    }

    public static class EndpointRouteBuilderExtensions
    {
        public static IEndpointRouteBuilder MapGet(this IEndpointRouteBuilder endpoints, string pattern, Action handler) => endpoints;

        public static IEndpointRouteBuilder MapPost(this IEndpointRouteBuilder endpoints, string pattern, Action handler) => endpoints;

        public static IEndpointRouteBuilder MapPut(this IEndpointRouteBuilder endpoints, string pattern, Action handler) => endpoints;

        public static IEndpointRouteBuilder MapPatch(this IEndpointRouteBuilder endpoints, string pattern, Action handler) => endpoints;

        public static IEndpointRouteBuilder MapDelete(this IEndpointRouteBuilder endpoints, string pattern, Action handler) => endpoints;

        public static IEndpointRouteBuilder MapMethods(this IEndpointRouteBuilder endpoints, string pattern, string[] methods, Action handler) => endpoints;
    }
}

namespace Microsoft.AspNetCore.Routing
{
    public interface IEndpointRouteBuilder
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
        public virtual Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public virtual Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        protected abstract Task ExecuteAsync(CancellationToken stoppingToken);
    }
}
