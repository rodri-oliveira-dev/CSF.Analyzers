using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

using System.Text;

namespace Swa.Analyzers.Tests;

internal static class Verifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    // Microsoft.CodeAnalysis.Testing 1.1.2 exposes reference assemblies up to .NET 9.
    private static readonly ReferenceAssemblies TargetReferenceAssemblies = ReferenceAssemblies.Net.Net90;

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

        if (editorConfig is not null)
        {
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", SourceText.From(editorConfig, Encoding.UTF8)));
        }

        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }
}
