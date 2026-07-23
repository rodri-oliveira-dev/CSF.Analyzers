using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using Microsoft.CodeAnalysis.CSharp;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.CodeAnalysis.Diagnostics;

using Microsoft.CodeAnalysis.Operations;

using CSF.Analyzers.Common;
using CSF.Analyzers.Reliability;

namespace CSF.Analyzers.Reliability.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Rel002ProhibitFireAndForgetInRequestFlowAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Reliability";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.ProhibitFireAndForgetInRequestFlow,
        title: "Evitar fire-and-forget em fluxo de request",
        messageFormat: "Avoid fire-and-forget '{0}' in ASP.NET request flow. Await the operation or move background work to an explicit queue/hosted service.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Discarding Task or ValueTask instances in ASP.NET request flows hides background work that can outlive the request, lose exceptions, or bypass cancellation. Await the operation or enqueue it explicitly outside the request.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.ProhibitFireAndForgetInRequestFlow));

    private static readonly ImmutableHashSet<string> KnownHttpRouteAttributeTypeNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "RouteAttribute",
        "HttpGetAttribute",
        "HttpPostAttribute",
        "HttpPutAttribute",
        "HttpPatchAttribute",
        "HttpDeleteAttribute",
        "HttpHeadAttribute",
        "HttpOptionsAttribute");

    private static readonly ImmutableHashSet<string> KnownMinimalApiMethodNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "MapGet",
        "MapPost",
        "MapPut",
        "MapPatch",
        "MapDelete",
        "MapMethods");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var taskType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            var taskOfTType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            var valueTaskType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
            var valueTaskOfTType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

            if (taskType is null && valueTaskType is null)
            {
                return;
            }

            var controllerBaseType = compilationContext.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerBase");
            var controllerType = compilationContext.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Controller");
            var backgroundServiceType = compilationContext.Compilation.GetTypeByMetadataName("Microsoft.Extensions.Hosting.BackgroundService");
            var hostedServiceType = compilationContext.Compilation.GetTypeByMetadataName("Microsoft.Extensions.Hosting.IHostedService");
            var testMethodAttributes = TestContextHelper.GetKnownTestMethodAttributes(compilationContext.Compilation);
            var isTestTypeCache = new ConcurrentDictionary<INamedTypeSymbol, bool>(SymbolEqualityComparer.Default);

            compilationContext.RegisterSyntaxNodeAction(
                context => AnalyzeAssignment(
                    context,
                    taskType,
                    taskOfTType,
                    valueTaskType,
                    valueTaskOfTType,
                    controllerBaseType,
                    controllerType,
                    backgroundServiceType,
                    hostedServiceType,
                    testMethodAttributes,
                    isTestTypeCache),
                SyntaxKind.SimpleAssignmentExpression);

            compilationContext.RegisterSyntaxNodeAction(
                context => AnalyzeExpressionStatement(
                    context,
                    taskType,
                    controllerBaseType,
                    controllerType,
                    backgroundServiceType,
                    hostedServiceType,
                    testMethodAttributes,
                    isTestTypeCache),
                SyntaxKind.ExpressionStatement);
        });
    }

    private static void AnalyzeAssignment(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol? taskType,
        INamedTypeSymbol? taskOfTType,
        INamedTypeSymbol? valueTaskType,
        INamedTypeSymbol? valueTaskOfTType,
        INamedTypeSymbol? controllerBaseType,
        INamedTypeSymbol? controllerType,
        INamedTypeSymbol? backgroundServiceType,
        INamedTypeSymbol? hostedServiceType,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        if (context.SemanticModel.GetOperation(context.Node, context.CancellationToken) is not ISimpleAssignmentOperation assignment)
        {
            return;
        }

        if (assignment.Target is not IDiscardOperation)
        {
            return;
        }

        var value = UnwrapConversion(assignment.Value);
        if (value is not IInvocationOperation invocation)
        {
            return;
        }

        if (!IsKnownAwaitableType(value.Type, taskType, taskOfTType, valueTaskType, valueTaskOfTType))
        {
            return;
        }

        if (!IsRelevantRequestFlow(
            context,
            assignment.Syntax,
            controllerBaseType,
            controllerType,
            backgroundServiceType,
            hostedServiceType,
            testMethodAttributes,
            isTestTypeCache))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, GetInvocationTargetLocation(invocation.Syntax), GetInvocationUsage(invocation, taskType)));
    }

    private static void AnalyzeExpressionStatement(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol? taskType,
        INamedTypeSymbol? controllerBaseType,
        INamedTypeSymbol? controllerType,
        INamedTypeSymbol? backgroundServiceType,
        INamedTypeSymbol? hostedServiceType,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        if (context.SemanticModel.GetOperation(context.Node, context.CancellationToken) is not IExpressionStatementOperation expressionStatement)
        {
            return;
        }

        var operation = UnwrapConversion(expressionStatement.Operation);

        if (operation is not IInvocationOperation invocation
            || !IsTaskRun(invocation.TargetMethod, taskType))
        {
            return;
        }

        if (!IsRelevantRequestFlow(
            context,
            expressionStatement.Syntax,
            controllerBaseType,
            controllerType,
            backgroundServiceType,
            hostedServiceType,
            testMethodAttributes,
            isTestTypeCache))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, GetInvocationTargetLocation(invocation.Syntax), "Task.Run"));
    }

    private static bool IsRelevantRequestFlow(
        SyntaxNodeAnalysisContext context,
        SyntaxNode syntax,
        INamedTypeSymbol? controllerBaseType,
        INamedTypeSymbol? controllerType,
        INamedTypeSymbol? backgroundServiceType,
        INamedTypeSymbol? hostedServiceType,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        var containingSymbol = context.SemanticModel.GetEnclosingSymbol(syntax.SpanStart, context.CancellationToken);
        if (containingSymbol is null)
        {
            return false;
        }

        if (!testMethodAttributes.IsDefaultOrEmpty
            && TestContextHelper.IsWithinTestContext(containingSymbol, testMethodAttributes, isTestTypeCache))
        {
            return false;
        }

        var containingType = GetContainingType(containingSymbol);
        if (containingType is not null && IsHostedServiceType(containingType, backgroundServiceType, hostedServiceType))
        {
            return false;
        }

        return IsAspNetRequestFlow(
            syntax,
            containingSymbol,
            containingType,
            context.SemanticModel,
            context.CancellationToken,
            controllerBaseType,
            controllerType);
    }

    private static bool IsAspNetRequestFlow(
        SyntaxNode syntax,
        ISymbol containingSymbol,
        INamedTypeSymbol? containingType,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken,
        INamedTypeSymbol? controllerBaseType,
        INamedTypeSymbol? controllerType)
    {
        if (containingType is not null && IsControllerType(containingType, controllerBaseType, controllerType))
        {
            return true;
        }

        if (containingSymbol is IMethodSymbol method && HasKnownHttpRouteAttribute(method))
        {
            return true;
        }

        return IsInsideMinimalApiInlineHandler(syntax, semanticModel, cancellationToken);
    }

    private static bool IsControllerType(
        INamedTypeSymbol type,
        INamedTypeSymbol? controllerBaseType,
        INamedTypeSymbol? controllerType)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if ((controllerBaseType is not null && SymbolEqualityComparer.Default.Equals(current, controllerBaseType))
                || (controllerType is not null && SymbolEqualityComparer.Default.Equals(current, controllerType)))
            {
                return true;
            }

            if ((string.Equals(current.Name, "ControllerBase", StringComparison.Ordinal)
                    || string.Equals(current.Name, "Controller", StringComparison.Ordinal))
                && string.Equals(current.ContainingNamespace?.ToDisplayString(), "Microsoft.AspNetCore.Mvc", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasKnownHttpRouteAttribute(IMethodSymbol method)
    {
        foreach (var attribute in method.GetAttributes())
        {
            var attributeType = attribute.AttributeClass;

            while (attributeType is not null)
            {
                if (KnownHttpRouteAttributeTypeNames.Contains(attributeType.MetadataName)
                    && string.Equals(attributeType.ContainingNamespace?.ToDisplayString(), "Microsoft.AspNetCore.Mvc", StringComparison.Ordinal))
                {
                    return true;
                }

                attributeType = attributeType.BaseType;
            }
        }

        return false;
    }

    private static bool IsInsideMinimalApiInlineHandler(
        SyntaxNode syntax,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        foreach (var lambda in syntax.Ancestors().OfType<AnonymousFunctionExpressionSyntax>())
        {
            if (TryGetContainingArgument(lambda, out var argument)
                && argument.Parent?.Parent is InvocationExpressionSyntax mapInvocation
                && IsKnownMinimalApiInvocation(mapInvocation, semanticModel, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetContainingArgument(SyntaxNode node, out ArgumentSyntax argument)
    {
        SyntaxNode? current = node;

        while (current is ParenthesizedExpressionSyntax or CastExpressionSyntax)
        {
            current = current.Parent;
        }

        if (current?.Parent is ArgumentSyntax parentArgument)
        {
            argument = parentArgument;
            return true;
        }

        argument = null!;
        return false;
    }

    private static bool IsKnownMinimalApiInvocation(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol targetMethod)
        {
            return false;
        }

        if (!KnownMinimalApiMethodNames.Contains(targetMethod.Name))
        {
            return false;
        }

        var originalDefinition = targetMethod.ReducedFrom ?? targetMethod;

        if (!string.Equals(originalDefinition.ContainingNamespace?.ToDisplayString(), "Microsoft.AspNetCore.Builder", StringComparison.Ordinal))
        {
            return false;
        }

        var receiverType = GetMinimalApiReceiverType(invocation, originalDefinition, semanticModel, cancellationToken);

        return IsEndpointRouteBuilderCompatible(receiverType);
    }

    private static ITypeSymbol? GetMinimalApiReceiverType(
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            return semanticModel.GetTypeInfo(memberAccess.Expression, cancellationToken).Type;
        }

        return method.IsExtensionMethod && method.Parameters.Length > 0
            ? method.Parameters[0].Type
            : null;
    }

    private static bool IsEndpointRouteBuilderCompatible(ITypeSymbol? type)
    {
        if (type is null)
        {
            return false;
        }

        if (IsEndpointRouteBuilder(type))
        {
            return true;
        }

        foreach (var interfaceType in type.AllInterfaces)
        {
            if (IsEndpointRouteBuilder(interfaceType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsEndpointRouteBuilder(ITypeSymbol type)
    {
        return string.Equals(type.Name, "IEndpointRouteBuilder", StringComparison.Ordinal)
            && (string.Equals(type.ContainingNamespace?.ToDisplayString(), "Microsoft.AspNetCore.Routing", StringComparison.Ordinal)
                || string.Equals(type.ContainingNamespace?.ToDisplayString(), "Microsoft.AspNetCore.Builder", StringComparison.Ordinal));
    }

    private static bool IsHostedServiceType(
        INamedTypeSymbol type,
        INamedTypeSymbol? backgroundServiceType,
        INamedTypeSymbol? hostedServiceType)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (backgroundServiceType is not null && SymbolEqualityComparer.Default.Equals(current, backgroundServiceType))
            {
                return true;
            }
        }

        if (hostedServiceType is null)
        {
            return false;
        }

        foreach (var interfaceType in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(interfaceType, hostedServiceType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsKnownAwaitableType(ITypeSymbol? type, params INamedTypeSymbol?[] expectedTypes)
    {
        if (type is null)
        {
            return false;
        }

        foreach (var expectedType in expectedTypes)
        {
            if (expectedType is null)
            {
                continue;
            }

            if (SymbolEqualityComparer.Default.Equals(type, expectedType))
            {
                return true;
            }

            if (type.OriginalDefinition is not null
                && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, expectedType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTaskRun(IMethodSymbol method, INamedTypeSymbol? taskType)
    {
        return string.Equals(method.Name, "Run", StringComparison.Ordinal)
            && taskType is not null
            && SymbolEqualityComparer.Default.Equals(method.ContainingType, taskType);
    }

    private static IOperation UnwrapConversion(IOperation operation)
    {
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        return operation;
    }

    private static INamedTypeSymbol? GetContainingType(ISymbol symbol)
    {
        for (ISymbol? current = symbol; current is not null; current = current.ContainingSymbol)
        {
            if (current is INamedTypeSymbol type)
            {
                return type;
            }
        }

        return null;
    }

    private static string GetInvocationUsage(IInvocationOperation invocation, INamedTypeSymbol? taskType)
    {
        return IsTaskRun(invocation.TargetMethod, taskType)
            ? "Task.Run"
            : invocation.TargetMethod.Name;
    }

    private static Location GetInvocationTargetLocation(SyntaxNode syntax)
    {
        return syntax switch
        {
            InvocationExpressionSyntax invocation => invocation.Expression.GetLocation(),
            _ => syntax.GetLocation(),
        };
    }
}
