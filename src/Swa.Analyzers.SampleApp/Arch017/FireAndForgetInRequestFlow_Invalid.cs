using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

namespace Swa.Analyzers.SampleApp.Arch017;

public sealed class FireAndForgetInvalidController : ControllerBase
{
    public void Post()
    {
        // ARCH017: Avoid fire-and-forget in ASP.NET request flow.
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
            // ARCH017: Avoid fire-and-forget in ASP.NET request flow.
            _ = PublishAsync();
        });
    }

    private static Task PublishAsync() => Task.CompletedTask;
}
