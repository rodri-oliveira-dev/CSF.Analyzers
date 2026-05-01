using Microsoft.CodeAnalysis.Testing;

using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch030AvoidDuplicatedPackageReferencesAcrossProjectsAnalyzerTests
{
    private const string EmptySource = "public sealed class Placeholder { }";

    [Fact]
    public async Task Reports_duplicated_package_in_two_projects()
    {
        await VerifyAsync(
            Project("src/App.Domain/App.Domain.csproj", "Serilog"),
            Project("src/App.Application/App.Application.csproj", "Serilog"),
            Expected("src/App.Application/App.Application.csproj", "Serilog", "App.Application.csproj, App.Domain.csproj"));
    }

    [Fact]
    public async Task Does_not_report_unique_package()
    {
        await VerifyAsync(
            Project("src/App.Domain/App.Domain.csproj", "Dapper"),
            Project("src/App.Application/App.Application.csproj", "Serilog"));
    }

    [Fact]
    public async Task Does_not_report_default_allowed_package()
    {
        await VerifyAsync(
            Project("tests/App.Tests/App.Tests.csproj", "xunit"),
            Project("tests/App.IntegrationTests/App.IntegrationTests.csproj", "xunit"));
    }

    [Fact]
    public async Task Respects_allowed_packages_configuration()
    {
        const string editorConfig = """
root = true

[*.csproj]
dotnet_diagnostic.ARCH030.allowed_packages = ["Serilog"]
""";

        await VerifyAsync(
            editorConfig,
            Project("src/App.Domain/App.Domain.csproj", "Serilog"),
            Project("src/App.Application/App.Application.csproj", "Serilog"));
    }

    [Fact]
    public async Task Invalid_allowed_packages_configuration_uses_defaults()
    {
        const string editorConfig = """
root = true

[*.csproj]
dotnet_diagnostic.ARCH030.allowed_packages = ["xunit",
""";

        await VerifyAsync(
            editorConfig,
            Project("tests/App.Tests/App.Tests.csproj", "xunit"),
            Project("tests/App.IntegrationTests/App.IntegrationTests.csproj", "xunit"));
    }

    [Fact]
    public async Task Respects_allowed_project_patterns_configuration()
    {
        const string editorConfig = """
root = true

[*.csproj]
dotnet_diagnostic.ARCH030.allowed_project_patterns = ["*.Tests.csproj"]
""";

        await VerifyAsync(
            editorConfig,
            Project("src/App.Domain/App.Domain.csproj", "Serilog"),
            Project("tests/App.Tests/App.Tests.csproj", "Serilog"));
    }

    [Fact]
    public async Task Large_allowed_project_patterns_configuration_does_not_break_analysis()
    {
        var editorConfig = $$"""
root = true

[*.csproj]
dotnet_diagnostic.ARCH030.allowed_project_patterns = {{CreateJsonStringArray(Enumerable.Range(0, 300).Select(index => "src/Allowed" + index + ".csproj"))}}
""";

        await VerifyAsync(
            editorConfig,
            [
                Project("src/App.Domain/App.Domain.csproj", "Serilog"),
                Project("src/App.Application/App.Application.csproj", "Serilog"),
            ],
            Expected("src/App.Application/App.Application.csproj", "Serilog", "App.Application.csproj, App.Domain.csproj"));
    }

    [Fact]
    public async Task Ignores_allowed_project_pattern_above_length_limit()
    {
        var editorConfig = $$"""
root = true

[*.csproj]
dotnet_diagnostic.ARCH030.allowed_project_patterns = ["{{new string('*', 257)}}"]
""";

        await VerifyAsync(
            editorConfig,
            [
                Project("src/App.Domain/App.Domain.csproj", "Serilog"),
                Project("src/App.Application/App.Application.csproj", "Serilog"),
            ],
            Expected("src/App.Application/App.Application.csproj", "Serilog", "App.Application.csproj, App.Domain.csproj"));
    }

    [Fact]
    public async Task Normalizes_duplicate_allowed_project_patterns()
    {
        const string editorConfig = """
root = true

[*.csproj]
dotnet_diagnostic.ARCH030.allowed_project_patterns = [" *.Tests.csproj ", "*.Tests.csproj", "*.TESTS.csproj"]
""";

        await VerifyAsync(
            editorConfig,
            Project("src/App.Domain/App.Domain.csproj", "Serilog"),
            Project("tests/App.Tests/App.Tests.csproj", "Serilog"));
    }

    [Fact]
    public async Task Reports_package_declared_with_include()
    {
        await VerifyAsync(
            Project("src/App.Domain/App.Domain.csproj", "Serilog"),
            Project("src/App.Application/App.Application.csproj", "Serilog"),
            Expected("src/App.Application/App.Application.csproj", "Serilog", "App.Application.csproj, App.Domain.csproj"));
    }

    [Fact]
    public async Task Reports_package_declared_with_update()
    {
        await VerifyAsync(
            ProjectWithUpdate("src/App.Domain/App.Domain.csproj", "Serilog"),
            ProjectWithUpdate("src/App.Application/App.Application.csproj", "Serilog"),
            Expected("src/App.Application/App.Application.csproj", "Serilog", "App.Application.csproj, App.Domain.csproj"));
    }

    [Fact]
    public async Task Ignores_invalid_xml()
    {
        await VerifyAsync(("src/App.Domain/App.Domain.csproj", "<Project><ItemGroup>"));
    }

    [Fact]
    public async Task Ignores_empty_project_file()
    {
        await VerifyAsync(
            ("src/App.Domain/App.Domain.csproj", string.Empty),
            Project("src/App.Application/App.Application.csproj", "Serilog"));
    }

    [Fact]
    public async Task Ignores_project_file_larger_than_limit()
    {
        await VerifyAsync(
            ("src/App.Domain/App.Domain.csproj", CreateLargeProjectFile()),
            Project("src/App.Application/App.Application.csproj", "Serilog"));
    }

    [Fact]
    public async Task Ignores_project_file_with_dtd()
    {
        const string projectWithDtd = """
<!DOCTYPE Project [
  <!ENTITY package "Serilog">
]>
<Project>
  <ItemGroup>
    <PackageReference Include="&package;" />
  </ItemGroup>
</Project>
""";

        await VerifyAsync(
            ("src/App.Domain/App.Domain.csproj", projectWithDtd),
            Project("src/App.Application/App.Application.csproj", "Serilog"));
    }

    [Fact]
    public async Task Reports_duplicate_package_with_different_casing()
    {
        await VerifyAsync(
            Project("src/App.Domain/App.Domain.csproj", "Serilog"),
            Project("src/App.Application/App.Application.csproj", "serilog"),
            Expected("src/App.Application/App.Application.csproj", "Serilog", "App.Application.csproj, App.Domain.csproj"));
    }

    [Fact]
    public async Task Reports_only_one_diagnostic_per_duplicated_package()
    {
        await VerifyAsync(
            Project("src/App.Domain/App.Domain.csproj", "Serilog"),
            Project("src/App.Application/App.Application.csproj", "Serilog"),
            Project("src/App.Infrastructure/App.Infrastructure.csproj", "Serilog"),
            Expected("src/App.Application/App.Application.csproj", "Serilog", "App.Application.csproj, App.Domain.csproj, App.Infrastructure.csproj"));
    }

    private static Task VerifyAsync(params (string FileName, string Source)[] additionalFiles)
    {
        return VerifyAsync(editorConfig: null, additionalFiles);
    }

    private static Task VerifyAsync(string? editorConfig, params (string FileName, string Source)[] additionalFiles)
    {
        return VerifyAsync(editorConfig, additionalFiles, Array.Empty<DiagnosticResult>());
    }

    private static Task VerifyAsync(
        (string FileName, string Source) additionalFile,
        params DiagnosticResult[] expected)
    {
        return VerifyAsync(editorConfig: null, new[] { additionalFile }, expected);
    }

    private static Task VerifyAsync(
        (string FileName, string Source) firstAdditionalFile,
        (string FileName, string Source) secondAdditionalFile,
        params DiagnosticResult[] expected)
    {
        return VerifyAsync(editorConfig: null, new[] { firstAdditionalFile, secondAdditionalFile }, expected);
    }

    private static Task VerifyAsync(
        (string FileName, string Source) firstAdditionalFile,
        (string FileName, string Source) secondAdditionalFile,
        (string FileName, string Source) thirdAdditionalFile,
        params DiagnosticResult[] expected)
    {
        return VerifyAsync(editorConfig: null, new[] { firstAdditionalFile, secondAdditionalFile, thirdAdditionalFile }, expected);
    }

    private static Task VerifyAsync(
        string? editorConfig,
        (string FileName, string Source)[] additionalFiles,
        params DiagnosticResult[] expected)
    {
        return Verifier<Arch030AvoidDuplicatedPackageReferencesAcrossProjectsAnalyzer>.VerifyAnalyzerAsync(
            EmptySource,
            editorConfig,
            additionalFiles,
            expected);
    }

    private static (string FileName, string Source) Project(string fileName, string packageName)
    {
        return (fileName, $$"""
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="{{packageName}}" />
  </ItemGroup>
</Project>
""");
    }

    private static (string FileName, string Source) ProjectWithUpdate(string fileName, string packageName)
    {
        return (fileName, $$"""
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Update="{{packageName}}" />
  </ItemGroup>
</Project>
""");
    }

    private static string CreateLargeProjectFile()
    {
        return "<Project><ItemGroup>"
            + new string(' ', 1_000_001)
            + "<PackageReference Include=\"Serilog\" /></ItemGroup></Project>";
    }

    private static string CreateJsonStringArray(IEnumerable<string> values)
    {
        return "["
            + string.Join(", ", values.Select(static value => "\"" + value + "\""))
            + "]";
    }

    private static DiagnosticResult Expected(string path, string packageName, string projects)
    {
        return Verifier<Arch030AvoidDuplicatedPackageReferencesAcrossProjectsAnalyzer>.Diagnostic("ARCH030")
            .WithSpan(path, 1, 1, 1, 1)
            .WithArguments(packageName, projects);
    }
}
