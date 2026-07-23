using Microsoft.EntityFrameworkCore;

namespace Swa.Analyzers.SampleApp.Rel005;

public sealed class ConcurrentOrdersQuery
{
    private readonly OrdersDbContext _db;

    public ConcurrentOrdersQuery(OrdersDbContext db)
    {
        _db = db;
    }

    public async Task LoadDashboardAsync()
    {
        // REL005: duas consultas usam a mesma instancia de DbContext em paralelo.
        await Task.WhenAll(
            _db.Customers.AsNoTracking().ToListAsync(),
            _db.Orders.AsNoTracking().ToListAsync());
    }

    public async Task LoadStartedTasksAsync()
    {
        var customersTask = _db.Customers.AsNoTracking().ToListAsync();
        var ordersTask = _db.Orders.AsNoTracking().ToListAsync();

        // REL005: as duas tasks ja foram iniciadas sobre o mesmo DbContext.
        await Task.WhenAll(customersTask, ordersTask);
    }
}
