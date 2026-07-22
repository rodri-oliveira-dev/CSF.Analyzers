using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

namespace Swa.Analyzers.SampleApp.Arch017;

public sealed class FireAndForgetInvalidController : ControllerBase
{
    public void Post()
    {
        // ARCH017: evite fire-and-forget no fluxo de request ASP.NET.
        _ = SaveAsync();
    }

    private static Task SaveAsync() => Task.CompletedTask;
}

public static class FireAndForgetInvalidMinimalApi
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", () =>
        {
            // ARCH017: evite fire-and-forget no fluxo de request ASP.NET.
            _ = PublishAsync();
        });
    }

    private static Task PublishAsync() => Task.CompletedTask;
}
