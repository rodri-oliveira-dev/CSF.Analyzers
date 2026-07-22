using Microsoft.EntityFrameworkCore;

namespace Swa.Analyzers.SampleApp.Rel003;

public sealed class ReadOnlyOrdersQuery
{
    private readonly OrdersDbContext _db;

    public ReadOnlyOrdersQuery(OrdersDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Order>> ExecuteAsync()
    {
        // REL003: consulta de leitura materializada sem AsNoTracking().
        return await _db.Orders
            .Where(order => order.IsOpen)
            .ToListAsync();
    }
}
