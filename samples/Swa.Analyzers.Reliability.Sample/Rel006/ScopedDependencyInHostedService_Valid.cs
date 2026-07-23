using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Swa.Analyzers.SampleApp.Rel006;

public sealed class ScopedResolutionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<WorkerOptions> _options;

    public ScopedResolutionWorker(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<WorkerOptions> options)
    {
        _scopeFactory = scopeFactory;
        _options = options;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var services = scope.ServiceProvider;
        var currentOptions = _options.CurrentValue;

        return Task.CompletedTask;
    }
}

public sealed class FactoryWorker : BackgroundService
{
    private readonly IDbContextFactory<OrdersDbContext> _factory;

    public FactoryWorker(IDbContextFactory<OrdersDbContext> factory)
    {
        _factory = factory;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = _factory;
        return Task.CompletedTask;
    }
}
