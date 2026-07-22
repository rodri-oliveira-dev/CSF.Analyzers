using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

namespace Swa.Analyzers.SampleApp.Rel002;

public sealed class FireAndForgetInvalidController : ControllerBase
{
    public void Post()
    {
        // REL002: evite fire-and-forget no fluxo de request ASP.NET.
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
            // REL002: evite fire-and-forget no fluxo de request ASP.NET.
            _ = PublishAsync();
        });
    }

    private static Task PublishAsync() => Task.CompletedTask;
}
