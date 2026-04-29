using Microsoft.EntityFrameworkCore;

namespace Swa.Analyzers.SampleApp.Arch022;

public sealed class PrematureOrdersQuery
{
    private readonly OrdersDbContext _db;

    public PrematureOrdersQuery(OrdersDbContext db)
    {
        _db = db;
    }

    public IEnumerable<Order> FilterAfterMaterialization()
    {
        // ARCH022: Where poderia compor a consulta antes de ToList().
        return _db.Orders
            .ToList()
            .Where(order => order.IsOpen);
    }

    public async Task<IEnumerable<Order>> FilterAfterAsyncMaterializationAsync()
    {
        // ARCH022: filtro imediatamente apos ToListAsync() acontece em memoria.
        var orders = await _db.Orders.ToListAsync();
        return orders.Where(order => order.IsOpen);
    }
}
