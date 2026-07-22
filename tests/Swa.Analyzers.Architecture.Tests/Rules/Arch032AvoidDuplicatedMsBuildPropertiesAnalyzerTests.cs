using Microsoft.CodeAnalysis.Testing;

using Swa.Analyzers.Architecture.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch032AvoidDuplicatedMsBuildPropertiesAnalyzerTests
{
    private const string EmptySource = "public sealed class Placeholder { }";

    [Fact]
    public async Task Reports_duplicated_property_with_same_value()
    {
        await VerifyAsync(
            Props("Directory.Build.props", ("Nullable", "enable")),
            Project("src/App/App.csproj", ("Nullable", "enable")),
            Expected("src/App/App.csproj", "Nullable"));
    }

    [Fact]
    public async Task Does_not_report_different_property()
    {
        await VerifyAsync(
            Props("Directory.Build.props", ("Nullable", "enable")),
            Project("src/App/App.csproj", ("ImplicitUsings", "enable")));
    }

    [Fact]
    public async Task Does_not_report_default_ignored_property()
    {
        await VerifyAsync(
            Props("Directory.Build.props", ("TargetFramework", "net9.0")),
            Project("src/App/App.csproj", ("TargetFramework", "net9.0")));
    }

    [Fact]
    public async Task Respects_ignored_properties_configuration()
    {
        const string editorConfig = """
root = true

[*.csproj]
dotnet_diagnostic.ARCH032.ignored_properties = ["Nullable"]
""";

        await VerifyAsync(
            editorConfig,
            Props("Directory.Build.props", ("Nullable", "enable")),
            Project("src/App/App.csproj", ("Nullable", "enable")));
    }

    [Fact]
    public async Task Compare_values_true_does_not_report_same_name_with_different_value()
    {
        await VerifyAsync(
            Props("Directory.Build.props", ("Nullable", "enable")),
            Project("src/App/App.csproj", ("Nullable", "disable")));
    }

    [Fact]
    public async Task Compare_values_false_reports_same_name_with_different_value()
    {
        const string editorConfig = """
root = true

[*.csproj]
dotnet_diagnostic.ARCH032.compare_values = false
""";

        await VerifyAsync(
            editorConfig,
            new[]
            {
                Props("Directory.Build.props", ("Nullable", "enable")),
                Project("src/App/App.csproj", ("Nullable", "disable")),
            },
            Expected("src/App/App.csproj", "Nullable"));
    }

    [Fact]
    public async Task Ignores_properties_with_condition()
    {
        await VerifyAsync(
            PropsWithConditionalProperty("Directory.Build.props", "Nullable", "enable"),
            Project("src/App/App.csproj", ("Nullable", "enable")),
            ProjectWithConditionalPropertyGroup("src/Worker/Worker.csproj", "Nullable", "enable"));
    }

    [Fact]
    public async Task Ignores_invalid_xml()
    {
        await VerifyAsync(
            ("Directory.Build.props", "<Project><PropertyGroup>"),
            Project("src/App/App.csproj", ("Nullable", "enable")));

        await VerifyAsync(
            Props("Directory.Build.props", ("Nullable", "enable")),
            ("src/App/App.csproj", "<Project><PropertyGroup>"));
    }

    [Fact]
    public async Task Ignores_empty_msbuild_files()
    {
        await VerifyAsync(
            ("Directory.Build.props", string.Empty),
            Project("src/App/App.csproj", ("Nullable", "enable")));

        await VerifyAsync(
            Props("Directory.Build.props", ("Nullable", "enable")),
            ("src/App/App.csproj", string.Empty));
    }

    [Fact]
    public async Task Ignores_msbuild_file_larger_than_limit()
    {
        await VerifyAsync(
            ("Directory.Build.props", CreateLargeMsBuildFile("Nullable", "enable")),
            Project("src/App/App.csproj", ("Nullable", "enable")));

        await VerifyAsync(
            Props("Directory.Build.props", ("Nullable", "enable")),
            ("src/App/App.csproj", CreateLargeMsBuildFile("Nullable", "enable")));
    }

    [Fact]
    public async Task Ignores_msbuild_file_with_dtd()
    {
        const string propsWithDtd = """
<!DOCTYPE Project [
  <!ENTITY nullable "enable">
]>
<Project>
  <PropertyGroup>
    <Nullable>&nullable;</Nullable>
  </PropertyGroup>
</Project>
""";

        await VerifyAsync(
            ("Directory.Build.props", propsWithDtd),
            Project("src/App/App.csproj", ("Nullable", "enable")));
    }

    [Fact]
    public async Task Reports_duplicates_in_multiple_projects()
    {
        await VerifyAsync(
            Props("Directory.Build.props", ("Nullable", "enable")),
            Project("src/App/App.csproj", ("Nullable", "enable")),
            Project("src/Worker/Worker.csproj", ("Nullable", "enable")),
            Expected("src/App/App.csproj", "Nullable"),
            Expected("src/Worker/Worker.csproj", "Nullable"));
    }

    [Fact]
    public async Task Uses_nearest_directory_build_props()
    {
        await VerifyAsync(
            Props("Directory.Build.props", ("Nullable", "enable")),
            Props("src/Directory.Build.props", ("LangVersion", "preview")),
            Project("src/App/App.csproj", ("Nullable", "enable"), ("LangVersion", "preview")),
            Expected("src/App/App.csproj", 4, "LangVersion"));
    }

    [Fact]
    public async Task Invalid_ignored_properties_configuration_uses_defaults()
    {
        const string editorConfig = """
root = true

[*.csproj]
dotnet_diagnostic.ARCH032.ignored_properties = ["TargetFramework",
""";

        await VerifyAsync(
            editorConfig,
            Props("Directory.Build.props", ("TargetFramework", "net9.0")),
            Project("src/App/App.csproj", ("TargetFramework", "net9.0")));
    }

    private static Task VerifyAsync(params (string FileName, string Source)[] additionalFiles)
    {
        return VerifyAsync(editorConfig: null, additionalFiles, Array.Empty<DiagnosticResult>());
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
        (string FileName, string Source) firstAdditionalFile,
        (string FileName, string Source) secondAdditionalFile,
        params DiagnosticResult[] expected)
    {
        return VerifyAsync(editorConfig, new[] { firstAdditionalFile, secondAdditionalFile }, expected);
    }

    private static Task VerifyAsync(
        string? editorConfig,
        (string FileName, string Source)[] additionalFiles,
        params DiagnosticResult[] expected)
    {
        return Verifier<Arch032AvoidDuplicatedMsBuildPropertiesAnalyzer>.VerifyAnalyzerAsync(
            EmptySource,
            editorConfig,
            additionalFiles,
            expected);
    }

    private static (string FileName, string Source) Props(string fileName, params (string Name, string Value)[] properties)
    {
        return (fileName, CreateXml(properties));
    }

    private static (string FileName, string Source) Project(string fileName, params (string Name, string Value)[] properties)
    {
        return (fileName, CreateXml(properties, " Sdk=\"Microsoft.NET.Sdk\""));
    }

    private static (string FileName, string Source) PropsWithConditionalProperty(string fileName, string name, string value)
    {
        return (fileName, $$"""
<Project>
  <PropertyGroup>
    <{{name}} Condition="'$(Configuration)' == 'Release'">{{value}}</{{name}}>
  </PropertyGroup>
</Project>
""");
    }

    private static (string FileName, string Source) ProjectWithConditionalPropertyGroup(string fileName, string name, string value)
    {
        return (fileName, $$"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <{{name}}>{{value}}</{{name}}>
  </PropertyGroup>
</Project>
""");
    }

    private static string CreateXml((string Name, string Value)[] properties, string projectAttributes = "")
    {
        var propertyLines = string.Join(
            Environment.NewLine,
            properties.Select(static property => $"    <{property.Name}>{property.Value}</{property.Name}>"));

        return $$"""
<Project{{projectAttributes}}>
  <PropertyGroup>
{{propertyLines}}
  </PropertyGroup>
</Project>
""";
    }

    private static string CreateLargeMsBuildFile(string name, string value)
    {
        return $$"""
<Project>
  <PropertyGroup>
    <{{name}}>{{value}}</{{name}}>
    <Large>{{new string(' ', 1_000_001)}}</Large>
  </PropertyGroup>
</Project>
""";
    }

    private static DiagnosticResult Expected(string path, string propertyName)
    {
        return Expected(path, 3, propertyName);
    }

    private static DiagnosticResult Expected(string path, int line, string propertyName)
    {
        return Verifier<Arch032AvoidDuplicatedMsBuildPropertiesAnalyzer>.Diagnostic("ARCH032")
            .WithSpan(path, line, 6, line, propertyName.Length + 8)
            .WithArguments(propertyName);
    }
}
