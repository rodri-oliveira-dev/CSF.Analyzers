using Microsoft.CodeAnalysis.Testing;

using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch026AvoidInsecureCorsConfigurationAnalyzerTests
{
    [Fact]
    public async Task Reports_allow_any_origin_followed_by_allow_credentials()
    {
        const string source = """
using Microsoft.AspNetCore.Cors.Infrastructure;

public static class CorsConfiguration
{
    public static void Configure(CorsPolicyBuilder policy)
    {
        policy.AllowAnyOrigin().{|#0:AllowCredentials|}();
    }
}
""";

        await VerifyCorsAsync(source, Expected(0, "AllowCredentials", "AllowAnyOrigin"));
    }

    [Fact]
    public async Task Reports_allow_credentials_followed_by_allow_any_origin()
    {
        const string source = """
using Microsoft.AspNetCore.Cors.Infrastructure;

public static class CorsConfiguration
{
    public static void Configure(CorsPolicyBuilder policy)
    {
        policy.AllowCredentials().{|#0:AllowAnyOrigin|}();
    }
}
""";

        await VerifyCorsAsync(source, Expected(0, "AllowAnyOrigin", "AllowCredentials"));
    }

    [Fact]
    public async Task Reports_combination_across_longer_chain()
    {
        const string source = """
using Microsoft.AspNetCore.Cors.Infrastructure;

public static class CorsConfiguration
{
    public static void Configure(CorsPolicyBuilder policy)
    {
        policy
            .AllowAnyHeader()
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .{|#0:AllowCredentials|}();
    }
}
""";

        await VerifyCorsAsync(source, Expected(0, "AllowCredentials", "AllowAnyOrigin"));
    }

    [Fact]
    public async Task Does_not_report_with_origins()
    {
        const string source = """
using Microsoft.AspNetCore.Cors.Infrastructure;

public static class CorsConfiguration
{
    public static void Configure(CorsPolicyBuilder policy)
    {
        policy.WithOrigins("https://example.com").AllowCredentials();
    }
}
""";

        await VerifyCorsAsync(source);
    }

    [Fact]
    public async Task Does_not_report_allow_any_origin_without_credentials_by_default()
    {
        const string source = """
using Microsoft.AspNetCore.Cors.Infrastructure;

public static class CorsConfiguration
{
    public static void Configure(CorsPolicyBuilder policy)
    {
        policy.AllowAnyOrigin();
    }
}
""";

        await VerifyCorsAsync(source);
    }

    [Fact]
    public async Task Reports_allow_any_origin_without_credentials_when_configured()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH026.disallow_any_origin = true
""";

        const string source = """
using Microsoft.AspNetCore.Cors.Infrastructure;

public static class CorsConfiguration
{
    public static void Configure(CorsPolicyBuilder policy)
    {
        policy.{|#0:AllowAnyOrigin|}();
    }
}
""";

        await VerifyCorsAsync(source, editorConfig, Expected(0, "AllowAnyOrigin", "policy"));
    }

    [Fact]
    public async Task Does_not_report_allow_any_origin_when_configuration_is_false_or_invalid()
    {
        const string editorConfig = """
root = true

[/false/*.cs]
dotnet_diagnostic.ARCH026.disallow_any_origin = false

[/invalid/*.cs]
dotnet_diagnostic.ARCH026.disallow_any_origin = yes
""";

        const string falseSource = """
using Microsoft.AspNetCore.Cors.Infrastructure;

public static class FalseCorsConfiguration
{
    public static void Configure(CorsPolicyBuilder policy)
    {
        policy.AllowAnyOrigin();
    }
}
""";

        const string invalidSource = """
using Microsoft.AspNetCore.Cors.Infrastructure;

public static class InvalidCorsConfiguration
{
    public static void Configure(CorsPolicyBuilder policy)
    {
        policy.AllowAnyOrigin();
    }
}
""";

        await Verifier<Arch026AvoidInsecureCorsConfigurationAnalyzer>.VerifyAnalyzerAsync(
            [
                ("/false/CorsConfiguration.cs", falseSource),
                ("/invalid/CorsConfiguration.cs", invalidSource),
                ("CorsStubs.cs", CorsStubs),
            ],
            [("/.editorconfig", editorConfig)]);
    }

    [Fact]
    public async Task Does_not_report_custom_methods_with_same_names()
    {
        const string source = """
public sealed class CustomCorsPolicyBuilder
{
    public CustomCorsPolicyBuilder AllowAnyOrigin() => this;
    public CustomCorsPolicyBuilder AllowCredentials() => this;
}

public static class CorsConfiguration
{
    public static void Configure(CustomCorsPolicyBuilder policy)
    {
        policy.AllowAnyOrigin().AllowCredentials();
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_inside_test_context()
    {
        const string source = """
using Microsoft.AspNetCore.Cors.Infrastructure;
using Xunit;

public sealed class CorsConfigurationTests
{
    [Fact]
    public void Configure_allows_test_fixture_to_exercise_insecure_policy()
    {
        var policy = new CorsPolicyBuilder();

        policy.AllowAnyOrigin().AllowCredentials();
    }
}

namespace Xunit
{
    public sealed class FactAttribute : System.Attribute
    {
    }
}
""";

        await VerifyCorsAsync(source);
    }

    private static Task VerifyCorsAsync(string source, params DiagnosticResult[] expected)
    {
        return VerifyAsync(source + CorsStubs, editorConfig: null, expected);
    }

    private static Task VerifyCorsAsync(string source, string? editorConfig, params DiagnosticResult[] expected)
    {
        return VerifyAsync(source + CorsStubs, editorConfig, expected);
    }

    private static Task VerifyAsync(string source, string? editorConfig = null, params DiagnosticResult[] expected)
    {
        return Verifier<Arch026AvoidInsecureCorsConfigurationAnalyzer>.VerifyAnalyzerAsync(source, editorConfig, expected);
    }

    private static DiagnosticResult Expected(int location, string currentMethod, string conflictingMethod)
    {
        return Verifier<Arch026AvoidInsecureCorsConfigurationAnalyzer>.Diagnostic("ARCH026")
            .WithLocation(location)
            .WithArguments(currentMethod, conflictingMethod);
    }

    private const string CorsStubs = """

namespace Microsoft.AspNetCore.Cors.Infrastructure
{
    public sealed class CorsPolicyBuilder
    {
        public CorsPolicyBuilder AllowAnyOrigin() => this;
        public CorsPolicyBuilder AllowCredentials() => this;
        public CorsPolicyBuilder AllowAnyHeader() => this;
        public CorsPolicyBuilder AllowAnyMethod() => this;
        public CorsPolicyBuilder WithOrigins(params string[] origins) => this;
    }
}
""";
}
