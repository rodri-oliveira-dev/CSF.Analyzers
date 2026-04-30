using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch019AvoidAuthorizeWithAllowAnonymousAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Security";
    private const string AspNetCoreAuthorizationNamespace = "Microsoft.AspNetCore.Authorization";
    private const string AspNetCoreBuilderNamespace = "Microsoft.AspNetCore.Builder";
    private const string AspNetCoreMvcNamespace = "Microsoft.AspNetCore.Mvc";
    private const string AuthorizeMetadataName = "Authorize";
    private const string AllowAnonymousMetadataName = "AllowAnonymous";
    private const string RequireAuthorizationMethodName = "RequireAuthorization";
    private const string AllowAnonymousMethodName = "AllowAnonymous";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.AvoidAuthorizeWithAllowAnonymous,
        title: "Evitar Authorize e AllowAnonymous no mesmo endpoint",
        messageFormat: "Avoid combining '{0}' with '{1}' on the same ASP.NET endpoint. Review the effective authorization metadata and make the intended access explicit.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Conflicting Authorize and AllowAnonymous metadata can make ASP.NET endpoints anonymous even when the code appears protected, or hide an intentional public override.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.AvoidAuthorizeWithAllowAnonymous));

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
            var controllerBaseType = compilationContext.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerBase");
            var controllerType = compilationContext.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Controller");

            compilationContext.RegisterSymbolAction(
                context => AnalyzeMethod(context, controllerBaseType, controllerType),
                SymbolKind.Method);

            compilationContext.RegisterSyntaxNodeAction(
                AnalyzeMinimalApiInvocation,
                SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeMethod(
        SymbolAnalysisContext context,
        INamedTypeSymbol? controllerBaseType,
        INamedTypeSymbol? controllerType)
    {
        var method = (IMethodSymbol)context.Symbol;

        if (method.MethodKind != MethodKind.Ordinary || method.ContainingType is null)
        {
            return;
        }

        var containingType = method.ContainingType;
        var isControllerAction = IsControllerType(containingType, controllerBaseType, controllerType)
            || HasKnownHttpRouteAttribute(method);

        if (!isControllerAction)
        {
            return;
        }

        var methodAuthorize = GetAuthorizationAttribute(method, AuthorizationMetadataKind.Authorize);
        var methodAllowAnonymous = GetAuthorizationAttribute(method, AuthorizationMetadataKind.AllowAnonymous);

        if (methodAuthorize.HasValue && methodAllowAnonymous.HasValue)
        {
            Report(context, methodAllowAnonymous.Value, AllowAnonymousMetadataName, AuthorizeMetadataName);
            return;
        }

        var controllerAuthorize = GetAuthorizationAttributeFromTypeHierarchy(containingType, AuthorizationMetadataKind.Authorize);
        var controllerAllowAnonymous = GetAuthorizationAttributeFromTypeHierarchy(containingType, AuthorizationMetadataKind.AllowAnonymous);

        if (controllerAllowAnonymous.HasValue && methodAuthorize.HasValue)
        {
            Report(context, methodAuthorize.Value, AuthorizeMetadataName, AllowAnonymousMetadataName);
            return;
        }

        if (controllerAuthorize.HasValue && methodAllowAnonymous.HasValue)
        {
            Report(context, methodAllowAnonymous.Value, AllowAnonymousMetadataName, AuthorizeMetadataName);
        }
    }

    private static void AnalyzeMinimalApiInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol targetMethod)
        {
            return;
        }

        if (!TryGetMinimalApiAuthorizationMetadata(targetMethod, out var currentMetadata, out var conflictingMetadata))
        {
            return;
        }

        if (!IsEndpointConventionBuilderInvocation(invocation, targetMethod, context.SemanticModel, context.CancellationToken))
        {
            return;
        }

        if (!ReceiverChainHasMetadata(
            invocation,
            conflictingMetadata,
            context.SemanticModel,
            context.CancellationToken))
        {
            return;
        }

        if (!ReceiverChainHasKnownMinimalApiMap(invocation, context.SemanticModel, context.CancellationToken))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            GetInvocationNameLocation(invocation),
            currentMetadata,
            conflictingMetadata));
    }

    private static void Report(
        SymbolAnalysisContext context,
        AuthorizationAttributeInfo attribute,
        string currentMetadata,
        string conflictingMetadata)
    {
        context.ReportDiagnostic(Diagnostic.Create(Rule, attribute.Location, currentMetadata, conflictingMetadata));
    }

    private static AuthorizationAttributeInfo? GetAuthorizationAttribute(
        ISymbol symbol,
        AuthorizationMetadataKind metadataKind)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (IsAuthorizationAttribute(attribute.AttributeClass, metadataKind))
            {
                return new AuthorizationAttributeInfo(GetAttributeLocation(attribute, symbol));
            }
        }

        return null;
    }

    private static AuthorizationAttributeInfo? GetAuthorizationAttributeFromTypeHierarchy(
        INamedTypeSymbol type,
        AuthorizationMetadataKind metadataKind)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            var attribute = GetAuthorizationAttribute(current, metadataKind);

            if (attribute.HasValue)
            {
                return attribute;
            }
        }

        return null;
    }

    private static Location GetAttributeLocation(AttributeData attribute, ISymbol fallbackSymbol)
    {
        if (attribute.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax attributeSyntax)
        {
            return attributeSyntax.Name.GetLocation();
        }

        return fallbackSymbol.Locations.FirstOrDefault() ?? Location.None;
    }

    private static bool IsAuthorizationAttribute(INamedTypeSymbol? attributeType, AuthorizationMetadataKind metadataKind)
    {
        var expectedMetadataName = metadataKind == AuthorizationMetadataKind.Authorize
            ? "AuthorizeAttribute"
            : "AllowAnonymousAttribute";

        for (INamedTypeSymbol? current = attributeType; current is not null; current = current.BaseType)
        {
            if (string.Equals(current.MetadataName, expectedMetadataName, StringComparison.Ordinal)
                && string.Equals(current.ContainingNamespace?.ToDisplayString(), AspNetCoreAuthorizationNamespace, StringComparison.Ordinal))
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
                    && string.Equals(attributeType.ContainingNamespace?.ToDisplayString(), AspNetCoreMvcNamespace, StringComparison.Ordinal))
                {
                    return true;
                }

                attributeType = attributeType.BaseType;
            }
        }

        return false;
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
                && string.Equals(current.ContainingNamespace?.ToDisplayString(), AspNetCoreMvcNamespace, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetMinimalApiAuthorizationMetadata(
        IMethodSymbol method,
        out string currentMetadata,
        out string conflictingMetadata)
    {
        var originalDefinition = method.ReducedFrom ?? method;

        if (!string.Equals(originalDefinition.ContainingNamespace?.ToDisplayString(), AspNetCoreBuilderNamespace, StringComparison.Ordinal))
        {
            currentMetadata = string.Empty;
            conflictingMetadata = string.Empty;
            return false;
        }

        if (string.Equals(method.Name, RequireAuthorizationMethodName, StringComparison.Ordinal))
        {
            currentMetadata = RequireAuthorizationMethodName;
            conflictingMetadata = AllowAnonymousMethodName;
            return true;
        }

        if (string.Equals(method.Name, AllowAnonymousMethodName, StringComparison.Ordinal))
        {
            currentMetadata = AllowAnonymousMethodName;
            conflictingMetadata = RequireAuthorizationMethodName;
            return true;
        }

        currentMetadata = string.Empty;
        conflictingMetadata = string.Empty;
        return false;
    }

    private static bool ReceiverChainHasMetadata(
        InvocationExpressionSyntax invocation,
        string metadataName,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        foreach (var receiverInvocation in EnumerateReceiverInvocations(invocation))
        {
            if (semanticModel.GetSymbolInfo(receiverInvocation, cancellationToken).Symbol is not IMethodSymbol method)
            {
                continue;
            }

            if (string.Equals(method.Name, metadataName, StringComparison.Ordinal)
                && IsEndpointConventionBuilderInvocation(receiverInvocation, method, semanticModel, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ReceiverChainHasKnownMinimalApiMap(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        foreach (var receiverInvocation in EnumerateReceiverInvocations(invocation))
        {
            if (semanticModel.GetSymbolInfo(receiverInvocation, cancellationToken).Symbol is not IMethodSymbol method)
            {
                continue;
            }

            if (IsKnownMinimalApiMapInvocation(receiverInvocation, method, semanticModel, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<InvocationExpressionSyntax> EnumerateReceiverInvocations(InvocationExpressionSyntax invocation)
    {
        ExpressionSyntax? current = GetReceiverExpression(invocation);

        while (current is not null)
        {
            current = UnwrapExpression(current);

            if (current is InvocationExpressionSyntax receiverInvocation)
            {
                yield return receiverInvocation;
                current = GetReceiverExpression(receiverInvocation);
                continue;
            }

            break;
        }
    }

    private static bool IsEndpointConventionBuilderInvocation(
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        var receiverType = GetReceiverType(invocation, method, semanticModel, cancellationToken);
        return IsEndpointConventionBuilderCompatible(receiverType);
    }

    private static bool IsKnownMinimalApiMapInvocation(
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        if (!KnownMinimalApiMethodNames.Contains(method.Name))
        {
            return false;
        }

        var originalDefinition = method.ReducedFrom ?? method;

        if (!string.Equals(originalDefinition.ContainingNamespace?.ToDisplayString(), AspNetCoreBuilderNamespace, StringComparison.Ordinal))
        {
            return false;
        }

        var receiverType = GetReceiverType(invocation, method, semanticModel, cancellationToken);
        return IsEndpointRouteBuilderCompatible(receiverType);
    }

    private static ITypeSymbol? GetReceiverType(
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            return semanticModel.GetTypeInfo(memberAccess.Expression, cancellationToken).Type;
        }

        var originalDefinition = method.ReducedFrom ?? method;

        return originalDefinition.IsExtensionMethod && originalDefinition.Parameters.Length > 0
            ? originalDefinition.Parameters[0].Type
            : null;
    }

    private static ExpressionSyntax? GetReceiverExpression(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression is MemberAccessExpressionSyntax memberAccess
            ? memberAccess.Expression
            : null;
    }

    private static ExpressionSyntax UnwrapExpression(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        return expression;
    }

    private static bool IsEndpointConventionBuilderCompatible(ITypeSymbol? type)
    {
        if (type is null)
        {
            return false;
        }

        if (IsEndpointConventionBuilder(type))
        {
            return true;
        }

        foreach (var interfaceType in type.AllInterfaces)
        {
            if (IsEndpointConventionBuilder(interfaceType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsEndpointConventionBuilder(ITypeSymbol type)
    {
        return string.Equals(type.Name, "IEndpointConventionBuilder", StringComparison.Ordinal)
            && string.Equals(type.ContainingNamespace?.ToDisplayString(), AspNetCoreBuilderNamespace, StringComparison.Ordinal);
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
                || string.Equals(type.ContainingNamespace?.ToDisplayString(), AspNetCoreBuilderNamespace, StringComparison.Ordinal));
    }

    private static Location GetInvocationNameLocation(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression is MemberAccessExpressionSyntax memberAccess
            ? memberAccess.Name.GetLocation()
            : invocation.Expression.GetLocation();
    }

    private readonly struct AuthorizationAttributeInfo
    {
        public AuthorizationAttributeInfo(Location location)
        {
            Location = location;
        }

        public Location Location
        {
            get;
        }
    }

    private enum AuthorizationMetadataKind
    {
        Authorize,
        AllowAnonymous,
    }
}
