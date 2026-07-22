using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

namespace Swa.Analyzers.SampleApp.Arc001;

public sealed class OrdersController : ControllerBase
{
    // ARC001: endpoint HTTP sem decisão explícita de autorização.
    [HttpGet("orders")]
    public void Get()
    {
    }
}

public static class PublicEndpointWithoutDecision
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // ARC001: declare RequireAuthorization() ou AllowAnonymous().
        app.MapGet("/orders", () => { });
    }
}
