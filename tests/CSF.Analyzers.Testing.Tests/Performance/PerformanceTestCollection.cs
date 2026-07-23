namespace CSF.Analyzers.Tests.Performance;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PerformanceTestCollection
{
    public const string Name = "Analyzer performance tests";
}

