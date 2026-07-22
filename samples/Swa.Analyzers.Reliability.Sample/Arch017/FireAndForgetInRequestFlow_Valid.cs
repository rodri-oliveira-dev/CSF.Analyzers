using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace Swa.Analyzers.SampleApp.Arch017;

public sealed class FireAndForgetValidController : ControllerBase
{
    public async Task PostAsync()
    {
        await SaveAsync();
    }

    public Task PutAsync()
    {
        return SaveAsync();
    }

    private static Task SaveAsync() => Task.CompletedTask;
}

public sealed class FireAndForgetValidWorker : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = ProcessQueueAsync(stoppingToken);
        return Task.CompletedTask;
    }

    private static Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}
