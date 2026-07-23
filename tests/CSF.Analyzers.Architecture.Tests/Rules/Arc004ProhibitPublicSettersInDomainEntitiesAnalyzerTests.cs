using Microsoft.CodeAnalysis.Testing;

using CSF.Analyzers.Architecture.Rules;

namespace CSF.Analyzers.Tests.Rules;

public sealed class Arc004ProhibitPublicSettersInDomainEntitiesAnalyzerTests
{
    [Fact]
    public async Task Reports_public_setter_in_domain_entities_namespace()
    {
        const string source = """
namespace MyApp.Domain.Entities;

public sealed class Customer
{
    public string Name { get; {|#0:set|}; } = "";
}
""";

        await VerifyAsync(source, Expected(0, "Name"));
    }

    [Fact]
    public async Task Reports_public_setter_in_class_that_inherits_from_entity()
    {
        const string source = """
namespace MyApp.Domain;

public abstract class Entity
{
}

public sealed class Order : Entity
{
    public decimal Amount { get; {|#0:set|}; }
}
""";

        await VerifyAsync(source, Expected(0, "Amount"));
    }

    [Fact]
    public async Task Reports_public_setter_in_class_that_implements_entity_interface()
    {
        const string source = """
namespace MyApp.Domain;

public interface IEntity
{
}

public sealed class Customer : IEntity
{
    public string Name { get; {|#0:set|}; } = "";
}
""";

        await VerifyAsync(source, Expected(0, "Name"));
    }

    [Fact]
    public async Task Does_not_report_class_outside_domain()
    {
        const string source = """
namespace MyApp.Application;

public sealed class Customer
{
    public string Name { get; set; } = "";
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_private_setter()
    {
        const string source = """
namespace MyApp.Domain.Entities;

public sealed class Customer
{
    public string Name { get; private set; } = "";
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_init_only_property()
    {
        const string source = """
namespace MyApp.Domain.Entities;

public sealed class Customer
{
    public string Name { get; init; } = "";
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_read_only_property()
    {
        const string source = """
namespace MyApp.Domain.Entities;

public sealed class Customer
{
    public string Name { get; } = "";
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Respects_entity_namespaces_configuration()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARC004.severity = warning
dotnet_diagnostic.ARC004.entity_namespaces = ["MyApp.Model"]
""";

        const string source = """
namespace MyApp.Model.Customers;

public sealed class Customer
{
    public string Name { get; {|#0:set|}; } = "";
}
""";

        await VerifyAsync(source, editorConfig, Expected(0, "Name"));
    }

    [Fact]
    public async Task Respects_entity_base_types_configuration()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARC004.severity = warning
dotnet_diagnostic.ARC004.entity_base_types = ["DomainEntity"]
""";

        const string source = """
namespace MyApp.Model;

public abstract class DomainEntity
{
}

public sealed class Customer : DomainEntity
{
    public string Name { get; {|#0:set|}; } = "";
}
""";

        await VerifyAsync(source, editorConfig, Expected(0, "Name"));
    }

    [Fact]
    public async Task Reports_internal_setter_by_default()
    {
        const string source = """
namespace MyApp.Domain.Entities;

public sealed class Customer
{
    public string Name { get; internal {|#0:set|}; } = "";
}
""";

        await VerifyAsync(source, Expected(0, "Name"));
    }

    [Fact]
    public async Task Does_not_report_internal_setter_when_allowed()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARC004.severity = warning
dotnet_diagnostic.ARC004.allow_internal_setters = true
""";

        const string source = """
namespace MyApp.Domain.Entities;

public sealed class Customer
{
    public string Name { get; internal set; } = "";
}
""";

        await VerifyAsync(source, editorConfig);
    }

    [Fact]
    public async Task Does_not_report_dto_in_application_contracts_namespace()
    {
        const string source = """
namespace MyApp.Application.Contracts;

public sealed class CustomerDto
{
    public string Name { get; set; } = "";
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_namespace_that_only_starts_with_entity_marker_text()
    {
        const string source = """
namespace MyApp.Domain.EntitiesLike;

public sealed class Customer
{
    public string Name { get; set; } = "";
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Ignores_invalid_json_array_configuration()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARC004.severity = warning
dotnet_diagnostic.ARC004.entity_namespaces = MyApp.Model
dotnet_diagnostic.ARC004.entity_base_types = ["DomainEntity",
""";

        const string source = """
namespace MyApp.Model;

public abstract class DomainEntity
{
}

public sealed class Customer : DomainEntity
{
    public string Name { get; set; } = "";
}
""";

        await VerifyAsync(source, editorConfig);
    }

    [Fact]
    public async Task Does_not_report_test_class()
    {
        const string source = """
namespace MyApp.Domain.Entities.Tests;

public sealed class CustomerTests
{
    public string Name { get; set; } = "";
}
""";

        await VerifyAsync(source);
    }

    private static Task VerifyAsync(string source, params DiagnosticResult[] expected)
    {
        return VerifyAsync(source, editorConfig: null, expected);
    }

    private static Task VerifyAsync(string source, string? editorConfig, params DiagnosticResult[] expected)
    {
        return Verifier<Arc004ProhibitPublicSettersInDomainEntitiesAnalyzer>.VerifyAnalyzerAsync(source, editorConfig, expected);
    }

    private static DiagnosticResult Expected(int location, string propertyName)
    {
        return Verifier<Arc004ProhibitPublicSettersInDomainEntitiesAnalyzer>.Diagnostic("ARC004")
            .WithLocation(location)
            .WithArguments(propertyName);
    }
}
