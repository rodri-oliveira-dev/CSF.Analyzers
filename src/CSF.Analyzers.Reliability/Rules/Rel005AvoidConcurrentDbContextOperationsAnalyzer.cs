using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using Microsoft.CodeAnalysis.CSharp;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.CodeAnalysis.Diagnostics;

using CSF.Analyzers.Common;
using CSF.Analyzers.Reliability;

namespace CSF.Analyzers.Reliability.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Rel005AvoidConcurrentDbContextOperationsAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Reliability";
    private const string DbContextType = "Microsoft.EntityFrameworkCore.DbContext";
    private const string EfCoreQueryableExtensionsType = "Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions";
    private const string TaskType = "System.Threading.Tasks.Task";
    private const string ParallelType = "System.Threading.Tasks.Parallel";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.AvoidConcurrentDbContextOperations,
        title: "Evitar operacoes concorrentes no mesmo DbContext",
        messageFormat: "Avoid running EF Core operations concurrently on DbContext instance '{0}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A DbContext instance does not support multiple parallel EF Core operations. Use separate contexts or run the operations sequentially.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.AvoidConcurrentDbContextOperations));

    private static readonly ImmutableHashSet<string> AsyncQueryOperationNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "ToListAsync",
        "ToArrayAsync",
        "FirstAsync",
        "FirstOrDefaultAsync",
        "SingleAsync",
        "SingleOrDefaultAsync",
        "AnyAsync",
        "AllAsync",
        "CountAsync",
        "LongCountAsync",
        "SumAsync",
        "AverageAsync",
        "MinAsync",
        "MaxAsync",
        "ForEachAsync",
        "LoadAsync");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var dbContextType = compilationContext.Compilation.GetTypeByMetadataName(DbContextType);
            var efCoreQueryableExtensionsType = compilationContext.Compilation.GetTypeByMetadataName(EfCoreQueryableExtensionsType);
            var taskType = compilationContext.Compilation.GetTypeByMetadataName(TaskType);
            var parallelType = compilationContext.Compilation.GetTypeByMetadataName(ParallelType);

            if (dbContextType is null || efCoreQueryableExtensionsType is null)
            {
                return;
            }

            var testMethodAttributes = TestContextHelper.GetKnownTestMethodAttributes(compilationContext.Compilation);
            var isTestTypeCache = new ConcurrentDictionary<INamedTypeSymbol, bool>(SymbolEqualityComparer.Default);

            compilationContext.RegisterSyntaxNodeAction(
                context => AnalyzeInvocation(
                    context,
                    dbContextType,
                    efCoreQueryableExtensionsType,
                    taskType,
                    parallelType,
                    testMethodAttributes,
                    isTestTypeCache),
                SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol dbContextType,
        INamedTypeSymbol efCoreQueryableExtensionsType,
        INamedTypeSymbol? taskType,
        INamedTypeSymbol? parallelType,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol targetMethod)
        {
            return;
        }

        var originalTargetMethod = targetMethod.ReducedFrom ?? targetMethod;

        if (taskType is not null && IsTaskWhenAll(originalTargetMethod, taskType))
        {
            AnalyzeTaskWhenAll(
                context,
                invocation,
                dbContextType,
                efCoreQueryableExtensionsType,
                testMethodAttributes,
                isTestTypeCache);
            return;
        }

        if (parallelType is not null && IsParallelForEachAsync(originalTargetMethod, parallelType))
        {
            AnalyzeParallelForEachAsync(
                context,
                invocation,
                dbContextType,
                efCoreQueryableExtensionsType,
                testMethodAttributes,
                isTestTypeCache);
        }
    }

    private static void AnalyzeTaskWhenAll(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax whenAllInvocation,
        INamedTypeSymbol dbContextType,
        INamedTypeSymbol efCoreQueryableExtensionsType,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        if (IsWithinTestContext(context, whenAllInvocation, testMethodAttributes, isTestTypeCache))
        {
            return;
        }

        var operations = ImmutableArray.CreateBuilder<DbContextOperation>();

        foreach (var argument in whenAllInvocation.ArgumentList.Arguments)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            AddOperationsFromExpression(argument.Expression, context, dbContextType, efCoreQueryableExtensionsType, operations);
        }

        var duplicateRoot = FindDuplicateRoot(operations);
        if (duplicateRoot is null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, GetInvocationTargetLocation(whenAllInvocation), duplicateRoot.Name));
    }

    private static void AnalyzeParallelForEachAsync(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax parallelInvocation,
        INamedTypeSymbol dbContextType,
        INamedTypeSymbol efCoreQueryableExtensionsType,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        if (IsWithinTestContext(context, parallelInvocation, testMethodAttributes, isTestTypeCache)
            || !TryGetParallelDelegate(parallelInvocation, out var delegateExpression))
        {
            return;
        }

        foreach (var operationInvocation in delegateExpression.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (!TryCreateDbContextOperation(
                    operationInvocation,
                    context.SemanticModel,
                    context.CancellationToken,
                    dbContextType,
                    efCoreQueryableExtensionsType,
                    out var operation)
                || IsDeclaredInside(operation.RootSymbol, delegateExpression))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                GetInvocationTargetLocation(operation.Invocation),
                operation.RootSymbol.Name));
        }
    }

    private static void AddOperationsFromExpression(
        ExpressionSyntax expression,
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol dbContextType,
        INamedTypeSymbol efCoreQueryableExtensionsType,
        ImmutableArray<DbContextOperation>.Builder operations)
    {
        var unwrapped = UnwrapExpression(expression);

        foreach (var operation in GetOperationsFromExpression(
                     unwrapped,
                     context.SemanticModel,
                     context.CancellationToken,
                     dbContextType,
                     efCoreQueryableExtensionsType))
        {
            operations.Add(operation);
        }

        if (unwrapped is IdentifierNameSyntax identifier
            && TryGetLocalTaskOperations(
                identifier,
                context.SemanticModel,
                context.CancellationToken,
                dbContextType,
                efCoreQueryableExtensionsType,
                out var localOperations))
        {
            operations.AddRange(localOperations);
        }
    }

    private static IEnumerable<DbContextOperation> GetOperationsFromExpression(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken,
        INamedTypeSymbol dbContextType,
        INamedTypeSymbol efCoreQueryableExtensionsType)
    {
        if (expression is CollectionExpressionSyntax collectionExpression)
        {
            foreach (var element in collectionExpression.Elements)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (element is ExpressionElementSyntax expressionElement)
                {
                    foreach (var operation in GetOperationsFromExpression(
                                 expressionElement.Expression,
                                 semanticModel,
                                 cancellationToken,
                                 dbContextType,
                                 efCoreQueryableExtensionsType))
                    {
                        yield return operation;
                    }
                }
            }

            yield break;
        }

        if (expression is ArrayCreationExpressionSyntax arrayCreation)
        {
            foreach (var expressionNode in arrayCreation.Initializer?.Expressions ?? [])
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var operation in GetOperationsFromExpression(
                             expressionNode,
                             semanticModel,
                             cancellationToken,
                             dbContextType,
                             efCoreQueryableExtensionsType))
                {
                    yield return operation;
                }
            }

            yield break;
        }

        if (expression is ImplicitArrayCreationExpressionSyntax implicitArrayCreation)
        {
            foreach (var expressionNode in implicitArrayCreation.Initializer.Expressions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var operation in GetOperationsFromExpression(
                             expressionNode,
                             semanticModel,
                             cancellationToken,
                             dbContextType,
                             efCoreQueryableExtensionsType))
                {
                    yield return operation;
                }
            }

            yield break;
        }

        if (expression is InvocationExpressionSyntax invocation
            && TryCreateDbContextOperation(
                invocation,
                semanticModel,
                cancellationToken,
                dbContextType,
                efCoreQueryableExtensionsType,
                out var directOperation))
        {
            yield return directOperation;
        }

        foreach (var descendantInvocation in expression.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (TryCreateDbContextOperation(
                descendantInvocation,
                semanticModel,
                cancellationToken,
                dbContextType,
                efCoreQueryableExtensionsType,
                out var nestedOperation))
            {
                yield return nestedOperation;
            }
        }
    }

    private static bool TryGetLocalTaskOperations(
        IdentifierNameSyntax identifier,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken,
        INamedTypeSymbol dbContextType,
        INamedTypeSymbol efCoreQueryableExtensionsType,
        out ImmutableArray<DbContextOperation> operations)
    {
        operations = ImmutableArray<DbContextOperation>.Empty;

        if (semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol is not ILocalSymbol localSymbol
            || identifier.FirstAncestorOrSelf<StatementSyntax>() is not { } currentStatement)
        {
            return false;
        }

        foreach (var statement in GetPriorStatements(currentStatement))
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var declarator in statement.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            {
                if (declarator.Initializer?.Value is null
                    || !SymbolEqualityComparer.Default.Equals(
                        semanticModel.GetDeclaredSymbol(declarator, cancellationToken),
                        localSymbol))
                {
                    continue;
                }

                operations = GetOperationsFromExpression(
                        UnwrapExpression(declarator.Initializer.Value),
                        semanticModel,
                        cancellationToken,
                        dbContextType,
                        efCoreQueryableExtensionsType)
                    .ToImmutableArray();

                return operations.Length > 0;
            }
        }

        return false;
    }

    private static IEnumerable<StatementSyntax> GetPriorStatements(StatementSyntax statement)
    {
        for (SyntaxNode? currentStatement = statement; currentStatement is StatementSyntax current; currentStatement = current.Parent?.FirstAncestorOrSelf<StatementSyntax>())
        {
            if (current.Parent is not BlockSyntax block)
            {
                continue;
            }

            var index = block.Statements.IndexOf(current);
            for (var i = index - 1; i >= 0; i--)
            {
                yield return block.Statements[i];
            }
        }
    }

    private static bool TryCreateDbContextOperation(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken,
        INamedTypeSymbol dbContextType,
        INamedTypeSymbol efCoreQueryableExtensionsType,
        out DbContextOperation operation)
    {
        operation = default;

        if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol targetMethod)
        {
            return false;
        }

        var originalTargetMethod = targetMethod.ReducedFrom ?? targetMethod;

        if (IsEfCoreAsyncQueryOperation(originalTargetMethod, efCoreQueryableExtensionsType))
        {
            if (!TryGetEfCoreQueryReceiver(invocation, targetMethod, out var receiver)
                || !TryGetRootDbContextSymbol(receiver, semanticModel, cancellationToken, dbContextType, out var rootSymbol))
            {
                return false;
            }

            operation = new DbContextOperation(invocation, rootSymbol);
            return true;
        }

        if (IsDbContextSaveChangesAsync(originalTargetMethod, dbContextType)
            && TryGetInvocationReceiver(invocation, out var saveReceiver)
            && TryGetRootDbContextSymbol(saveReceiver, semanticModel, cancellationToken, dbContextType, out var saveRootSymbol))
        {
            operation = new DbContextOperation(invocation, saveRootSymbol);
            return true;
        }

        return false;
    }

    private static bool TryGetRootDbContextSymbol(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken,
        INamedTypeSymbol dbContextType,
        out ISymbol rootSymbol)
    {
        ExpressionSyntax? current = UnwrapExpression(expression);

        while (current is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var symbol = semanticModel.GetSymbolInfo(current, cancellationToken).Symbol;
            if (symbol is ILocalSymbol or IParameterSymbol or IFieldSymbol or IPropertySymbol)
            {
                var type = GetSymbolType(symbol);
                if (IsDbContext(type, dbContextType))
                {
                    rootSymbol = symbol;
                    return true;
                }
            }

            if (current is InvocationExpressionSyntax invocation)
            {
                if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol method
                    && string.Equals(method.Name, "Set", StringComparison.Ordinal)
                    && IsDbContext(method.ContainingType, dbContextType)
                    && TryGetInvocationReceiver(invocation, out var setReceiver))
                {
                    current = UnwrapExpression(setReceiver);
                    continue;
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

        rootSymbol = null!;
        return false;
    }

    private static ISymbol? FindDuplicateRoot(ImmutableArray<DbContextOperation>.Builder operations)
    {
        for (var i = 0; i < operations.Count; i++)
        {
            for (var j = i + 1; j < operations.Count; j++)
            {
                if (SymbolEqualityComparer.Default.Equals(operations[i].RootSymbol, operations[j].RootSymbol))
                {
                    return operations[i].RootSymbol;
                }
            }
        }

        return null;
    }

    private static bool IsTaskWhenAll(IMethodSymbol method, INamedTypeSymbol taskType)
    {
        return string.Equals(method.Name, "WhenAll", StringComparison.Ordinal)
            && SymbolEqualityComparer.Default.Equals(method.ContainingType, taskType);
    }

    private static bool IsParallelForEachAsync(IMethodSymbol method, INamedTypeSymbol parallelType)
    {
        return string.Equals(method.Name, "ForEachAsync", StringComparison.Ordinal)
            && SymbolEqualityComparer.Default.Equals(method.ContainingType, parallelType);
    }

    private static bool IsEfCoreAsyncQueryOperation(IMethodSymbol method, INamedTypeSymbol efCoreQueryableExtensionsType)
    {
        return AsyncQueryOperationNames.Contains(method.Name)
            && SymbolEqualityComparer.Default.Equals(method.ContainingType, efCoreQueryableExtensionsType);
    }

    private static bool IsDbContextSaveChangesAsync(IMethodSymbol method, INamedTypeSymbol dbContextType)
    {
        return string.Equals(method.Name, "SaveChangesAsync", StringComparison.Ordinal)
            && IsDbContext(method.ContainingType, dbContextType);
    }

    private static bool IsDbContext(ITypeSymbol? type, INamedTypeSymbol dbContextType)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, dbContextType))
            {
                return true;
            }
        }

        return false;
    }

    private static ITypeSymbol? GetSymbolType(ISymbol symbol)
    {
        return symbol switch
        {
            ILocalSymbol local => local.Type,
            IParameterSymbol parameter => parameter.Type,
            IFieldSymbol field => field.Type,
            IPropertySymbol property => property.Type,
            _ => null,
        };
    }

    private static bool TryGetParallelDelegate(InvocationExpressionSyntax invocation, out AnonymousFunctionExpressionSyntax delegateExpression)
    {
        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            if (UnwrapExpression(argument.Expression) is AnonymousFunctionExpressionSyntax anonymousFunction)
            {
                delegateExpression = anonymousFunction;
                return true;
            }
        }

        delegateExpression = null!;
        return false;
    }

    private static bool TryGetEfCoreQueryReceiver(InvocationExpressionSyntax invocation, IMethodSymbol targetMethod, out ExpressionSyntax receiver)
    {
        if (targetMethod.ReducedFrom is null && targetMethod.IsExtensionMethod && invocation.ArgumentList.Arguments.Count > 0)
        {
            receiver = invocation.ArgumentList.Arguments[0].Expression;
            return true;
        }

        if (TryGetInvocationReceiver(invocation, out receiver))
        {
            return true;
        }

        if (invocation.ArgumentList.Arguments.Count > 0)
        {
            receiver = invocation.ArgumentList.Arguments[0].Expression;
            return true;
        }

        receiver = null!;
        return false;
    }

    private static bool IsDeclaredInside(ISymbol symbol, SyntaxNode scope)
    {
        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            var syntax = syntaxReference.GetSyntax();
            if (scope.Span.Contains(syntax.SpanStart))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsWithinTestContext(
        SyntaxNodeAnalysisContext context,
        SyntaxNode node,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        var containingSymbol = context.SemanticModel.GetEnclosingSymbol(node.SpanStart, context.CancellationToken);
        return containingSymbol is not null
            && !testMethodAttributes.IsDefaultOrEmpty
            && TestContextHelper.IsWithinTestContext(containingSymbol, testMethodAttributes, isTestTypeCache);
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

    private readonly struct DbContextOperation
    {
        public DbContextOperation(InvocationExpressionSyntax invocation, ISymbol rootSymbol)
        {
            Invocation = invocation;
            RootSymbol = rootSymbol;
        }

        public InvocationExpressionSyntax Invocation { get; }

        public ISymbol RootSymbol { get; }
    }
}
