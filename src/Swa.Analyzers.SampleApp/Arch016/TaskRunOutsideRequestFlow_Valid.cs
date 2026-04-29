using Microsoft.Extensions.Hosting;

namespace Swa.Analyzers.SampleApp.Arch016;

public sealed class TaskRunValidWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Run(() => ProcessQueue(), stoppingToken);
    }

    private static void ProcessQueue()
    {
    }
}

public sealed class TaskRunValidConsoleJob
{
    public Task<int> Execute()
    {
        return Task.Run(() => 42);
    }
}
