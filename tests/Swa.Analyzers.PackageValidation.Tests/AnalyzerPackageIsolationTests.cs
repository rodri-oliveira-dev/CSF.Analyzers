using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using Swa.Analyzers.Architecture.Rules;
using Swa.Analyzers.Reliability.Rules;
using Swa.Analyzers.Testing.Rules;

namespace Swa.Analyzers.PackageValidation.Tests;

public sealed class AnalyzerPackageIsolationTests
{
    [Fact]
    public void Analyzer_packages_expose_only_their_expected_ids_without_duplicates()
    {
        var packages = new[]
        {
            new AnalyzerPackage(
                "Swa.Analyzers.Reliability",
                [
                    new Rel001AvoidTaskRunInAspNetRequestFlowAnalyzer(),
                    new Rel002ProhibitFireAndForgetInRequestFlowAnalyzer(),
                    new Rel003PreferAsNoTrackingForReadOnlyQueriesAnalyzer(),
                    new Rel004AvoidPrematureQueryMaterializationAnalyzer(),
                ],
                ["REL001", "REL002", "REL003", "REL004"]),
            new AnalyzerPackage(
                "Swa.Analyzers.Architecture",
                [
                    new Arc001RequireExplicitAuthorizationOnHttpEndpointsAnalyzer(),
                    new Arc002PreventInfrastructureDependenciesInCoreLayersAnalyzer(),
                    new Arc003ProhibitVerbsInHttpRoutesAnalyzer(),
                    new Arc004ProhibitPublicSettersInDomainEntitiesAnalyzer(),
                    new Arc005AvoidDuplicatedMsBuildPropertiesAnalyzer(),
                ],
                ["ARC001", "ARC002", "ARC003", "ARC004", "ARC005"]),
            new AnalyzerPackage(
                "Swa.Analyzers.Testing",
                [
                    new Tst001RestrictArgAnyUsageAnalyzer(),
                    new Tst002WarnOnExcludingInBeEquivalentToAnalyzer(),
                ],
                ["TST001", "TST002"]),
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

    [Fact]
    public void Opt_in_rules_are_info_and_disabled_by_default()
    {
        var descriptors = new DiagnosticAnalyzer[]
            {
                new Rel003PreferAsNoTrackingForReadOnlyQueriesAnalyzer(),
                new Arc003ProhibitVerbsInHttpRoutesAnalyzer(),
                new Arc004ProhibitPublicSettersInDomainEntitiesAnalyzer(),
                new Arc005AvoidDuplicatedMsBuildPropertiesAnalyzer(),
                new Tst001RestrictArgAnyUsageAnalyzer(),
                new Tst002WarnOnExcludingInBeEquivalentToAnalyzer(),
            }
            .SelectMany(static analyzer => analyzer.SupportedDiagnostics)
            .ToDictionary(static descriptor => descriptor.Id, StringComparer.Ordinal);

        foreach (var diagnosticId in new[] { "REL003", "ARC003", "ARC004", "ARC005", "TST001", "TST002" })
        {
            Assert.False(descriptors[diagnosticId].IsEnabledByDefault);
            Assert.Equal(DiagnosticSeverity.Info, descriptors[diagnosticId].DefaultSeverity);
        }
    }

    private sealed record AnalyzerPackage(
        string Name,
        ImmutableArray<DiagnosticAnalyzer> Analyzers,
        ImmutableArray<string> ExpectedIds);
}
