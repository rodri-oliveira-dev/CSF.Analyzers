using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

namespace Swa.Analyzers.SampleApp.Arch019;

[AllowAnonymous]
public sealed class PublicOrdersController : ControllerBase
{
    // ARCH019: [Authorize] is ineffective because the controller allows anonymous access.
    [Authorize]
    [HttpGet("orders")]
    public void Get()
    {
    }
}

[Authorize]
public sealed class ProtectedHealthController : ControllerBase
{
    // ARCH019: public exception in a protected controller must be reviewed explicitly.
    [AllowAnonymous]
    [HttpGet("health")]
    public void Health()
    {
    }
}

public sealed class MixedMetadataController : ControllerBase
{
    // ARCH019: avoid combining both authorization metadata on the same action.
    [Authorize]
    [AllowAnonymous]
    [HttpGet("profile")]
    public void Profile()
    {
    }
}

public static class ConflictingAuthorizationMinimalApi
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // ARCH019: avoid RequireAuthorization and AllowAnonymous on the same endpoint.
        app.MapGet("/orders", () => { })
            .RequireAuthorization()
            .AllowAnonymous();
    }
}
