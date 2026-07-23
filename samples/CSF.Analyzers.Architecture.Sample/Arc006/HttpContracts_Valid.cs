using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CSF.Analyzers.SampleApp.Arc006.Contracts;

public sealed record CreateOrderRequest(Guid CustomerId);

public sealed record OrderResponse(Guid Id);

public sealed class OrdersWithContractsController : ControllerBase
{
    [HttpPost("orders")]
    public IActionResult Create(CreateOrderRequest request) => null!;

    [HttpGet("orders/{id}")]
    public Task<ActionResult<OrderResponse>> Get(Guid id) => null!;
}

public static class OrderContractEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", (CreateOrderRequest request) => Results.Ok());
        app.MapGet("/orders/{id}", () => TypedResults.Ok(new OrderResponse(Guid.NewGuid())));
    }
}
