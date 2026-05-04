using Microsoft.Extensions.DependencyInjection;

namespace Swa.Analyzers.SampleApp.Arch033;

public sealed class RuntimeServiceProviderValid
{
    public object? ResolveAtRuntime(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetService(typeof(RuntimeServiceProviderValid));
    }
}

public static class CompositionRootValid
{
    public static void Configure(IServiceCollection services)
    {
        _ = services;
    }
}
