using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using CSF.Analyzers.Common;
using CSF.Analyzers.Common.Common;
using CSF.Analyzers.Reliability;

namespace CSF.Analyzers.Reliability.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Rel006AvoidScopedDependencyCaptureInHostedServicesAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Reliability";
    private const string ScopedTypePatternsOption = "dotnet_diagnostic.REL006.scoped_type_patterns";
    private const string BackgroundServiceType = "Microsoft.Extensions.Hosting.BackgroundService";
    private const string HostedServiceType = "Microsoft.Extensions.Hosting.IHostedService";
    private const string DbContextType = "Microsoft.EntityFrameworkCore.DbContext";
    private const string DbContextFactoryType = "Microsoft.EntityFrameworkCore.IDbContextFactory`1";
    private const string OptionsSnapshotType = "Microsoft.Extensions.Options.IOptionsSnapshot`1";
    private const string OptionsMonitorType = "Microsoft.Extensions.Options.IOptionsMonitor`1";
    private const string OptionsType = "Microsoft.Extensions.Options.IOptions`1";
    private const string ServiceScopeFactoryType = "Microsoft.Extensions.DependencyInjection.IServiceScopeFactory";
    private const string ServiceProviderType = "System.IServiceProvider";
    private const int MaxConfiguredPatterns = 64;
    private const int MaxPatternLength = 256;

    private static readonly SymbolDisplayFormat FullyQualifiedTypeFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.AvoidScopedDependencyCaptureInHostedServices,
        title: "Evitar captura de dependencias scoped em hosted services",
        messageFormat: "Avoid capturing scoped dependency '{0}' in hosted service '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Hosted services are singletons by default. Resolve scoped dependencies inside an explicit scope instead of capturing them.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.AvoidScopedDependencyCaptureInHostedServices));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var backgroundServiceType = compilationContext.Compilation.GetTypeByMetadataName(BackgroundServiceType);
            var hostedServiceType = compilationContext.Compilation.GetTypeByMetadataName(HostedServiceType);

            if (backgroundServiceType is null || hostedServiceType is null)
            {
                return;
            }

            var symbols = new KnownSymbols(
                backgroundServiceType,
                hostedServiceType,
                compilationContext.Compilation.GetTypeByMetadataName(DbContextType),
                compilationContext.Compilation.GetTypeByMetadataName(DbContextFactoryType),
                compilationContext.Compilation.GetTypeByMetadataName(OptionsSnapshotType),
                compilationContext.Compilation.GetTypeByMetadataName(OptionsMonitorType),
                compilationContext.Compilation.GetTypeByMetadataName(OptionsType),
                compilationContext.Compilation.GetTypeByMetadataName(ServiceScopeFactoryType),
                compilationContext.Compilation.GetTypeByMetadataName(ServiceProviderType));

            var optionsCache = new ScopedDependencyOptionsCache(compilationContext.Options.AnalyzerConfigOptionsProvider);
            var testMethodAttributes = TestContextHelper.GetKnownTestMethodAttributes(compilationContext.Compilation);
            var isTestTypeCache = new ConcurrentDictionary<INamedTypeSymbol, bool>(SymbolEqualityComparer.Default);

            compilationContext.RegisterSyntaxNodeAction(
                context => AnalyzeTypeDeclaration(context, symbols, optionsCache, testMethodAttributes, isTestTypeCache),
                SyntaxKind.ClassDeclaration);
        });
    }

    private static void AnalyzeTypeDeclaration(
        SyntaxNodeAnalysisContext context,
        KnownSymbols symbols,
        ScopedDependencyOptionsCache optionsCache,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        var declaration = (ClassDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(declaration, context.CancellationToken) is not INamedTypeSymbol typeSymbol
            || !IsHostedService(typeSymbol, symbols)
            || IsTestType(typeSymbol, testMethodAttributes, isTestTypeCache))
        {
            return;
        }

        var options = optionsCache.Get(declaration.SyntaxTree);

        var constructorCapturedFields = GetConstructorCapturedFields(context, declaration);

        AnalyzeFields(context, declaration, typeSymbol, symbols, options, constructorCapturedFields);
        AnalyzeConstructors(context, declaration, typeSymbol, symbols, options);
        AnalyzePrimaryConstructorParameters(context, declaration, typeSymbol, symbols, options);
    }

    private static void AnalyzeFields(
        SyntaxNodeAnalysisContext context,
        ClassDeclarationSyntax declaration,
        INamedTypeSymbol hostedServiceType,
        KnownSymbols symbols,
        ScopedDependencyOptions options,
        ImmutableHashSet<IFieldSymbol> constructorCapturedFields)
    {
        foreach (var fieldDeclaration in declaration.Members.OfType<FieldDeclarationSyntax>())
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (fieldDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword)
                || fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword))
            {
                continue;
            }

            foreach (var variable in fieldDeclaration.Declaration.Variables)
            {
                if (context.SemanticModel.GetDeclaredSymbol(variable, context.CancellationToken) is not IFieldSymbol field
                    || constructorCapturedFields.Contains(field)
                    || !IsScopedDependency(field.Type, symbols, options))
                {
                    continue;
                }

                Report(context, field.Type, hostedServiceType, variable.Identifier.GetLocation());
            }
        }
    }

    private static void AnalyzeConstructors(
        SyntaxNodeAnalysisContext context,
        ClassDeclarationSyntax declaration,
        INamedTypeSymbol hostedServiceType,
        KnownSymbols symbols,
        ScopedDependencyOptions options)
    {
        foreach (var constructor in declaration.Members.OfType<ConstructorDeclarationSyntax>())
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            foreach (var parameterSyntax in constructor.ParameterList.Parameters)
            {
                if (context.SemanticModel.GetDeclaredSymbol(parameterSyntax, context.CancellationToken) is not IParameterSymbol parameter
                    || !IsScopedDependency(parameter.Type, symbols, options)
                    || !IsParameterCaptured(constructor, parameter, context.SemanticModel, context.CancellationToken))
                {
                    continue;
                }

                Report(context, parameter.Type, hostedServiceType, parameterSyntax.Identifier.GetLocation());
            }
        }
    }

    private static ImmutableHashSet<IFieldSymbol> GetConstructorCapturedFields(
        SyntaxNodeAnalysisContext context,
        ClassDeclarationSyntax declaration)
    {
        var builder = ImmutableHashSet.CreateBuilder<IFieldSymbol>(SymbolEqualityComparer.Default);

        foreach (var constructor in declaration.Members.OfType<ConstructorDeclarationSyntax>())
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (constructor.Body is null && constructor.ExpressionBody is null)
            {
                continue;
            }

            var nodes = constructor.Body is not null
                ? constructor.Body.DescendantNodes()
                : constructor.ExpressionBody!.DescendantNodes();

            foreach (var assignment in nodes.OfType<AssignmentExpressionSyntax>())
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                if (assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
                    && context.SemanticModel.GetSymbolInfo(UnwrapExpression(assignment.Left), context.CancellationToken).Symbol is IFieldSymbol { IsStatic: false } field)
                {
                    builder.Add(field);
                }
            }
        }

        return builder.ToImmutable();
    }

    private static void AnalyzePrimaryConstructorParameters(
        SyntaxNodeAnalysisContext context,
        ClassDeclarationSyntax declaration,
        INamedTypeSymbol hostedServiceType,
        KnownSymbols symbols,
        ScopedDependencyOptions options)
    {
        if (declaration.ParameterList is null)
        {
            return;
        }

        foreach (var parameterSyntax in declaration.ParameterList.Parameters)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (context.SemanticModel.GetDeclaredSymbol(parameterSyntax, context.CancellationToken) is not IParameterSymbol parameter
                || !IsScopedDependency(parameter.Type, symbols, options)
                || !IsPrimaryConstructorParameterUsedInInstanceMember(declaration, parameter, context.SemanticModel, context.CancellationToken))
            {
                continue;
            }

            Report(context, parameter.Type, hostedServiceType, parameterSyntax.Identifier.GetLocation());
        }
    }

    private static bool IsParameterCaptured(
        ConstructorDeclarationSyntax constructor,
        IParameterSymbol parameter,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        if (constructor.Body is null && constructor.ExpressionBody is null)
        {
            return false;
        }

        var nodes = constructor.Body is not null
            ? constructor.Body.DescendantNodes()
            : constructor.ExpressionBody!.DescendantNodes();

        foreach (var assignment in nodes.OfType<AssignmentExpressionSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
                && IsInstanceFieldOrProperty(assignment.Left, semanticModel, cancellationToken)
                && IsParameterReference(assignment.Right, parameter, semanticModel, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPrimaryConstructorParameterUsedInInstanceMember(
        ClassDeclarationSyntax declaration,
        IParameterSymbol parameter,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        foreach (var member in declaration.Members)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is ConstructorDeclarationSyntax)
            {
                continue;
            }

            var memberSymbol = semanticModel.GetDeclaredSymbol(member, cancellationToken);
            if (memberSymbol?.IsStatic == true)
            {
                continue;
            }

            foreach (var identifier in member.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (IsParameterReference(identifier, parameter, semanticModel, cancellationToken))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsParameterReference(
        ExpressionSyntax expression,
        IParameterSymbol parameter,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        expression = UnwrapExpression(expression);
        return semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol is IParameterSymbol referencedParameter
            && SymbolEqualityComparer.Default.Equals(referencedParameter, parameter);
    }

    private static bool IsInstanceFieldOrProperty(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        expression = UnwrapExpression(expression);
        var symbol = semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol;
        return symbol is IFieldSymbol { IsStatic: false } or IPropertySymbol { IsStatic: false };
    }

    private static bool IsHostedService(INamedTypeSymbol type, KnownSymbols symbols)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, symbols.BackgroundServiceType))
            {
                return true;
            }
        }

        foreach (var interfaceType in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(interfaceType, symbols.HostedServiceType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsScopedDependency(ITypeSymbol type, KnownSymbols symbols, ScopedDependencyOptions options)
    {
        if (IsKnownSafeType(type, symbols))
        {
            return false;
        }

        if (symbols.DbContextType is not null && IsOrInheritsFrom(type, symbols.DbContextType))
        {
            return true;
        }

        if (symbols.OptionsSnapshotType is not null && IsOriginalDefinition(type, symbols.OptionsSnapshotType))
        {
            return true;
        }

        if (options.Matches(type))
        {
            return true;
        }

        return false;
    }

    private static bool IsKnownSafeType(ITypeSymbol type, KnownSymbols symbols)
    {
        return IsOriginalDefinition(type, symbols.DbContextFactoryType)
            || IsOriginalDefinition(type, symbols.OptionsMonitorType)
            || IsOriginalDefinition(type, symbols.OptionsType)
            || IsSameType(type, symbols.ServiceScopeFactoryType)
            || IsSameType(type, symbols.ServiceProviderType);
    }

    private static bool IsOrInheritsFrom(ITypeSymbol type, INamedTypeSymbol baseType)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsOriginalDefinition(ITypeSymbol type, INamedTypeSymbol? originalDefinition)
    {
        return originalDefinition is not null
            && type is INamedTypeSymbol namedType
            && SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, originalDefinition);
    }

    private static bool IsSameType(ITypeSymbol type, INamedTypeSymbol? expectedType)
    {
        return expectedType is not null && SymbolEqualityComparer.Default.Equals(type, expectedType);
    }

    private static bool IsTestType(
        INamedTypeSymbol type,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        return !testMethodAttributes.IsDefaultOrEmpty
            && TestContextHelper.IsTestType(type, testMethodAttributes, isTestTypeCache);
    }

    private static void Report(
        SyntaxNodeAnalysisContext context,
        ITypeSymbol dependencyType,
        INamedTypeSymbol hostedServiceType,
        Location location)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            location,
            GetTypeDisplayName(dependencyType),
            GetTypeDisplayName(hostedServiceType)));
    }

    private static string GetTypeDisplayName(ITypeSymbol type)
    {
        return type.ToDisplayString(FullyQualifiedTypeFormat);
    }

    private static ExpressionSyntax UnwrapExpression(ExpressionSyntax expression)
    {
        var current = expression;

        while (current is ParenthesizedExpressionSyntax parenthesized)
        {
            current = parenthesized.Expression;
        }

        return current;
    }

    private readonly struct KnownSymbols
    {
        public KnownSymbols(
            INamedTypeSymbol backgroundServiceType,
            INamedTypeSymbol hostedServiceType,
            INamedTypeSymbol? dbContextType,
            INamedTypeSymbol? dbContextFactoryType,
            INamedTypeSymbol? optionsSnapshotType,
            INamedTypeSymbol? optionsMonitorType,
            INamedTypeSymbol? optionsType,
            INamedTypeSymbol? serviceScopeFactoryType,
            INamedTypeSymbol? serviceProviderType)
        {
            BackgroundServiceType = backgroundServiceType;
            HostedServiceType = hostedServiceType;
            DbContextType = dbContextType;
            DbContextFactoryType = dbContextFactoryType;
            OptionsSnapshotType = optionsSnapshotType;
            OptionsMonitorType = optionsMonitorType;
            OptionsType = optionsType;
            ServiceScopeFactoryType = serviceScopeFactoryType;
            ServiceProviderType = serviceProviderType;
        }

        public INamedTypeSymbol BackgroundServiceType { get; }

        public INamedTypeSymbol HostedServiceType { get; }

        public INamedTypeSymbol? DbContextType { get; }

        public INamedTypeSymbol? DbContextFactoryType { get; }

        public INamedTypeSymbol? OptionsSnapshotType { get; }

        public INamedTypeSymbol? OptionsMonitorType { get; }

        public INamedTypeSymbol? OptionsType { get; }

        public INamedTypeSymbol? ServiceScopeFactoryType { get; }

        public INamedTypeSymbol? ServiceProviderType { get; }
    }

    private sealed class ScopedDependencyOptionsCache
    {
        private readonly AnalyzerConfigOptionsProvider _provider;
        private readonly ConcurrentDictionary<SyntaxTree, ScopedDependencyOptions> _optionsBySyntaxTree = new();

        public ScopedDependencyOptionsCache(AnalyzerConfigOptionsProvider provider)
        {
            _provider = provider;
        }

        public ScopedDependencyOptions Get(SyntaxTree syntaxTree)
        {
            return _optionsBySyntaxTree.GetOrAdd(syntaxTree, CreateOptions);
        }

        private ScopedDependencyOptions CreateOptions(SyntaxTree syntaxTree)
        {
            return ScopedDependencyOptions.Create(_provider.GetOptions(syntaxTree));
        }
    }

    private readonly struct ScopedDependencyOptions
    {
        private ScopedDependencyOptions(ImmutableArray<string> scopedTypePatterns)
        {
            ScopedTypePatterns = scopedTypePatterns;
        }

        private ImmutableArray<string> ScopedTypePatterns { get; }

        public static ScopedDependencyOptions Create(AnalyzerConfigOptions options)
        {
            if (!TryGetScopedTypePatterns(options, out var configuredValue)
                || string.IsNullOrWhiteSpace(configuredValue))
            {
                return new ScopedDependencyOptions(ImmutableArray<string>.Empty);
            }

            var builder = ImmutableArray.CreateBuilder<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var rawPattern in configuredValue.Split(';'))
            {
                if (builder.Count >= MaxConfiguredPatterns)
                {
                    break;
                }

                var pattern = rawPattern.Trim();
                if (!IsValidPattern(pattern) || !seen.Add(pattern))
                {
                    continue;
                }

                builder.Add(pattern);
            }

            return new ScopedDependencyOptions(builder.ToImmutable());
        }

        private static bool TryGetScopedTypePatterns(AnalyzerConfigOptions options, out string configuredValue)
        {
            if (options.TryGetValue(ScopedTypePatternsOption, out var exactValue)
                || options.TryGetValue("dotnet_diagnostic.rel006.scoped_type_patterns", out exactValue))
            {
                configuredValue = exactValue;
                return true;
            }

            configuredValue = string.Empty;
            return false;
        }

        public bool Matches(ITypeSymbol type)
        {
            if (ScopedTypePatterns.IsDefaultOrEmpty)
            {
                return false;
            }

            foreach (var typeName in GetConfiguredTypeNames(type))
            {
                foreach (var pattern in ScopedTypePatterns)
                {
                    if (WildcardPatternMatcher.Matches(typeName, pattern, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static ImmutableArray<string> GetConfiguredTypeNames(ITypeSymbol type)
        {
            var builder = ImmutableArray.CreateBuilder<string>();
            builder.Add(GetTypeDisplayName(type));

            if (type is INamedTypeSymbol namedType)
            {
                var metadataName = GetMetadataQualifiedName(namedType);
                if (!builder.Contains(metadataName, StringComparer.Ordinal))
                {
                    builder.Add(metadataName);
                }
            }

            return builder.ToImmutable();
        }

        private static string GetMetadataQualifiedName(INamedTypeSymbol type)
        {
            var parts = new Stack<string>();

            for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
            {
                parts.Push(GetMetadataNameWithoutArity(current));
            }

            var namespaceName = type.ContainingNamespace?.IsGlobalNamespace == false
                ? type.ContainingNamespace.ToDisplayString()
                : string.Empty;

            var typeName = string.Join(".", parts);
            return namespaceName.Length == 0 ? typeName : namespaceName + "." + typeName;
        }

        private static string GetMetadataNameWithoutArity(INamedTypeSymbol type)
        {
            var metadataName = type.MetadataName;
            var arityIndex = metadataName.IndexOf('`');
            return arityIndex < 0 ? metadataName : metadataName.Substring(0, arityIndex);
        }

        private static bool IsValidPattern(string pattern)
        {
            if (pattern.Length == 0 || pattern.Length > MaxPatternLength)
            {
                return false;
            }

            foreach (var character in pattern)
            {
                if (char.IsLetterOrDigit(character)
                    || character is '.' or '_' or '*' or '<' or '>' or ',')
                {
                    continue;
                }

                return false;
            }

            return true;
        }
    }
}
