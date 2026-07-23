using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using CSF.Analyzers.Architecture.Rules;
using CSF.Analyzers.Reliability.Rules;
using CSF.Analyzers.Testing.Rules;

namespace CSF.Analyzers.PackageValidation.Tests;

public sealed class AnalyzerPackageIsolationTests
{
    [Fact]
    public void Analyzer_packages_expose_only_their_expected_ids_without_duplicates()
    {
        var packages = new[]
        {
            new AnalyzerPackage(
                "CSF.Analyzers.Reliability",
                [
                    new Rel001AvoidTaskRunInAspNetRequestFlowAnalyzer(),
                    new Rel002ProhibitFireAndForgetInRequestFlowAnalyzer(),
                    new Rel003PreferAsNoTrackingForReadOnlyQueriesAnalyzer(),
                    new Rel004AvoidPrematureQueryMaterializationAnalyzer(),
                    new Rel005AvoidConcurrentDbContextOperationsAnalyzer(),
                    new Rel006AvoidScopedDependencyCaptureInHostedServicesAnalyzer(),
                ],
                ["REL001", "REL002", "REL003", "REL004", "REL005", "REL006"]),
            new AnalyzerPackage(
                "CSF.Analyzers.Architecture",
                [
                    new Arc001RequireExplicitAuthorizationOnHttpEndpointsAnalyzer(),
                    new Arc002PreventInfrastructureDependenciesInCoreLayersAnalyzer(),
                    new Arc003ProhibitVerbsInHttpRoutesAnalyzer(),
                    new Arc004ProhibitPublicSettersInDomainEntitiesAnalyzer(),
                    new Arc005AvoidDuplicatedMsBuildPropertiesAnalyzer(),
                    new Arc006AvoidDomainEntitiesInHttpContractsAnalyzer(),
                ],
                ["ARC001", "ARC002", "ARC003", "ARC004", "ARC005", "ARC006"]),
            new AnalyzerPackage(
                "CSF.Analyzers.Testing",
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
                new Arc006AvoidDomainEntitiesInHttpContractsAnalyzer(),
                new Tst001RestrictArgAnyUsageAnalyzer(),
                new Tst002WarnOnExcludingInBeEquivalentToAnalyzer(),
            }
            .SelectMany(static analyzer => analyzer.SupportedDiagnostics)
            .ToDictionary(static descriptor => descriptor.Id, StringComparer.Ordinal);

        foreach (var diagnosticId in new[] { "REL003", "ARC003", "ARC004", "ARC005", "ARC006", "TST001", "TST002" })
        {
            Assert.False(descriptors[diagnosticId].IsEnabledByDefault);
            Assert.Equal(DiagnosticSeverity.Info, descriptors[diagnosticId].DefaultSeverity);
        }
    }

    [Fact]
    public void Analyzer_descriptors_link_to_their_rule_documentation()
    {
        var descriptors = new DiagnosticAnalyzer[]
            {
                new Rel001AvoidTaskRunInAspNetRequestFlowAnalyzer(),
                new Rel002ProhibitFireAndForgetInRequestFlowAnalyzer(),
                new Rel003PreferAsNoTrackingForReadOnlyQueriesAnalyzer(),
                new Rel004AvoidPrematureQueryMaterializationAnalyzer(),
                new Rel005AvoidConcurrentDbContextOperationsAnalyzer(),
                new Rel006AvoidScopedDependencyCaptureInHostedServicesAnalyzer(),
                new Arc001RequireExplicitAuthorizationOnHttpEndpointsAnalyzer(),
                new Arc002PreventInfrastructureDependenciesInCoreLayersAnalyzer(),
                new Arc003ProhibitVerbsInHttpRoutesAnalyzer(),
                new Arc004ProhibitPublicSettersInDomainEntitiesAnalyzer(),
                new Arc005AvoidDuplicatedMsBuildPropertiesAnalyzer(),
                new Arc006AvoidDomainEntitiesInHttpContractsAnalyzer(),
                new Tst001RestrictArgAnyUsageAnalyzer(),
                new Tst002WarnOnExcludingInBeEquivalentToAnalyzer(),
            }
            .SelectMany(static analyzer => analyzer.SupportedDiagnostics)
            .ToArray();

        foreach (var descriptor in descriptors)
        {
            Assert.Equal(
                $"https://github.com/rodri-oliveira-dev/CSF.Analyzers/blob/main/docs/rules/{GetRuleGroup(descriptor.Id)}/{descriptor.Id}.md",
                descriptor.HelpLinkUri);
        }
    }

    private static string GetRuleGroup(string diagnosticId)
    {
        if (diagnosticId.StartsWith("REL", StringComparison.Ordinal))
        {
            return "reliability";
        }

        if (diagnosticId.StartsWith("ARC", StringComparison.Ordinal))
        {
            return "architecture";
        }

        if (diagnosticId.StartsWith("TST", StringComparison.Ordinal))
        {
            return "testing";
        }

        throw new InvalidOperationException($"Unknown diagnostic prefix for '{diagnosticId}'.");
    }

    private sealed record AnalyzerPackage(
        string Name,
        ImmutableArray<DiagnosticAnalyzer> Analyzers,
        ImmutableArray<string> ExpectedIds);
}
