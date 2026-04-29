using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

namespace Swa.Analyzers.SampleApp.Arch016;

public sealed class TaskRunInvalidController : ControllerBase
{
    public async Task<int> GetAsync()
    {
        // ARCH016: Avoid 'Task.Run' in ASP.NET request flow.
        return await Task.Run(() => 42);
    }

    [HttpPost("orders")]
    public Task<int> Post()
    {
        // ARCH016: Avoid 'Task.Run' in ASP.NET request flow.
        return Task.Run(() => 42);
    }
}

public static class TaskRunInvalidMinimalApi
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", () =>
        {
            // ARCH016: Avoid 'Task.Run' in ASP.NET request flow.
            _ = Task.Run(() => Save());
        });

        // ARCH016: Avoid 'Task.Factory.StartNew' in ASP.NET request flow.
        app.MapGet("/orders", () => Task.Factory.StartNew(() => 42));
    }

    private static void Save()
    {
    }
}
