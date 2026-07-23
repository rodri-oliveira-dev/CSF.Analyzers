using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

namespace CSF.Analyzers.SampleApp.Rel001;

public sealed class TaskRunInvalidController : ControllerBase
{
    public async Task<int> GetAsync()
    {
        // REL001: evite 'Task.Run' no fluxo de request ASP.NET.
        return await Task.Run(() => 42);
    }

    [HttpPost("orders")]
    public Task<int> Post()
    {
        // REL001: evite 'Task.Run' no fluxo de request ASP.NET.
        return Task.Run(() => 42);
    }
}

public static class TaskRunInvalidMinimalApi
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", () =>
        {
            // REL001: evite 'Task.Run' no fluxo de request ASP.NET.
            _ = Task.Run(() => Save());
        });

        // REL001: evite 'Task.Factory.StartNew' no fluxo de request ASP.NET.
        app.MapGet("/orders", () => Task.Factory.StartNew(() => 42));
    }

    private static void Save()
    {
    }
}
