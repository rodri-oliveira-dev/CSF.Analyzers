using Microsoft.CodeAnalysis;

using CSF.Analyzers.Testing.Rules;

namespace Swa.Analyzers.Tests.Performance;

[Collection(PerformanceTestCollection.Name)]
public sealed class TestingAnalyzerPerformanceTests
{
    private static readonly TimeSpan ConservativeLimit = TimeSpan.FromSeconds(20);

    [Fact]
    public async Task TST001_handles_many_nsubstitute_matchers_within_guardrail()
    {
        var sources = new[]
            {
                ("NSubstituteStubs.cs", NSubstituteStubs),
                ("XunitStubs.cs", XunitStubs),
            }
            .Concat(AnalyzerPerformanceRunner.CreateNumberedSources(
                "NSubstituteTests",
                40,
                CreateNSubstituteTestSource));

        var result = await AnalyzerPerformanceRunner.MeasureAsync(
            new Tst001RestrictArgAnyUsageAnalyzer(),
            sources,
            new Dictionary<string, ReportDiagnostic>
            {
                ["TST001"] = ReportDiagnostic.Warn,
            });

        Assert.Equal(80, result.Diagnostics.Length);
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal("TST001", diagnostic.Id));
        Assert.True(
            result.Elapsed < ConservativeLimit,
            $"TST001 took {result.Elapsed.TotalSeconds:n2}s, above the {ConservativeLimit.TotalSeconds:n0}s guardrail.");
    }

    [Fact]
    public async Task TST002_handles_many_equivalency_assertions_within_guardrail()
    {
        var sources = new[]
            {
                ("FluentAssertionsStubs.cs", FluentAssertionsStubs),
                ("XunitStubs.cs", XunitStubs),
            }
            .Concat(AnalyzerPerformanceRunner.CreateNumberedSources(
                "EquivalencyTests",
                40,
                CreateEquivalencyTestSource));

        var result = await AnalyzerPerformanceRunner.MeasureAsync(
            new Tst002WarnOnExcludingInBeEquivalentToAnalyzer(),
            sources,
            new Dictionary<string, ReportDiagnostic>
            {
                ["TST002"] = ReportDiagnostic.Warn,
            });

        Assert.Equal(80, result.Diagnostics.Length);
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal("TST002", diagnostic.Id));
        Assert.True(
            result.Elapsed < ConservativeLimit,
            $"TST002 took {result.Elapsed.TotalSeconds:n2}s, above the {ConservativeLimit.TotalSeconds:n0}s guardrail.");
    }

    private static string CreateNSubstituteTestSource(int index)
    {
        return $$"""
using NSubstitute;
using Xunit;

namespace Performance.Testing.NSubstitute{{index}};

public interface IDependency{{index}}
{
    void Save(int value);
    void Delete(int value);
}

public sealed class MatcherTests{{index}}
{
    [Fact]
    public void Should_verify_calls()
    {
        IDependency{{index}} dependency = null!;
        dependency.Received().Save(NSubstitute.Arg.Any<int>());
        dependency.Received().Delete(NSubstitute.Arg.Any<int>());
        dependency.DidNotReceive().Save(NSubstitute.Arg.Any<int>());
        dependency.DidNotReceiveWithAnyArgs().Delete(default);
    }
}
""";
    }

    private static string CreateEquivalencyTestSource(int index)
    {
        return $$"""
using FluentAssertions;
using Xunit;

namespace Performance.Testing.Equivalency{{index}};

public sealed class EquivalencyTests{{index}}
{
    [Fact]
    public void Should_compare_objects()
    {
        var actual = new { Id = {{index}}, Name = "actual", Nested = new { Value = 1 } };
        var expected = new { Id = {{index}}, Name = "expected" };

        actual.Should().BeEquivalentTo(
            expected,
            options => options
                .Excluding(member => true)
                .ExcludingMissingMembers());
    }
}
""";
    }

    private const string XunitStubs = """
namespace Xunit
{
    public sealed class FactAttribute : System.Attribute
    {
    }
}
""";

    private const string NSubstituteStubs = """
namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }

    public static class SubstituteExtensions
    {
        public static T Received<T>(this T substitute) where T : class => substitute;
        public static T DidNotReceive<T>(this T substitute) where T : class => substitute;
        public static T DidNotReceiveWithAnyArgs<T>(this T substitute) where T : class => substitute;
    }
}
""";

    private const string FluentAssertionsStubs = """
namespace FluentAssertions
{
    public static class AssertionExtensions
    {
        public static ObjectAssertions Should(this object? value) => new();
    }

    public sealed class ObjectAssertions
    {
        public void BeEquivalentTo(object? expected, System.Func<Equivalency.EquivalencyAssertionOptions, Equivalency.EquivalencyAssertionOptions> config)
        {
        }
    }

    namespace Equivalency
    {
        public sealed class EquivalencyAssertionOptions
        {
            public EquivalencyAssertionOptions Excluding(System.Func<object, bool> predicate) => this;
            public EquivalencyAssertionOptions ExcludingMissingMembers() => this;
        }
    }
}
""";
}

