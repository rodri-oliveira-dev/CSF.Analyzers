using Microsoft.Extensions.DependencyInjection;

namespace Swa.Analyzers.SampleApp.Arch033;

public static class BuildServiceProviderInvalid
{
    public static void Configure(IServiceCollection services)
    {
        // ARCH033: evite criar um provider paralelo durante o registro de serviços.
        services.BuildServiceProvider();
    }
}
