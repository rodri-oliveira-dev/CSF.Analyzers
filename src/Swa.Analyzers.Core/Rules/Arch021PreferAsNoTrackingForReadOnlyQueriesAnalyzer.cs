using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch021PreferAsNoTrackingForReadOnlyQueriesAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Performance";
    private const string EfCoreNamespace = "Microsoft.EntityFrameworkCore";
    private const string EfCoreQueryableExtensionsType = "Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions";
    private const string EfCoreDbSetType = "Microsoft.EntityFrameworkCore.DbSet`1";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.PreferAsNoTrackingForReadOnlyQueries,
        title: "Preferir AsNoTracking em consultas somente leitura",
        messageFormat: "Prefer AsNoTracking() for read-only EF Core query materialized by '{0}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Read-only EF Core queries should opt out of change tracking with AsNoTracking() when no entity update is persisted in the same method.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.PreferAsNoTrackingForReadOnlyQueries));

    private static readonly ImmutableHashSet<string> MaterializerMethodNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "ToListAsync",
        "FirstOrDefaultAsync",
        "SingleOrDefaultAsync");

    private static readonly ImmutableHashSet<string> TrackingMethodNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "AsNoTracking",
        "AsTracking");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var dbSetType = compilationContext.Compilation.GetTypeByMetadataName(EfCoreDbSetType);
            var efCoreQueryableExtensionsType = compilationContext.Compilation.GetTypeByMetadataName(EfCoreQueryableExtensionsType);

            if (dbSetType is null || efCoreQueryableExtensionsType is null)
            {
                return;
            }

            var testMethodAttributes = TestContextHelper.GetKnownTestMethodAttributes(compilationContext.Compilation);
            var isTestTypeCache = new ConcurrentDictionary<INamedTypeSymbol, bool>(SymbolEqualityComparer.Default);

            compilationContext.RegisterSyntaxNodeAction(
                context => AnalyzeInvocation(context, dbSetType, efCoreQueryableExtensionsType, testMethodAttributes, isTestTypeCache),
                SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol dbSetType,
        INamedTypeSymbol efCoreQueryableExtensionsType,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol targetMethod)
        {
            return;
        }

        var originalTargetMethod = targetMethod.ReducedFrom ?? targetMethod;
        if (!IsEfCoreMaterializer(originalTargetMethod, efCoreQueryableExtensionsType))
        {
            return;
        }

        var containingSymbol = context.SemanticModel.GetEnclosingSymbol(invocation.SpanStart, context.CancellationToken);
        if (containingSymbol is not null
            && !testMethodAttributes.IsDefaultOrEmpty
            && TestContextHelper.IsWithinTestContext(containingSymbol, testMethodAttributes, isTestTypeCache))
        {
            return;
        }

        if (!TryGetInvocationReceiver(invocation, out var receiver))
        {
            return;
        }

        var queryChain = InspectQueryChain(receiver, context.SemanticModel, context.CancellationToken, dbSetType, efCoreQueryableExtensionsType);
        if (!queryChain.StartsFromDbSet || queryChain.HasExplicitTrackingDecision)
        {
            return;
        }

        var materializedType = TryGetMaterializedType(targetMethod);
        if (queryChain.RootEntityType is not null
            && materializedType is not null
            && !SymbolEqualityComparer.Default.Equals(queryChain.RootEntityType, materializedType))
        {
            return;
        }

        var containingMethod = invocation.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>();
        if (containingMethod is not null
            && HasEntityMutationPersistedInMethod(containingMethod, context.SemanticModel, context.CancellationToken))
        {
            return;
        }

        var containingType = invocation.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (containingType is not null
            && HasGlobalNoTrackingConfiguration(containingType, context.SemanticModel, context.CancellationToken))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            GetInvocationTargetLocation(invocation),
            targetMethod.Name));
    }

    private static bool IsEfCoreMaterializer(IMethodSymbol method, INamedTypeSymbol efCoreQueryableExtensionsType)
    {
        return MaterializerMethodNames.Contains(method.Name)
            && SymbolEqualityComparer.Default.Equals(method.ContainingType, efCoreQueryableExtensionsType);
    }

    private static QueryChainInfo InspectQueryChain(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken,
        INamedTypeSymbol dbSetType,
        INamedTypeSymbol efCoreQueryableExtensionsType)
    {
        var current = UnwrapExpression(expression);
        var info = new QueryChainInfo();

        while (current is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var type = semanticModel.GetTypeInfo(current, cancellationToken).Type;
            if (IsDbSet(type, dbSetType))
            {
                info.StartsFromDbSet = true;
                info.RootEntityType = GetDbSetEntityType(type);
            }

            if (current is InvocationExpressionSyntax invocation)
            {
                if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol method)
                {
                    var originalMethod = method.ReducedFrom ?? method;
                    if (TrackingMethodNames.Contains(originalMethod.Name)
                        && SymbolEqualityComparer.Default.Equals(originalMethod.ContainingType, efCoreQueryableExtensionsType))
                    {
                        info.HasExplicitTrackingDecision = true;
                    }
                }

                current = TryGetInvocationReceiver(invocation, out var receiver)
                    ? UnwrapExpression(receiver)
                    : null;
                continue;
            }

            if (current is MemberAccessExpressionSyntax memberAccess)
            {
                current = UnwrapExpression(memberAccess.Expression);
                continue;
            }

            break;
        }

        return info;
    }

    private static bool IsDbSet(ITypeSymbol? type, INamedTypeSymbol dbSetType)
    {
        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        return SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, dbSetType);
    }

    private static ITypeSymbol? GetDbSetEntityType(ITypeSymbol? type)
    {
        return type is INamedTypeSymbol { TypeArguments.Length: 1 } namedType
            ? namedType.TypeArguments[0]
            : null;
    }

    private static ITypeSymbol? TryGetMaterializedType(IMethodSymbol materializer)
    {
        return materializer.TypeArguments.Length == 1
            ? materializer.TypeArguments[0]
            : null;
    }

    private static bool HasEntityMutationPersistedInMethod(
        BaseMethodDeclarationSyntax containingMethod,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        var hasSaveChangesCall = false;
        var hasMemberAssignment = false;

        foreach (var invocation in containingMethod.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol method
                && IsSaveChangesMethod(method))
            {
                hasSaveChangesCall = true;
                break;
            }
        }

        if (!hasSaveChangesCall)
        {
            return false;
        }

        foreach (var assignment in containingMethod.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (assignment.Left is MemberAccessExpressionSyntax or MemberBindingExpressionSyntax)
            {
                hasMemberAssignment = true;
                break;
            }
        }

        return hasMemberAssignment || HasEntityStateMutationCall(containingMethod, semanticModel, cancellationToken);
    }

    private static bool HasEntityStateMutationCall(
        BaseMethodDeclarationSyntax containingMethod,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        foreach (var invocation in containingMethod.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol method
                && IsEntityStateMutationMethod(method))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsEntityStateMutationMethod(IMethodSymbol method)
    {
        return (string.Equals(method.Name, "Attach", StringComparison.Ordinal)
                || string.Equals(method.Name, "Update", StringComparison.Ordinal)
                || string.Equals(method.Name, "Remove", StringComparison.Ordinal))
            && IsEfCoreNamespace(method.ContainingType?.ContainingNamespace);
    }

    private static bool IsSaveChangesMethod(IMethodSymbol method)
    {
        return (string.Equals(method.Name, "SaveChanges", StringComparison.Ordinal)
                || string.Equals(method.Name, "SaveChangesAsync", StringComparison.Ordinal))
            && IsEfCoreNamespace(method.ContainingType?.ContainingNamespace);
    }

    private static bool HasGlobalNoTrackingConfiguration(
        TypeDeclarationSyntax containingType,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        foreach (var invocation in containingType.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method
                || !string.Equals(method.Name, "UseQueryTrackingBehavior", StringComparison.Ordinal))
            {
                continue;
            }

            if (invocation.ArgumentList.Arguments.Any(argument => IsNoTrackingExpression(argument.Expression, semanticModel, cancellationToken)))
            {
                return true;
            }
        }

        foreach (var assignment in containingType.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (assignment.Left.ToString().EndsWith(".QueryTrackingBehavior", StringComparison.Ordinal)
                && IsNoTrackingExpression(assignment.Right, semanticModel, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNoTrackingExpression(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        if (semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol is not IFieldSymbol field)
        {
            return false;
        }

        return string.Equals(field.Name, "NoTracking", StringComparison.Ordinal)
            && string.Equals(field.ContainingType.Name, "QueryTrackingBehavior", StringComparison.Ordinal)
            && IsEfCoreNamespace(field.ContainingType.ContainingNamespace);
    }

    private static bool IsEfCoreNamespace(INamespaceSymbol? namespaceSymbol)
    {
        return string.Equals(namespaceSymbol?.ToDisplayString(), EfCoreNamespace, StringComparison.Ordinal);
    }

    private static bool TryGetInvocationReceiver(InvocationExpressionSyntax invocation, out ExpressionSyntax receiver)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            receiver = memberAccess.Expression;
            return true;
        }

        receiver = null!;
        return false;
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

    private static Location GetInvocationTargetLocation(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression is MemberAccessExpressionSyntax memberAccess
            ? memberAccess.Name.GetLocation()
            : invocation.Expression.GetLocation();
    }

    private sealed class QueryChainInfo
    {
        public bool StartsFromDbSet { get; set; }

        public bool HasExplicitTrackingDecision { get; set; }

        public ITypeSymbol? RootEntityType { get; set; }
    }
}
