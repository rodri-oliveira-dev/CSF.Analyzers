using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Swa.Analyzers.SampleApp.Rel006;

public sealed class CapturingDbContextWorker : BackgroundService
{
    private readonly OrdersDbContext _db;

    public CapturingDbContextWorker(OrdersDbContext db)
    {
        _db = db;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // REL006: DbContext e scoped por padrao e foi capturado por um hosted service singleton.
        _ = _db;
        return Task.CompletedTask;
    }
}

public sealed class CapturingOptionsSnapshotWorker : BackgroundService
{
    private readonly IOptionsSnapshot<WorkerOptions> _options;

    public CapturingOptionsSnapshotWorker(IOptionsSnapshot<WorkerOptions> options)
    {
        _options = options;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // REL006: IOptionsSnapshot<T> e scoped; prefira IOptionsMonitor<T> em singletons.
        _ = _options;
        return Task.CompletedTask;
    }
}

public sealed class OrdersDbContext : DbContext
{
}

public sealed class WorkerOptions
{
}
