using Microsoft.EntityFrameworkCore;

namespace CSF.Analyzers.SampleApp.Rel005;

public sealed class SequentialOrdersQuery
{
    private readonly OrdersDbContext _db;

    public SequentialOrdersQuery(OrdersDbContext db)
    {
        _db = db;
    }

    public async Task LoadDashboardAsync()
    {
        var customers = await _db.Customers.AsNoTracking().ToListAsync();
        var orders = await _db.Orders.AsNoTracking().ToListAsync();
    }
}

public sealed class FactoryOrdersQuery
{
    private readonly IDbContextFactory<OrdersDbContext> _factory;

    public FactoryOrdersQuery(IDbContextFactory<OrdersDbContext> factory)
    {
        _factory = factory;
    }

    public async Task LoadByCustomerAsync(IEnumerable<int> customerIds)
    {
        await Parallel.ForEachAsync(customerIds, async (customerId, cancellationToken) =>
        {
            await using var db = await _factory.CreateDbContextAsync(cancellationToken);
            var orders = await db.Orders
                .AsNoTracking()
                .Where(order => order.CustomerId == customerId)
                .ToListAsync(cancellationToken);
        });
    }
}

public sealed class OrdersDbContext : Microsoft.EntityFrameworkCore.DbContext, IAsyncDisposable
{
    public Microsoft.EntityFrameworkCore.DbSet<Customer> Customers => throw new NotImplementedException();

    public Microsoft.EntityFrameworkCore.DbSet<Order> Orders => throw new NotImplementedException();

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}

public sealed class Customer
{
    public int Id
    {
        get; set;
    }
}

public sealed class Order
{
    public int CustomerId
    {
        get; set;
    }
}
