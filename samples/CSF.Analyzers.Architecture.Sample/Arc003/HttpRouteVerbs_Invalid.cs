using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

namespace CSF.Analyzers.SampleApp.Arc003;

[Route("api/v1/apolices")]
public sealed class HttpRouteVerbsInvalidController
{
    [HttpGet("buscar/{id}")]
    public void GetById()
    {
    }

    [HttpPost("emitir")]
    public void Issue()
    {
    }

    [HttpPut("{id}/cancelar")]
    public void Cancel()
    {
    }
}

public static class HttpRouteVerbsInvalidMinimalApi
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/apolices/emitir", () => { });
        app.MapPost("/clientes/ativar", () => { });
    }
}
