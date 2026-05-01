using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Swa.Analyzers.Core.Common;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch029ProhibitPublicSettersInDomainEntitiesAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Design";
    private const string EntityNamespacesOption = "dotnet_diagnostic.ARCH029.entity_namespaces";
    private const string EntityBaseTypesOption = "dotnet_diagnostic.ARCH029.entity_base_types";
    private const string AllowInternalSettersOption = "dotnet_diagnostic.ARCH029.allow_internal_setters";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.ProhibitPublicSettersInDomainEntities,
        title: "Proibir setters publicos em entidades de dominio",
        messageFormat: "Domain entity property '{0}' exposes a public setter. Prefer private set and behavior methods to preserve invariants.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Domain entities should protect invariants by avoiding publicly mutable state.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.ProhibitPublicSettersInDomainEntities));

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

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var optionsCache = new DomainEntityOptionsCache(compilationContext.Options.AnalyzerConfigOptionsProvider);

            compilationContext.RegisterSyntaxNodeAction(
                context => AnalyzeProperty(context, optionsCache),
                SyntaxKind.PropertyDeclaration);
        });
    }

    private static void AnalyzeProperty(
        SyntaxNodeAnalysisContext context,
        DomainEntityOptionsCache optionsCache)
    {
        var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;

        if (propertyDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
        {
            return;
        }

        if (!TryGetSetAccessor(propertyDeclaration, out var setAccessor)
            || setAccessor.IsKind(SyntaxKind.InitAccessorDeclaration))
        {
            return;
        }

        var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclaration, context.CancellationToken);
        if (propertySymbol?.ContainingType is not INamedTypeSymbol containingType)
        {
            return;
        }

        if (containingType.TypeKind != TypeKind.Class || containingType.IsRecord)
        {
            return;
        }

        if (IsTestType(containingType, propertyDeclaration.SyntaxTree.FilePath))
        {
            return;
        }

        var options = optionsCache.Get(propertyDeclaration.SyntaxTree);
        if (!IsDomainEntity(containingType, options))
        {
            return;
        }

        if (!ShouldReportSetter(propertySymbol.SetMethod, options))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            setAccessor.Keyword.GetLocation(),
            propertySymbol.Name));
    }

    private static bool TryGetSetAccessor(
        PropertyDeclarationSyntax propertyDeclaration,
        out AccessorDeclarationSyntax accessor)
    {
        foreach (var candidate in propertyDeclaration.AccessorList?.Accessors ?? default)
        {
            if (candidate.IsKind(SyntaxKind.SetAccessorDeclaration)
                || candidate.IsKind(SyntaxKind.InitAccessorDeclaration))
            {
                accessor = candidate;
                return true;
            }
        }

        accessor = null!;
        return false;
    }

    private static bool ShouldReportSetter(IMethodSymbol? setMethod, DomainEntityOptions options)
    {
        if (setMethod is null)
        {
            return false;
        }

        if (setMethod.DeclaredAccessibility == Accessibility.Public)
        {
            return true;
        }

        if (options.AllowInternalSetters)
        {
            return false;
        }

        return setMethod.DeclaredAccessibility is Accessibility.Internal or Accessibility.ProtectedOrInternal;
    }

    private static bool IsDomainEntity(INamedTypeSymbol type, DomainEntityOptions options)
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

    private static bool IsEntityNamespace(string? namespaceName, DomainEntityOptions options)
    {
        if (string.IsNullOrWhiteSpace(namespaceName))
        {
            return false;
        }

        var currentNamespace = namespaceName!;

        foreach (var marker in DefaultEntityNamespaceMarkers)
        {
            if (currentNamespace.EndsWith(marker.Substring(1), StringComparison.Ordinal)
                || currentNamespace.IndexOf(marker, StringComparison.Ordinal) >= 0)
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

    private static bool IsTestType(INamedTypeSymbol type, string filePath)
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

    private sealed class DomainEntityOptionsCache
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

    private readonly struct DomainEntityOptions
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
                ReadBoolean(options, AllowInternalSettersOption, defaultValue: false));
        }

        private static IEnumerable<string> ReadStringArray(AnalyzerConfigOptions options, string optionName)
        {
            if (!options.TryGetValue(optionName, out var configuredValue)
                || !JsonStringArrayOptionParser.TryParse(configuredValue, out var parsedValues))
            {
                yield break;
            }

            foreach (var parsedValue in parsedValues)
            {
                var normalized = parsedValue.Trim();

                if (normalized.Length > 0)
                {
                    yield return normalized;
                }
            }
        }

        private static bool ReadBoolean(AnalyzerConfigOptions options, string optionName, bool defaultValue)
        {
            return options.TryGetValue(optionName, out var configuredValue)
                && bool.TryParse(configuredValue.Trim(), out var parsedValue)
                    ? parsedValue
                    : defaultValue;
        }

    }
}
