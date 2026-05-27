using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

namespace Swa.Analyzers.SampleApp.Arch019;

[AllowAnonymous]
public sealed class PublicOrdersController : ControllerBase
{
    // ARCH019: [Authorize] é inefetivo porque o controller permite acesso anônimo.
    [Authorize]
    [HttpGet("orders")]
    public void Get()
    {
    }
}

[Authorize]
public sealed class ProtectedHealthController : ControllerBase
{
    // ARCH019: exceção pública em controller protegido deve ser revisada explicitamente.
    [AllowAnonymous]
    [HttpGet("health")]
    public void Health()
    {
    }
}

public sealed class MixedMetadataController : ControllerBase
{
    // ARCH019: evite combinar os dois metadados de autorização na mesma action.
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
        // ARCH019: evite RequireAuthorization e AllowAnonymous no mesmo endpoint.
        app.MapGet("/orders", () => { })
            .RequireAuthorization()
            .AllowAnonymous();
    }
}
