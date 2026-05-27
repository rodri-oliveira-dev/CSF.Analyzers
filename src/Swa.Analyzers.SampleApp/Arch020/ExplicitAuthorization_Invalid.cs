using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

namespace Swa.Analyzers.SampleApp.Arch020;

public sealed class OrdersController : ControllerBase
{
    // ARCH020: endpoint HTTP sem decisão explícita de autorização.
    [HttpGet("orders")]
    public void Get()
    {
    }
}

public static class PublicEndpointWithoutDecision
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // ARCH020: declare RequireAuthorization() ou AllowAnonymous().
        app.MapGet("/orders", () => { });
    }
}
