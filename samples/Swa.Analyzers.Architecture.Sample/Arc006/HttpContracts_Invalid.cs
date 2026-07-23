using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Swa.Analyzers.SampleApp.Arc006.Domain.Entities;

public sealed class Order
{
    public Guid Id
    {
        get;
        init;
    }
}

public sealed class OrdersController : ControllerBase
{
    [HttpPost("orders")]
    public IActionResult Create(Order order) => null!;

    [HttpGet("orders/{id}")]
    public Task<ActionResult<Order>> Get(Guid id) => null!;
}

public static class OrderEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // ARC006: contratos HTTP nao devem expor entidades de dominio diretamente.
        app.MapPost("/orders", (Order order) => Results.Ok());
        app.MapGet("/orders/{id}", () => TypedResults.Ok(new Order()));
    }
}
