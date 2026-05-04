namespace Microsoft.Extensions.DependencyInjection
{
    public interface IServiceCollection
    {
    }

    public sealed class ServiceCollection : IServiceCollection
    {
    }

    public static class ServiceCollectionContainerBuilderExtensions
    {
        public static IServiceProvider BuildServiceProvider(this IServiceCollection services)
        {
            return new ServiceProvider();
        }
    }

    internal sealed class ServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return null;
        }
    }
}
