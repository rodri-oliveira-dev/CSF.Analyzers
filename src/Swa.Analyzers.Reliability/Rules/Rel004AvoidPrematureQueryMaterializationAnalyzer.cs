using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using Microsoft.CodeAnalysis.CSharp;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.CodeAnalysis.Diagnostics;

using Swa.Analyzers.Common;
using Swa.Analyzers.Reliability;

namespace Swa.Analyzers.Reliability.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Rel004AvoidPrematureQueryMaterializationAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Performance";
    private const string EfCoreDbSetType = "Microsoft.EntityFrameworkCore.DbSet`1";
    private const string EfCoreQueryableExtensionsType = "Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.AvoidPrematureQueryMaterialization,
        title: "Evitar materializacao antes de filtro ou projecao",
        messageFormat: "Avoid '{0}' before '{1}'. Compose the query before materializing it.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Queries should apply filters, projections and pagination before materialization so EF Core can translate the work to the database.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.AvoidPrematureQueryMaterialization));

    private static readonly ImmutableHashSet<string> SynchronousMaterializerNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "ToList",
        "ToArray");

    private static readonly ImmutableHashSet<string> AsyncMaterializerNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "ToListAsync");

    private static readonly ImmutableHashSet<string> InMemoryQueryOperatorNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "Where",
        "Select",
        "Skip",
        "Take",
        "OrderBy",
        "OrderByDescending",
        "ThenBy",
        "ThenByDescending");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var dbSetType = compilationContext.Compilation.GetTypeByMetadataName(EfCoreDbSetType);
            var efCoreQueryableExtensionsType = compilationContext.Compilation.GetTypeByMetadataName(EfCoreQueryableExtensionsType);

            if (dbSetType is null)
            {
                return;
            }

            compilationContext.RegisterSyntaxNodeAction(
                context => AnalyzeInvocation(context, dbSetType),
                SyntaxKind.InvocationExpression);

            if (efCoreQueryableExtensionsType is not null)
            {
                compilationContext.RegisterSyntaxNodeAction(
                    context => AnalyzeLocalDeclaration(context, dbSetType, efCoreQueryableExtensionsType),
                    SyntaxKind.LocalDeclarationStatement);
            }
        });
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, INamedTypeSymbol dbSetType)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol targetMethod
            || !IsSafeInMemoryQueryOperator(invocation, targetMethod))
        {
            return;
        }

        if (!TryGetInvocationReceiver(invocation, out var materializerExpression)
            || materializerExpression is not InvocationExpressionSyntax materializerInvocation
            || context.SemanticModel.GetSymbolInfo(materializerInvocation, context.CancellationToken).Symbol is not IMethodSymbol materializerMethod
            || !IsSynchronousMaterializer(materializerMethod))
        {
            return;
        }

        if (!TryGetInvocationReceiver(materializerInvocation, out var queryExpression)
            || !StartsFromDbSet(queryExpression, context.SemanticModel, context.CancellationToken, dbSetType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            GetInvocationTargetLocation(materializerInvocation),
            materializerMethod.Name,
            targetMethod.Name));
    }

    private static void AnalyzeLocalDeclaration(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol dbSetType,
        INamedTypeSymbol efCoreQueryableExtensionsType)
    {
        var declaration = (LocalDeclarationStatementSyntax)context.Node;

        if (declaration.Declaration.Variables.Count != 1)
        {
            return;
        }

        var variable = declaration.Declaration.Variables[0];
        if (variable.Initializer?.Value is not AwaitExpressionSyntax awaitExpression
            || UnwrapExpression(awaitExpression.Expression) is not InvocationExpressionSyntax materializerInvocation
            || context.SemanticModel.GetSymbolInfo(materializerInvocation, context.CancellationToken).Symbol is not IMethodSymbol materializerMethod
            || !IsEfCoreAsyncMaterializer(materializerMethod, efCoreQueryableExtensionsType)
            || !TryGetInvocationReceiver(materializerInvocation, out var queryExpression)
            || !StartsFromDbSet(queryExpression, context.SemanticModel, context.CancellationToken, dbSetType))
        {
            return;
        }

        if (!TryGetNextStatement(declaration, out var nextStatement)
            || !TryFindImmediateInMemoryOperator(nextStatement, variable.Identifier.ValueText, context.SemanticModel, context.CancellationToken, out var nextOperatorName))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            GetInvocationTargetLocation(materializerInvocation),
            materializerMethod.Name,
            nextOperatorName));
    }

    private static bool TryFindImmediateInMemoryOperator(
        StatementSyntax statement,
        string variableName,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken,
        out string operatorName)
    {
        foreach (var invocation in statement.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method
                || !IsSafeInMemoryQueryOperator(invocation, method)
                || !TryGetInvocationReceiver(invocation, out var receiver)
                || UnwrapExpression(receiver) is not IdentifierNameSyntax identifier
                || !string.Equals(identifier.Identifier.ValueText, variableName, StringComparison.Ordinal))
            {
                continue;
            }

            operatorName = method.Name;
            return true;
        }

        operatorName = string.Empty;
        return false;
    }

    private static bool IsSynchronousMaterializer(IMethodSymbol method)
    {
        var originalMethod = method.ReducedFrom ?? method;

        return SynchronousMaterializerNames.Contains(originalMethod.Name)
            && string.Equals(originalMethod.ContainingNamespace?.ToDisplayString(), "System.Linq", StringComparison.Ordinal);
    }

    private static bool IsEfCoreAsyncMaterializer(IMethodSymbol method, INamedTypeSymbol efCoreQueryableExtensionsType)
    {
        var originalMethod = method.ReducedFrom ?? method;

        return AsyncMaterializerNames.Contains(originalMethod.Name)
            && SymbolEqualityComparer.Default.Equals(originalMethod.ContainingType, efCoreQueryableExtensionsType);
    }

    private static bool IsSafeInMemoryQueryOperator(InvocationExpressionSyntax invocation, IMethodSymbol method)
    {
        var originalMethod = method.ReducedFrom ?? method;

        if (!InMemoryQueryOperatorNames.Contains(originalMethod.Name)
            || !string.Equals(originalMethod.ContainingNamespace?.ToDisplayString(), "System.Linq", StringComparison.Ordinal))
        {
            return false;
        }

        return !IsOrderingMethodWithExplicitComparer(originalMethod.Name, invocation);
    }

    private static bool IsOrderingMethodWithExplicitComparer(string methodName, InvocationExpressionSyntax invocation)
    {
        return (string.Equals(methodName, "OrderBy", StringComparison.Ordinal)
                || string.Equals(methodName, "OrderByDescending", StringComparison.Ordinal)
                || string.Equals(methodName, "ThenBy", StringComparison.Ordinal)
                || string.Equals(methodName, "ThenByDescending", StringComparison.Ordinal))
            && invocation.ArgumentList.Arguments.Count > 1;
    }

    private static bool StartsFromDbSet(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken,
        INamedTypeSymbol dbSetType)
    {
        ExpressionSyntax? current = UnwrapExpression(expression);

        while (current is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var type = semanticModel.GetTypeInfo(current, cancellationToken).Type;
            if (IsDbSet(type, dbSetType))
            {
                return true;
            }

            if (current is InvocationExpressionSyntax invocation)
            {
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

        return false;
    }

    private static bool IsDbSet(ITypeSymbol? type, INamedTypeSymbol dbSetType)
    {
        return type is INamedTypeSymbol namedType
            && SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, dbSetType);
    }

    private static bool TryGetNextStatement(StatementSyntax statement, out StatementSyntax nextStatement)
    {
        if (statement.Parent is not BlockSyntax block)
        {
            nextStatement = null!;
            return false;
        }

        var index = block.Statements.IndexOf(statement);
        if (index < 0 || index + 1 >= block.Statements.Count)
        {
            nextStatement = null!;
            return false;
        }

        nextStatement = block.Statements[index + 1];
        return true;
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
}
