using Microsoft.CodeAnalysis.Testing;

using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch028ProhibitMutablePropertiesInRecordsAnalyzerTests
{
    [Fact]
    public async Task Reports_public_mutable_property_in_record()
    {
        const string source = """
public record Customer
{
    public string {|#0:Name|} { get; set; } = "";
}
""";

        await VerifyAsync(source, Expected(0, "Name"));
    }

    [Fact]
    public async Task Reports_required_mutable_property()
    {
        const string source = """
public record Customer
{
    public required string {|#0:Name|} { get; set; }
}
""";

        await VerifyAsync(source, Expected(0, "Name"));
    }

    [Fact]
    public async Task Reports_implicit_accessibility_mutable_property()
    {
        const string source = """
public record Customer
{
    string {|#0:Name|} { get; set; } = "";
}
""";

        await VerifyAsync(source, Expected(0, "Name"));
    }

    [Fact]
    public async Task Reports_mutable_property_in_record_class()
    {
        const string source = """
public record class Customer
{
    public string {|#0:Name|} { get; set; } = "";
}
""";

        await VerifyAsync(source, Expected(0, "Name"));
    }

    [Fact]
    public async Task Does_not_report_init_only_property()
    {
        const string source = """
public record Customer
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
public record Customer
{
    public string Name { get; } = "";
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_property_in_regular_class()
    {
        const string source = """
public sealed class Customer
{
    public string Name { get; set; } = "";
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_private_setter_by_default()
    {
        const string source = """
public record Customer
{
    public string Name { get; private set; } = "";
    public string Description { get; protected set; } = "";
    public string Code { get; internal set; } = "";
    public string Region { get; protected internal set; } = "";
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Reports_private_setter_when_non_public_setters_are_disallowed()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH028.allow_non_public_setters = false
""";

        const string source = """
public record Customer
{
    public string {|#0:Name|} { get; private set; } = "";
}
""";

        await VerifyAsync(source, editorConfig, Expected(0, "Name"));
    }

    [Fact]
    public async Task Uses_default_option_when_configuration_is_invalid()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH028.allow_non_public_setters = maybe
""";

        const string source = """
public record Customer
{
    public string Name { get; private set; } = "";
}
""";

        await VerifyAsync(source, editorConfig);
    }

    [Fact]
    public async Task Reports_multiple_mutable_properties_in_same_record()
    {
        const string source = """
public record Customer
{
    public string {|#0:Name|} { get; set; } = "";
    public int {|#1:Age|} { get; set; }
}
""";

        await VerifyAsync(source, Expected(0, "Name"), Expected(1, "Age"));
    }

    [Fact]
    public async Task Does_not_report_property_in_nested_regular_type_inside_record()
    {
        const string source = """
public record Customer
{
    public sealed class Snapshot
    {
        public string Name { get; set; } = "";
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Reports_mutable_property_in_record_struct()
    {
        const string source = """
public record struct Customer
{
    public string {|#0:Name|} { get; set; }
}
""";

        await VerifyAsync(source, Expected(0, "Name"));
    }

    private static Task VerifyAsync(string source, params DiagnosticResult[] expected)
    {
        return VerifyAsync(source, editorConfig: null, expected);
    }

    private static Task VerifyAsync(string source, string? editorConfig, params DiagnosticResult[] expected)
    {
        return Verifier<Arch028ProhibitMutablePropertiesInRecordsAnalyzer>.VerifyAnalyzerAsync(source, editorConfig, expected);
    }

    private static DiagnosticResult Expected(int location, string propertyName)
    {
        return Verifier<Arch028ProhibitMutablePropertiesInRecordsAnalyzer>.Diagnostic("ARCH028")
            .WithLocation(location)
            .WithArguments(propertyName);
    }
}
