using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

namespace Swa.Analyzers.SampleApp.Arch015;

[Route("api/v1/apolices")]
public sealed class HttpRouteVerbsValidController
{
    [HttpGet("{id:int}")]
    public void GetById()
    {
    }

    [HttpPost("")]
    public void Create()
    {
    }

    [HttpPut("{id}")]
    public void Update()
    {
    }

    [HttpGet("[action]")]
    public void ActionToken()
    {
    }
}

public static class HttpRouteVerbsValidMinimalApi
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/apolices/{id}/itens", () => { });
        app.MapPost("/clientes", () => { });
        app.MapGet("/approval-status/{id}", () => { });
        app.MapGet("/created-at/{id}", () => { });
    }
}
