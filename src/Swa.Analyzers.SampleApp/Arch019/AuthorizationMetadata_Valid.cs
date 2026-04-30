using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

namespace Swa.Analyzers.SampleApp.Arch019;

[Authorize]
public sealed class ProtectedOrdersController : ControllerBase
{
    [HttpGet("orders")]
    public void Get()
    {
    }
}

public sealed class PublicStatusController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("status")]
    public void Status()
    {
    }
}

public static class AuthorizationMinimalApi
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders", () => { })
            .RequireAuthorization();

        app.MapGet("/status", () => { })
            .AllowAnonymous();
    }
}
