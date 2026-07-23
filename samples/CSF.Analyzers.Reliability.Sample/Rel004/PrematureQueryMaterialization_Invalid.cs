using Microsoft.EntityFrameworkCore;

namespace CSF.Analyzers.SampleApp.Rel004;

public sealed class PrematureOrdersQuery
{
    private readonly OrdersDbContext _db;

    public PrematureOrdersQuery(OrdersDbContext db)
    {
        _db = db;
    }

    public IEnumerable<Order> FilterAfterMaterialization()
    {
        // REL004: Where poderia compor a consulta antes de ToList().
        return _db.Orders
            .ToList()
            .Where(order => order.IsOpen);
    }

    public async Task<IEnumerable<Order>> FilterAfterAsyncMaterializationAsync()
    {
        // REL004: filtro imediatamente após ToListAsync() acontece em memória.
        var orders = await _db.Orders.ToListAsync();
        return orders.Where(order => order.IsOpen);
    }
}
