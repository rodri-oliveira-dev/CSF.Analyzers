using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

namespace Swa.Analyzers.SampleApp.Arch020;

[Authorize]
public sealed class ProtectedOrdersController : ControllerBase
{
    [HttpGet("orders")]
    public void Get()
    {
    }
}

public sealed class PublicLoginController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public void Login()
    {
    }
}

public static class ExplicitAuthorizationEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders", () => { })
            .RequireAuthorization();

        app.MapPost("/login", () => { })
            .AllowAnonymous();

        app.MapGet("/health", () => { });
    }
}
