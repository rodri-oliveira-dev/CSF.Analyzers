using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using CSF.Analyzers.Common.Common;

namespace CSF.Analyzers.Architecture.Rules;

internal static class DomainEntityClassifier
{
    private const string EntityNamespacesOption = "dotnet_diagnostic.ARC004.entity_namespaces";
    private const string EntityBaseTypesOption = "dotnet_diagnostic.ARC004.entity_base_types";
    private const string AllowInternalSettersOption = "dotnet_diagnostic.ARC004.allow_internal_setters";

    private static readonly ImmutableArray<string> DefaultEntityNamespaceMarkers = ImmutableArray.Create(
        ".Domain.Entities",
        ".Domain.Entity",
        ".Domain.Aggregates",
        ".Domain.Aggregate");

    private static readonly ImmutableHashSet<string> DefaultEntityTypeNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "Entity",
        "AggregateRoot",
        "IEntity",
        "IAggregateRoot");

    public static bool IsDomainEntity(INamedTypeSymbol type, DomainEntityOptions options)
    {
        if (IsEntityNamespace(type.ContainingNamespace?.ToDisplayString(), options))
        {
            return true;
        }

        for (INamedTypeSymbol? current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (IsEntityTypeName(current, options))
            {
                return true;
            }
        }

        foreach (var interfaceType in type.AllInterfaces)
        {
            if (IsEntityTypeName(interfaceType, options))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsTestType(INamedTypeSymbol type, string filePath)
    {
        var namespaceName = type.ContainingNamespace?.ToDisplayString();

        if (!string.IsNullOrWhiteSpace(namespaceName)
            && (namespaceName!.EndsWith(".Tests", StringComparison.Ordinal)
                || namespaceName.IndexOf(".Tests.", StringComparison.Ordinal) >= 0))
        {
            return true;
        }

        if (type.Name.EndsWith("Tests", StringComparison.Ordinal)
            || type.Name.EndsWith("Test", StringComparison.Ordinal)
            || type.Name.EndsWith("Specs", StringComparison.Ordinal)
            || type.Name.EndsWith("Spec", StringComparison.Ordinal))
        {
            return true;
        }

        return filePath.IndexOf("/Tests/", StringComparison.OrdinalIgnoreCase) >= 0
            || filePath.IndexOf("\\Tests\\", StringComparison.OrdinalIgnoreCase) >= 0
            || filePath.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEntityNamespace(string? namespaceName, DomainEntityOptions options)
    {
        if (string.IsNullOrWhiteSpace(namespaceName))
        {
            return false;
        }

        var currentNamespace = namespaceName!;

        foreach (var marker in DefaultEntityNamespaceMarkers)
        {
            var normalizedMarker = marker.Substring(1);

            if (currentNamespace.EndsWith(normalizedMarker, StringComparison.Ordinal)
                || currentNamespace.IndexOf(marker + ".", StringComparison.Ordinal) >= 0)
            {
                return true;
            }
        }

        foreach (var configuredNamespace in options.EntityNamespaces)
        {
            if (string.Equals(currentNamespace, configuredNamespace, StringComparison.Ordinal)
                || currentNamespace.StartsWith(configuredNamespace + ".", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsEntityTypeName(INamedTypeSymbol type, DomainEntityOptions options)
    {
        return DefaultEntityTypeNames.Contains(type.Name) || options.EntityBaseTypes.Contains(type.Name);
    }

    internal sealed class DomainEntityOptionsCache
    {
        private readonly AnalyzerConfigOptionsProvider _provider;
        private readonly ConcurrentDictionary<SyntaxTree, DomainEntityOptions> _optionsBySyntaxTree = new();

        public DomainEntityOptionsCache(AnalyzerConfigOptionsProvider provider)
        {
            _provider = provider;
        }

        public DomainEntityOptions Get(SyntaxTree syntaxTree)
        {
            return _optionsBySyntaxTree.GetOrAdd(syntaxTree, CreateOptions);
        }

        private DomainEntityOptions CreateOptions(SyntaxTree syntaxTree)
        {
            return DomainEntityOptions.Create(_provider, syntaxTree);
        }
    }

    internal readonly struct DomainEntityOptions
    {
        private DomainEntityOptions(
            ImmutableArray<string> entityNamespaces,
            ImmutableHashSet<string> entityBaseTypes,
            bool allowInternalSetters)
        {
            EntityNamespaces = entityNamespaces;
            EntityBaseTypes = entityBaseTypes;
            AllowInternalSetters = allowInternalSetters;
        }

        public ImmutableArray<string> EntityNamespaces
        {
            get;
        }

        public ImmutableHashSet<string> EntityBaseTypes
        {
            get;
        }

        public bool AllowInternalSetters
        {
            get;
        }

        public static DomainEntityOptions Create(AnalyzerConfigOptionsProvider provider, SyntaxTree syntaxTree)
        {
            var options = provider.GetOptions(syntaxTree);

            return new DomainEntityOptions(
                ReadStringArray(options, EntityNamespacesOption).ToImmutableArray(),
                ReadStringArray(options, EntityBaseTypesOption).ToImmutableHashSet(StringComparer.Ordinal),
                AnalyzerConfigOptionReader.ReadBooleanOption(options, AllowInternalSettersOption, defaultValue: false));
        }

        private static IEnumerable<string> ReadStringArray(AnalyzerConfigOptions options, string optionName)
        {
            return AnalyzerConfigOptionReader.ReadStringArrayOption(
                options,
                optionName,
                ImmutableArray<string>.Empty,
                static value => value.Trim());
        }
    }
}
