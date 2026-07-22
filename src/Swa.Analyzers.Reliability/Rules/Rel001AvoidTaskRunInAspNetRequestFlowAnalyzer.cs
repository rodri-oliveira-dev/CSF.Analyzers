using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using Microsoft.CodeAnalysis.CSharp;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.CodeAnalysis.Diagnostics;

using Swa.Analyzers.Common;
using Swa.Analyzers.Reliability;

namespace Swa.Analyzers.Reliability.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Rel001AvoidTaskRunInAspNetRequestFlowAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Performance";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.AvoidTaskRunInAspNetRequestFlow,
        title: "Evitar Task.Run em fluxo de request ASP.NET",
        messageFormat: "Avoid '{0}' in ASP.NET request flow. Prefer awaiting asynchronous APIs directly or move background work to a hosted service.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Using Task.Run or Task.Factory.StartNew inside ASP.NET request flows shifts work to the ThreadPool without improving scalability. Prefer true asynchronous APIs or background processing outside the request.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.AvoidTaskRunInAspNetRequestFlow));

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
            var taskFactoryType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.TaskFactory");
            var taskFactoryOfTType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.TaskFactory`1");

            if (taskType is null && taskFactoryType is null && taskFactoryOfTType is null)
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
                context => AnalyzeInvocation(
                    context,
                    taskType,
                    taskFactoryType,
                    taskFactoryOfTType,
                    controllerBaseType,
                    controllerType,
                    backgroundServiceType,
                    hostedServiceType,
                    testMethodAttributes,
                    isTestTypeCache),
                SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol? taskType,
        INamedTypeSymbol? taskFactoryType,
        INamedTypeSymbol? taskFactoryOfTType,
        INamedTypeSymbol? controllerBaseType,
        INamedTypeSymbol? controllerType,
        INamedTypeSymbol? backgroundServiceType,
        INamedTypeSymbol? hostedServiceType,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol targetMethod)
        {
            return;
        }

        if (!TryGetForbiddenUsage(targetMethod, taskType, taskFactoryType, taskFactoryOfTType, out var usage))
        {
            return;
        }

        var containingSymbol = context.SemanticModel.GetEnclosingSymbol(invocation.SpanStart, context.CancellationToken);
        if (containingSymbol is null)
        {
            return;
        }

        if (!testMethodAttributes.IsDefaultOrEmpty
            && TestContextHelper.IsWithinTestContext(containingSymbol, testMethodAttributes, isTestTypeCache))
        {
            return;
        }

        var containingType = GetContainingType(containingSymbol);
        if (containingType is not null && IsHostedServiceType(containingType, backgroundServiceType, hostedServiceType))
        {
            return;
        }

        if (!IsAspNetRequestFlow(
            invocation,
            containingSymbol,
            containingType,
            context.SemanticModel,
            context.CancellationToken,
            controllerBaseType,
            controllerType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, GetInvocationTargetLocation(invocation), usage));
    }

    private static bool TryGetForbiddenUsage(
        IMethodSymbol targetMethod,
        INamedTypeSymbol? taskType,
        INamedTypeSymbol? taskFactoryType,
        INamedTypeSymbol? taskFactoryOfTType,
        out string usage)
    {
        if (string.Equals(targetMethod.Name, "Run", StringComparison.Ordinal)
            && taskType is not null
            && SymbolEqualityComparer.Default.Equals(targetMethod.ContainingType, taskType))
        {
            usage = "Task.Run";
            return true;
        }

        if (string.Equals(targetMethod.Name, "StartNew", StringComparison.Ordinal)
            && IsKnownTaskFactoryType(targetMethod.ContainingType, taskFactoryType, taskFactoryOfTType))
        {
            usage = "Task.Factory.StartNew";
            return true;
        }

        usage = string.Empty;
        return false;
    }

    private static bool IsKnownTaskFactoryType(
        INamedTypeSymbol? type,
        INamedTypeSymbol? taskFactoryType,
        INamedTypeSymbol? taskFactoryOfTType)
    {
        if (type is null)
        {
            return false;
        }

        return (taskFactoryType is not null && SymbolEqualityComparer.Default.Equals(type, taskFactoryType))
            || (taskFactoryOfTType is not null && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, taskFactoryOfTType));
    }

    private static bool IsAspNetRequestFlow(
        InvocationExpressionSyntax invocation,
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

        return IsInsideMinimalApiInlineHandler(invocation, semanticModel, cancellationToken);
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
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        foreach (var lambda in invocation.Ancestors().OfType<AnonymousFunctionExpressionSyntax>())
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

    private static Location GetInvocationTargetLocation(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression.GetLocation();
    }
}
