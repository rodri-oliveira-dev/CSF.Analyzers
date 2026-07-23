using Microsoft.EntityFrameworkCore;

namespace CSF.Analyzers.SampleApp.Rel003;

public sealed class TrackedOrdersQuery
{
    private readonly OrdersDbContext _db;

    public TrackedOrdersQuery(OrdersDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Order>> ReadAsync()
    {
        return await _db.Orders
            .AsNoTracking()
            .Where(order => order.IsOpen)
            .ToListAsync();
    }

    public async Task<Order?> ReadWithTrackingAsync()
    {
        return await _db.Orders
            .AsTracking()
            .FirstOrDefaultAsync();
    }

    public async Task ProcessAsync()
    {
        var order = await _db.Orders.FirstOrDefaultAsync();
        order!.Status = "Processed";
        await _db.SaveChangesAsync();
    }
}

public sealed class OrdersDbContext : DbContext
{
    public DbSet<Order> Orders => throw new NotImplementedException();
}

public sealed class Order
{
    public bool IsOpen
    {
        get; set;
    }

    public string Status { get; set; } = "";
}
