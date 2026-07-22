using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Swa.Analyzers.Tests.Performance;

internal static class AnalyzerPerformanceRunner
{
    private static readonly MetadataReference[] References =
    [
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
        MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location),
        MetadataReference.CreateFromFile(Assembly.Load("System.Linq.Expressions").Location),
        MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
    ];

    public static async Task<AnalyzerPerformanceResult> MeasureAsync(
        DiagnosticAnalyzer analyzer,
        IEnumerable<(string FileName, string Source)> sources,
        CancellationToken cancellationToken = default)
    {
        var compilation = CreateCompilation(sources);

        // Estes testes são guardrails contra regressões grandes, não benchmarks científicos.
        var stopwatch = Stopwatch.StartNew();
        var diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create(analyzer), options: null)
            .GetAnalyzerDiagnosticsAsync(cancellationToken)
            .ConfigureAwait(false);
        stopwatch.Stop();

        return new AnalyzerPerformanceResult(stopwatch.Elapsed, diagnostics);
    }

    public static IEnumerable<(string FileName, string Source)> CreateNumberedSources(
        string prefix,
        int count,
        Func<int, string> createSource)
    {
        for (var index = 0; index < count; index++)
        {
            yield return ($"{prefix}{index}.cs", createSource(index));
        }
    }

    private static CSharpCompilation CreateCompilation(IEnumerable<(string FileName, string Source)> sources)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12);

        var syntaxTrees = sources.Select(source =>
            CSharpSyntaxTree.ParseText(
                source.Source,
                parseOptions,
                source.FileName));

        return CSharpCompilation.Create(
            "AnalyzerPerformanceTests",
            syntaxTrees,
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
