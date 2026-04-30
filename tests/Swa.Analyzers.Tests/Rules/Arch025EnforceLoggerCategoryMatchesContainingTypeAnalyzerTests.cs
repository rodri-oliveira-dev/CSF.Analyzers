using Microsoft.CodeAnalysis.Testing;

using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch025EnforceLoggerCategoryMatchesContainingTypeAnalyzerTests
{
    private const string LoggingStubs = """
namespace Microsoft.Extensions.Logging
{
    public interface ILogger
    {
    }

    public interface ILogger<TCategoryName> : ILogger
    {
    }
}
""";

    [Fact]
    public async Task Reports_constructor_parameter_with_different_logger_category()
    {
        const string source = """
using Microsoft.Extensions.Logging;

public sealed class CustomerService
{
    public CustomerService({|#0:ILogger<OrderService>|} logger)
    {
    }
}

public sealed class OrderService
{
}
""";

        await VerifyAsync(source, Expected(0, "OrderService", "CustomerService"));
    }

    [Fact]
    public async Task Reports_field_with_different_logger_category()
    {
        const string source = """
using Microsoft.Extensions.Logging;

public sealed class CustomerService
{
    private readonly {|#0:ILogger<OrderService>|} _logger;
}

public sealed class OrderService
{
}
""";

        await VerifyAsync(source, Expected(0, "OrderService", "CustomerService"));
    }

    [Fact]
    public async Task Reports_property_with_different_logger_category()
    {
        const string source = """
using Microsoft.Extensions.Logging;

public sealed class CustomerService
{
    public {|#0:ILogger<OrderService>|} Logger { get; }
}

public sealed class OrderService
{
}
""";

        await VerifyAsync(source, Expected(0, "OrderService", "CustomerService"));
    }

    [Fact]
    public async Task Does_not_report_logger_category_matching_containing_type()
    {
        const string source = """
using Microsoft.Extensions.Logging;

public sealed class CustomerService
{
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(ILogger<CustomerService> logger)
    {
        _logger = logger;
    }

    public ILogger<CustomerService> Logger => _logger;
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_non_generic_ILogger()
    {
        const string source = """
using Microsoft.Extensions.Logging;

public sealed class CustomerService
{
    private readonly ILogger _logger;

    public CustomerService(ILogger logger)
    {
        _logger = logger;
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_generic_class_when_logger_category_matches_containing_type()
    {
        const string source = """
using Microsoft.Extensions.Logging;

public sealed class Repository<TEntity>
{
    private readonly ILogger<Repository<TEntity>> _logger;

    public Repository(ILogger<Repository<TEntity>> logger)
    {
        _logger = logger;
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_nested_class_when_logger_category_matches_nested_type()
    {
        const string source = """
using Microsoft.Extensions.Logging;

public sealed class OuterService
{
    public sealed class InnerService
    {
        private readonly ILogger<InnerService> _logger;

        public InnerService(ILogger<InnerService> logger)
        {
            _logger = logger;
        }
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_different_logger_category_inside_tests()
    {
        const string source = """
using Microsoft.Extensions.Logging;
using Xunit;

public sealed class CustomerServiceTests
{
    private readonly ILogger<OrderService> _logger;

    public CustomerServiceTests(ILogger<OrderService> logger)
    {
        _logger = logger;
    }

    [Fact]
    public void Uses_logger_test_double()
    {
    }
}

public sealed class OrderService
{
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

    private static Task VerifyAsync(string source, params DiagnosticResult[] expected)
    {
        return Verifier<Arch025EnforceLoggerCategoryMatchesContainingTypeAnalyzer>.VerifyAnalyzerAsync(
            (new[]
            {
                ("LoggingStubs.cs", LoggingStubs),
                ("Test0.cs", source),
            }),
            Array.Empty<(string FileName, string Source)>(),
            expected);
    }

    private static DiagnosticResult Expected(int location, string loggerCategory, string containingType)
    {
        return Verifier<Arch025EnforceLoggerCategoryMatchesContainingTypeAnalyzer>.Diagnostic("ARCH025")
            .WithLocation(location)
            .WithArguments(loggerCategory, containingType);
    }
}
