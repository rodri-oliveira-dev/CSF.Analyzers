using Microsoft.CodeAnalysis.Testing;

using Swa.Analyzers.Architecture.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch027PreventInfrastructureDependenciesInCoreLayersAnalyzerTests
{
    private const string DefaultEditorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH027.core_namespace_patterns = "*.Domain;*.Application"
dotnet_diagnostic.ARCH027.forbidden_namespace_patterns = "Microsoft.EntityFrameworkCore;Microsoft.AspNetCore;StackExchange.Redis;Npgsql"
dotnet_diagnostic.ARCH027.allowed_namespace_patterns =
dotnet_diagnostic.ARCH027.ignore_tests = true
""";

    [Fact]
    public async Task Reports_forbidden_using_in_domain_namespace()
    {
        const string source = """
using {|#0:Microsoft.EntityFrameworkCore|};

namespace Billing.Domain;

public sealed class Invoice
{
}
""";

        await VerifyAsync(source, Expected(0, "Microsoft.EntityFrameworkCore", "Billing.Domain"));
    }

    [Fact]
    public async Task Reports_fully_qualified_forbidden_type_reference()
    {
        const string source = """
namespace Billing.Domain;

public sealed class InvoiceRepository
{
    private readonly {|#0:Microsoft.EntityFrameworkCore.DbContext|} _dbContext;
}
""";

        await VerifyAsync(source, Expected(0, "Microsoft.EntityFrameworkCore", "Billing.Domain"));
    }

    [Fact]
    public async Task Reports_application_dependency_on_forbidden_namespace()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH027.core_namespace_patterns = Billing.Application
dotnet_diagnostic.ARCH027.forbidden_namespace_patterns = Microsoft.EntityFrameworkCore
dotnet_diagnostic.ARCH027.allowed_namespace_patterns =
dotnet_diagnostic.ARCH027.ignore_tests = true
""";

        const string source = """
using {|#0:Microsoft.EntityFrameworkCore|};

namespace Billing.Application;

public sealed class ExportInvoices
{
}
""";

        await VerifyAsync(source, editorConfig, Expected(0, "Microsoft.EntityFrameworkCore", "Billing.Application"));
    }

    [Fact]
    public async Task Does_not_report_in_infrastructure_namespace()
    {
        const string source = """
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure.Persistence;

public sealed class BillingDbContext : DbContext
{
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_allowed_namespace_pattern()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH027.core_namespace_patterns = *.Domain
dotnet_diagnostic.ARCH027.forbidden_namespace_patterns = "Microsoft.EntityFrameworkCore;Microsoft.AspNetCore"
dotnet_diagnostic.ARCH027.allowed_namespace_patterns = Microsoft.AspNetCore.Http
dotnet_diagnostic.ARCH027.ignore_tests = true
""";

        const string source = """
using Microsoft.AspNetCore.Http;

namespace Billing.Domain;

public sealed class RequestContext
{
    private readonly HttpContext _httpContext;
}
""";

        await VerifyAsync(source, editorConfig);
    }

    [Fact]
    public async Task Does_not_report_similar_namespace_that_is_not_forbidden()
    {
        const string source = """
using Microsoft.EntityFrameworkCoreLike;

namespace Billing.Domain
{
    public sealed class Invoice
    {
        private readonly DbContext _dbContext;
    }
}

namespace Microsoft.EntityFrameworkCoreLike
{
    public sealed class DbContext
    {
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Reports_system_net_http_when_configured_as_forbidden()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH027.core_namespace_patterns = *.Application
dotnet_diagnostic.ARCH027.forbidden_namespace_patterns = System.Net.Http
dotnet_diagnostic.ARCH027.allowed_namespace_patterns =
dotnet_diagnostic.ARCH027.ignore_tests = true
""";

        const string source = """
using {|#0:System.Net.Http|};

namespace Billing.Application;

public sealed class CustomerGateway
{
}
""";

        await VerifyAsync(source, editorConfig, Expected(0, "System.Net.Http", "Billing.Application"));
    }

    [Fact]
    public async Task Does_not_report_tests_when_ignore_tests_is_true()
    {
        const string source = """
using Microsoft.EntityFrameworkCore;

namespace Billing.Domain.Tests;

public sealed class InvoiceTests
{
    private readonly DbContext _dbContext;
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
dotnet_diagnostic.ARCH027.core_namespace_patterns = *.Domain.Tests
dotnet_diagnostic.ARCH027.forbidden_namespace_patterns = Microsoft.EntityFrameworkCore
dotnet_diagnostic.ARCH027.allowed_namespace_patterns =
dotnet_diagnostic.ARCH027.ignore_tests = false
""";

        const string source = """
using {|#0:Microsoft.EntityFrameworkCore|};

namespace Billing.Domain.Tests;

public sealed class InvoiceTests
{
}
""";

        await VerifyAsync(source, editorConfig, Expected(0, "Microsoft.EntityFrameworkCore", "Billing.Domain.Tests"));
    }

    [Fact]
    public async Task Uses_file_specific_editorconfig_options()
    {
        const string domainEditorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH027.core_namespace_patterns = *.Domain
dotnet_diagnostic.ARCH027.forbidden_namespace_patterns = Npgsql
dotnet_diagnostic.ARCH027.allowed_namespace_patterns =
dotnet_diagnostic.ARCH027.ignore_tests = true
""";

        const string applicationEditorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH027.core_namespace_patterns = *.Application
dotnet_diagnostic.ARCH027.forbidden_namespace_patterns = StackExchange.Redis
dotnet_diagnostic.ARCH027.allowed_namespace_patterns =
dotnet_diagnostic.ARCH027.ignore_tests = true
""";

        const string domainSource = """
using {|#0:Npgsql|};
using StackExchange.Redis;

namespace Billing.Domain;

public sealed class DomainService
{
}
""";

        const string applicationSource = """
using Npgsql;
using {|#1:StackExchange.Redis|};

namespace Billing.Application;

public sealed class ApplicationService
{
}
""";

        await Verifier<Arch027PreventInfrastructureDependenciesInCoreLayersAnalyzer>.VerifyAnalyzerAsync(
            [
                ("/domain/DomainService.cs", domainSource),
                ("/application/ApplicationService.cs", applicationSource),
                ("InfrastructureStubs.cs", InfrastructureStubs),
            ],
            [
                ("/domain/.editorconfig", domainEditorConfig),
                ("/application/.editorconfig", applicationEditorConfig),
            ],
            Expected(0, "Npgsql", "Billing.Domain"),
            Expected(1, "StackExchange.Redis", "Billing.Application"));
    }

    [Fact]
    public async Task Normalizes_empty_pattern_entries()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH027.core_namespace_patterns = "*.Domain;;*.Application"
dotnet_diagnostic.ARCH027.forbidden_namespace_patterns = "Microsoft.EntityFrameworkCore;;Npgsql"
dotnet_diagnostic.ARCH027.allowed_namespace_patterns =
dotnet_diagnostic.ARCH027.ignore_tests = true
""";

        const string source = """
using {|#0:Microsoft.EntityFrameworkCore|};

namespace Billing.Domain;

public sealed class Invoice
{
}
""";

        await VerifyAsync(source, editorConfig, Expected(0, "Microsoft.EntityFrameworkCore", "Billing.Domain"));
    }

    [Fact]
    public async Task Normalizes_duplicate_pattern_entries()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH027.core_namespace_patterns = "*.Domain;*.Domain"
dotnet_diagnostic.ARCH027.forbidden_namespace_patterns = "Microsoft.EntityFrameworkCore;Microsoft.EntityFrameworkCore"
dotnet_diagnostic.ARCH027.allowed_namespace_patterns = "Microsoft.EntityFrameworkCore;Microsoft.EntityFrameworkCore"
dotnet_diagnostic.ARCH027.ignore_tests = true
""";

        const string source = """
using Microsoft.EntityFrameworkCore;

namespace Billing.Domain;

public sealed class Invoice
{
}
""";

        await VerifyAsync(source, editorConfig);
    }

    [Fact]
    public async Task Limits_configured_pattern_count()
    {
        var editorConfig = $$"""
root = true

[*.cs]
dotnet_diagnostic.ARCH027.core_namespace_patterns = "{{CreatePatternList("Billing.Layer", 300)}}"
dotnet_diagnostic.ARCH027.forbidden_namespace_patterns = Microsoft.EntityFrameworkCore
dotnet_diagnostic.ARCH027.allowed_namespace_patterns =
dotnet_diagnostic.ARCH027.ignore_tests = true
""";

        const string source = """
using Microsoft.EntityFrameworkCore;

namespace Billing.Layer299;

public sealed class Invoice
{
}
""";

        await VerifyAsync(source, editorConfig);
    }

    [Fact]
    public async Task Ignores_pattern_above_length_limit()
    {
        var editorConfig = $$"""
root = true

[*.cs]
dotnet_diagnostic.ARCH027.core_namespace_patterns = *.Domain
dotnet_diagnostic.ARCH027.forbidden_namespace_patterns = {{new string('A', 257)}}
dotnet_diagnostic.ARCH027.allowed_namespace_patterns =
dotnet_diagnostic.ARCH027.ignore_tests = true
""";

        const string source = """
using Microsoft.EntityFrameworkCore;

namespace Billing.Domain;

public sealed class Invoice
{
}
""";

        await VerifyAsync(source, editorConfig);
    }

    private static Task VerifyAsync(string source, params DiagnosticResult[] expected)
    {
        return VerifyAsync(source, DefaultEditorConfig, expected);
    }

    private static Task VerifyAsync(string source, string? editorConfig, params DiagnosticResult[] expected)
    {
        return Verifier<Arch027PreventInfrastructureDependenciesInCoreLayersAnalyzer>.VerifyAnalyzerAsync(
            [
                ("/0/Test0.cs", source),
                ("InfrastructureStubs.cs", InfrastructureStubs),
            ],
            editorConfig is null ? [] : [("/.editorconfig", editorConfig)],
            expected);
    }

    private static DiagnosticResult Expected(int location, string dependencyNamespace, string coreNamespace)
    {
        return Verifier<Arch027PreventInfrastructureDependenciesInCoreLayersAnalyzer>.Diagnostic("ARCH027")
            .WithLocation(location)
            .WithArguments(dependencyNamespace, coreNamespace);
    }

    private static string CreatePatternList(string prefix, int count)
    {
        return string.Join(";", Enumerable.Range(0, count).Select(index => prefix + index));
    }

    private const string InfrastructureStubs = """

namespace Microsoft.EntityFrameworkCore
{
    public class DbContext
    {
    }
}

namespace Microsoft.AspNetCore.Http
{
    public sealed class HttpContext
    {
    }
}

namespace Npgsql
{
    public sealed class NpgsqlConnection
    {
    }
}

namespace StackExchange.Redis
{
    public sealed class ConnectionMultiplexer
    {
    }
}
""";
}
