using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public interface IServiceScopeFactory
    {
        IServiceScope CreateScope();
    }

    public interface IServiceScope : IDisposable
    {
        IServiceProvider ServiceProvider
        {
            get;
        }
    }
}

namespace Microsoft.Extensions.Options
{
    public interface IOptions<TOptions>
    {
        TOptions Value
        {
            get;
        }
    }

    public interface IOptionsMonitor<TOptions>
    {
        TOptions CurrentValue
        {
            get;
        }
    }

    public interface IOptionsSnapshot<TOptions> : IOptions<TOptions>
    {
    }
}
