using System.Collections.Immutable;

using Microsoft.CodeAnalysis.Diagnostics;

using Swa.Analyzers.Architecture.Rules;
using Swa.Analyzers.Reliability.Rules;
using Swa.Analyzers.Testing.Rules;

namespace Swa.Analyzers.PackageValidation.Tests;

public sealed class AnalyzerPackageIsolationTests
{
    [Fact]
    public void Analyzer_packages_expose_only_their_expected_arch_ids_without_duplicates()
    {
        var packages = new[]
        {
            new AnalyzerPackage(
                "Swa.Analyzers.Reliability",
                [
                    new Arch016AvoidTaskRunInAspNetRequestFlowAnalyzer(),
                    new Arch017ProhibitFireAndForgetInRequestFlowAnalyzer(),
                    new Arch021PreferAsNoTrackingForReadOnlyQueriesAnalyzer(),
                    new Arch022AvoidPrematureQueryMaterializationAnalyzer(),
                ],
                ["ARCH016", "ARCH017", "ARCH021", "ARCH022"]),
            new AnalyzerPackage(
                "Swa.Analyzers.Architecture",
                [
                    new Arch015ProhibitVerbsInHttpRoutesAnalyzer(),
                    new Arch020RequireExplicitAuthorizationOnHttpEndpointsAnalyzer(),
                    new Arch027PreventInfrastructureDependenciesInCoreLayersAnalyzer(),
                    new Arch029ProhibitPublicSettersInDomainEntitiesAnalyzer(),
                    new Arch032AvoidDuplicatedMsBuildPropertiesAnalyzer(),
                ],
                ["ARCH015", "ARCH020", "ARCH027", "ARCH029", "ARCH032"]),
            new AnalyzerPackage(
                "Swa.Analyzers.Testing",
                [
                    new Arch005RestrictArgAnyUsageAnalyzer(),
                    new Arch006WarnOnExcludingInBeEquivalentToAnalyzer(),
                ],
                ["ARCH005", "ARCH006"]),
        };

        var idsByPackage = packages.ToDictionary(
            static package => package.Name,
            static package => package.Analyzers
                .SelectMany(static analyzer => analyzer.SupportedDiagnostics)
                .Select(static descriptor => descriptor.Id)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToImmutableArray());

        foreach (var package in packages)
        {
            Assert.Equal(package.ExpectedIds.Order(StringComparer.Ordinal), idsByPackage[package.Name]);
        }

        var duplicates = idsByPackage
            .SelectMany(static pair => pair.Value.Select(id => new { Id = id, Package = pair.Key }))
            .GroupBy(static item => item.Id, StringComparer.Ordinal)
            .Where(static group => group.Select(static item => item.Package).Distinct(StringComparer.Ordinal).Count() > 1)
            .Select(static group => group.Key)
            .ToArray();

        Assert.Empty(duplicates);
    }

    private sealed record AnalyzerPackage(
        string Name,
        ImmutableArray<DiagnosticAnalyzer> Analyzers,
        ImmutableArray<string> ExpectedIds);
}
