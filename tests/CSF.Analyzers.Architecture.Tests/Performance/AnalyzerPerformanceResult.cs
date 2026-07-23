using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace CSF.Analyzers.Tests.Performance;

internal sealed class AnalyzerPerformanceResult
{
    public AnalyzerPerformanceResult(TimeSpan elapsed, ImmutableArray<Diagnostic> diagnostics)
    {
        Elapsed = elapsed;
        Diagnostics = diagnostics;
    }

    public TimeSpan Elapsed
    {
        get;
    }

    public ImmutableArray<Diagnostic> Diagnostics
    {
        get;
    }
}

