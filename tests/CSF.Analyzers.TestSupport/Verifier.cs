using System.Text;

using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace CSF.Analyzers.Tests;

internal static class Verifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    private static readonly ReferenceAssemblies TargetReferenceAssemblies = new(
        "net10.0",
        new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.11"),
        Path.Combine("ref", "net10.0"));

    private static readonly HashSet<string> OptInDiagnosticIds =
    [
        "REL003",
        "ARC003",
        "ARC004",
        "ARC005",
        "ARC006",
        "TST001",
        "TST002",
    ];

    public static DiagnosticResult Diagnostic(string diagnosticId) =>
        CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);

    public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        return VerifyAnalyzerAsync(source, editorConfig: null, expected);
    }

    public static Task VerifyAnalyzerAsync(string source, string? editorConfig, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = TargetReferenceAssemblies,
        };

        var effectiveEditorConfig = AddOptInSeverity(editorConfig, expected);

        if (effectiveEditorConfig is not null)
        {
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", SourceText.From(effectiveEditorConfig, Encoding.UTF8)));
        }

        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    public static Task VerifyAnalyzerAsync(
        string source,
        string? editorConfig,
        IEnumerable<(string FileName, string Source)> additionalFiles,
        params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = TargetReferenceAssemblies,
        };

        var effectiveEditorConfig = AddOptInSeverity(editorConfig, expected);

        if (effectiveEditorConfig is not null)
        {
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", SourceText.From(effectiveEditorConfig, Encoding.UTF8)));
        }

        foreach (var additionalFile in additionalFiles)
        {
            test.TestState.AdditionalFiles.Add((additionalFile.FileName, SourceText.From(additionalFile.Source, Encoding.UTF8)));
        }

        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    public static Task VerifyAnalyzerAsync(
        IEnumerable<(string FileName, string Source)> sources,
        IEnumerable<(string FileName, string Source)> analyzerConfigFiles,
        params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = TargetReferenceAssemblies,
        };

        foreach (var source in sources)
        {
            test.TestState.Sources.Add((source.FileName, SourceText.From(source.Source, Encoding.UTF8)));
        }

        var expectedOptInIds = GetExpectedOptInIds(expected);
        var hasInjectedOptInConfig = false;

        foreach (var analyzerConfigFile in analyzerConfigFiles)
        {
            var source = analyzerConfigFile.Source;
            if (!hasInjectedOptInConfig && expectedOptInIds.Length > 0)
            {
                source = AddOptInSeverity(source, expectedOptInIds);
                hasInjectedOptInConfig = true;
            }

            test.TestState.AnalyzerConfigFiles.Add((analyzerConfigFile.FileName, SourceText.From(source, Encoding.UTF8)));
        }

        if (!hasInjectedOptInConfig && expectedOptInIds.Length > 0)
        {
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", SourceText.From(CreateOptInEditorConfig(expectedOptInIds), Encoding.UTF8)));
        }

        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    private static string? AddOptInSeverity(string? editorConfig, IEnumerable<DiagnosticResult> expected)
    {
        var expectedOptInIds = GetExpectedOptInIds(expected);
        if (expectedOptInIds.Length == 0)
        {
            return editorConfig;
        }

        return editorConfig is null
            ? CreateOptInEditorConfig(expectedOptInIds)
            : AddOptInSeverity(editorConfig, expectedOptInIds);
    }

    private static string AddOptInSeverity(string editorConfig, string[] expectedOptInIds)
    {
        var builder = new StringBuilder(editorConfig.TrimEnd());
        foreach (var diagnosticId in expectedOptInIds)
        {
            if (editorConfig.Contains($"dotnet_diagnostic.{diagnosticId}.severity", StringComparison.Ordinal))
            {
                continue;
            }

            builder.AppendLine();
            builder.Append("dotnet_diagnostic.");
            builder.Append(diagnosticId);
            builder.AppendLine(".severity = warning");
        }

        return builder.ToString();
    }

    private static string CreateOptInEditorConfig(string[] expectedOptInIds)
    {
        var builder = new StringBuilder();
        builder.AppendLine("root = true");
        builder.AppendLine();
        builder.AppendLine("[*]");
        foreach (var diagnosticId in expectedOptInIds)
        {
            builder.Append("dotnet_diagnostic.");
            builder.Append(diagnosticId);
            builder.AppendLine(".severity = warning");
        }

        return builder.ToString();
    }

    private static string[] GetExpectedOptInIds(IEnumerable<DiagnosticResult> expected)
    {
        return expected
            .Select(static diagnostic => diagnostic.Id)
            .Where(static id => id is not null && OptInDiagnosticIds.Contains(id))
            .Select(static id => id!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
