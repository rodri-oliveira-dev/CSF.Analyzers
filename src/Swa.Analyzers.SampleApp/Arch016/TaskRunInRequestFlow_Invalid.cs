using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

namespace Swa.Analyzers.SampleApp.Arch016;

public sealed class TaskRunInvalidController : ControllerBase
{
    public async Task<int> GetAsync()
    {
        // ARCH016: evite 'Task.Run' no fluxo de request ASP.NET.
        return await Task.Run(() => 42);
    }

    [HttpPost("orders")]
    public Task<int> Post()
    {
        // ARCH016: evite 'Task.Run' no fluxo de request ASP.NET.
        return Task.Run(() => 42);
    }
}

public static class TaskRunInvalidMinimalApi
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", () =>
        {
            // ARCH016: evite 'Task.Run' no fluxo de request ASP.NET.
            _ = Task.Run(() => Save());
        });

        // ARCH016: evite 'Task.Factory.StartNew' no fluxo de request ASP.NET.
        app.MapGet("/orders", () => Task.Factory.StartNew(() => 42));
    }

    private static void Save()
    {
    }
}
