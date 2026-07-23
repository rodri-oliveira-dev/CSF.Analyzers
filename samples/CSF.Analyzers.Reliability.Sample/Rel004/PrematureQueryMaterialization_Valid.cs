namespace CSF.Analyzers.SampleApp.Rel004;

public sealed class ComposedOrdersQuery
{
    private readonly OrdersDbContext _db;

    public ComposedOrdersQuery(OrdersDbContext db)
    {
        _db = db;
    }

    public List<Order> FilterBeforeMaterialization()
    {
        return _db.Orders
            .Where(order => order.IsOpen)
            .ToList();
    }

    public List<Order> MaterializeAtEnd()
    {
        return _db.Orders.ToList();
    }
}

public sealed class OrdersDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public Microsoft.EntityFrameworkCore.DbSet<Order> Orders => throw new NotImplementedException();
}

public sealed class Order
{
    public bool IsOpen
    {
        get; set;
    }
}
