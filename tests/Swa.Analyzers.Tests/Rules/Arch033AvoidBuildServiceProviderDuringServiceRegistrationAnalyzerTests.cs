using Microsoft.CodeAnalysis.Testing;

using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch033AvoidBuildServiceProviderDuringServiceRegistrationAnalyzerTests
{
    [Fact]
    public async Task Reports_BuildServiceProvider_on_IServiceCollection()
    {
        const string source = """
using Microsoft.Extensions.DependencyInjection;

public static class ServiceRegistration
{
    public static void Configure(IServiceCollection services)
    {
        services.{|#0:BuildServiceProvider|}();
    }
}
""";

        await VerifyAsync(source, Expected(0));
    }

    [Fact]
    public async Task Reports_BuildServiceProvider_on_builder_services()
    {
        const string source = """
using Microsoft.Extensions.DependencyInjection;

public sealed class AppBuilder
{
    public IServiceCollection Services { get; } = new ServiceCollection();
}

public static class ServiceRegistration
{
    public static void Configure(AppBuilder builder)
    {
        builder.Services.{|#0:BuildServiceProvider|}();
    }
}
""";

        await VerifyAsync(source, Expected(0));
    }

    [Fact]
    public async Task Reports_BuildServiceProvider_overload_with_validate_scopes()
    {
        const string source = """
using Microsoft.Extensions.DependencyInjection;

public static class ServiceRegistration
{
    public static void Configure(IServiceCollection services)
    {
        services.{|#0:BuildServiceProvider|}(validateScopes: true);
    }
}
""";

        await VerifyAsync(source, Expected(0));
    }

    [Fact]
    public async Task Does_not_report_runtime_IServiceProvider_parameter_usage()
    {
        const string source = """
using System;

public sealed class Handler
{
    public object? Handle(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetService(typeof(Handler));
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_custom_method_with_same_name()
    {
        const string source = """
public sealed class CustomServices
{
    public object BuildServiceProvider()
    {
        return new object();
    }
}

public static class ServiceRegistration
{
    public static void Configure(CustomServices services)
    {
        services.BuildServiceProvider();
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_custom_extension_method_with_same_name()
    {
        const string source = """
public sealed class CustomServices
{
}

public static class CustomServiceExtensions
{
    public static object BuildServiceProvider(this CustomServices services)
    {
        return new object();
    }
}

public static class ServiceRegistration
{
    public static void Configure(CustomServices services)
    {
        services.BuildServiceProvider();
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_tests_when_ignore_tests_is_true()
    {
        const string source = """
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class ServiceRegistrationTests
{
    [Fact]
    public void Builds_provider_for_integration_test()
    {
        var services = new ServiceCollection();
        services.BuildServiceProvider();
    }
}

namespace Xunit
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class FactAttribute : System.Attribute
    {
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Reports_tests_when_ignore_tests_is_false()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH033.ignore_tests = false
""";

        const string source = """
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class ServiceRegistrationTests
{
    [Fact]
    public void Builds_provider_for_integration_test()
    {
        var services = new ServiceCollection();
        services.{|#0:BuildServiceProvider|}();
    }
}

namespace Xunit
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class FactAttribute : System.Attribute
    {
    }
}
""";

        await VerifyAsync(source, editorConfig, Expected(0));
    }

    [Fact]
    public async Task Invalid_ignore_tests_option_uses_default_true()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH033.ignore_tests = maybe
""";

        const string source = """
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class ServiceRegistrationTests
{
    [Fact]
    public void Builds_provider_for_integration_test()
    {
        var services = new ServiceCollection();
        services.BuildServiceProvider();
    }
}

namespace Xunit
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class FactAttribute : System.Attribute
    {
    }
}
""";

        await VerifyAsync(source, editorConfig);
    }

    private static Task VerifyAsync(string source, params DiagnosticResult[] expected)
    {
        return VerifyAsync(source, editorConfig: null, expected);
    }

    private static Task VerifyAsync(string source, string? editorConfig, params DiagnosticResult[] expected)
    {
        return Verifier<Arch033AvoidBuildServiceProviderDuringServiceRegistrationAnalyzer>.VerifyAnalyzerAsync(
            source + DependencyInjectionStubs,
            editorConfig,
            expected);
    }

    private static DiagnosticResult Expected(int location)
    {
        return Verifier<Arch033AvoidBuildServiceProviderDuringServiceRegistrationAnalyzer>.Diagnostic("ARCH033")
            .WithLocation(location);
    }

    private const string DependencyInjectionStubs = """

namespace Microsoft.Extensions.DependencyInjection
{
    public interface IServiceCollection
    {
    }

    public sealed class ServiceCollection : IServiceCollection
    {
    }

    public sealed class ServiceProviderOptions
    {
    }

    public static class ServiceCollectionContainerBuilderExtensions
    {
        public static System.IServiceProvider BuildServiceProvider(this IServiceCollection services)
        {
            return new ServiceProvider();
        }

        public static System.IServiceProvider BuildServiceProvider(this IServiceCollection services, bool validateScopes)
        {
            return new ServiceProvider();
        }

        public static System.IServiceProvider BuildServiceProvider(this IServiceCollection services, ServiceProviderOptions options)
        {
            return new ServiceProvider();
        }
    }

    internal sealed class ServiceProvider : System.IServiceProvider
    {
        public object? GetService(System.Type serviceType)
        {
            return null;
        }
    }
}
""";
}
