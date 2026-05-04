using Microsoft.Extensions.DependencyInjection;

namespace Swa.Analyzers.SampleApp.Arch033;

public static class BuildServiceProviderInvalid
{
    public static void Configure(IServiceCollection services)
    {
        // ARCH033: Avoid creating a parallel provider while registering services.
        services.BuildServiceProvider();
    }
}
