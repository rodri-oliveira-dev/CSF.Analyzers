using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using Microsoft.CodeAnalysis.CSharp;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.CodeAnalysis.Diagnostics;


using CSF.Analyzers.Common.Common;

using CSF.Analyzers.Common;
using CSF.Analyzers.Architecture;

namespace CSF.Analyzers.Architecture.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arc001RequireExplicitAuthorizationOnHttpEndpointsAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Security";
    private const string AllowedRoutesOption = "dotnet_diagnostic.ARC001.allowed_routes";
    private const string AllowedMethodsOption = "dotnet_diagnostic.ARC001.allowed_methods";
    private const string IgnoredNamespacesOption = "dotnet_diagnostic.ARC001.ignored_namespaces";
    private const string AspNetCoreAuthorizationNamespace = "Microsoft.AspNetCore.Authorization";
    private const string AspNetCoreBuilderNamespace = "Microsoft.AspNetCore.Builder";
    private const string AspNetCoreMvcNamespace = "Microsoft.AspNetCore.Mvc";
    private const string AuthorizeAttributeMetadataName = "AuthorizeAttribute";
    private const string AllowAnonymousAttributeMetadataName = "AllowAnonymousAttribute";
    private const string RequireAuthorizationMethodName = "RequireAuthorization";
    private const string AllowAnonymousMethodName = "AllowAnonymous";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.RequireExplicitAuthorizationOnHttpEndpoints,
        title: "Exigir autorizacao explicita em endpoints HTTP",
        messageFormat: "Endpoint HTTP '{0}' must declare an explicit authorization decision with Authorize/RequireAuthorization or AllowAnonymous",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "HTTP endpoints should explicitly declare whether they are protected or intentionally public.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.RequireExplicitAuthorizationOnHttpEndpoints));

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

    private static readonly ImmutableHashSet<string> DefaultAllowedRouteSegments = ImmutableHashSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "health",
        "healthz",
        "swagger",
        "metrics",
        "ready",
        "readiness",
        "live",
        "liveness");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var controllerBaseType = compilationContext.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerBase");
            var controllerType = compilationContext.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Controller");
            var optionsCache = new AuthorizationRuleOptionsCache(compilationContext.Options.AnalyzerConfigOptionsProvider);

            compilationContext.RegisterSymbolAction(
                context => AnalyzeMethod(context, controllerBaseType, controllerType, optionsCache),
                SymbolKind.Method);

            compilationContext.RegisterSyntaxNodeAction(
                context => AnalyzeMinimalApiInvocation(context, optionsCache),
                SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeMethod(
        SymbolAnalysisContext context,
        INamedTypeSymbol? controllerBaseType,
        INamedTypeSymbol? controllerType,
        AuthorizationRuleOptionsCache optionsCache)
    {
        var method = (IMethodSymbol)context.Symbol;

        if (method.MethodKind != MethodKind.Ordinary || method.ContainingType is null)
        {
            return;
        }

        var containingType = method.ContainingType;

        if (containingType.IsAbstract)
        {
            return;
        }

        if (!IsControllerType(containingType, controllerBaseType, controllerType))
        {
            return;
        }

        if (!TryGetHttpRouteAttribute(method, out var routeAttribute))
        {
            return;
        }

        var syntaxTree = method.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree;
        if (syntaxTree is null)
        {
            return;
        }

        var options = optionsCache.Get(syntaxTree);
        if (options.IsIgnoredNamespace(containingType.ContainingNamespace?.ToDisplayString())
            || options.IsAllowedMethod(method.Name))
        {
            return;
        }

        var controllerRoute = TryGetHttpRouteAttribute(containingType, out var controllerRouteAttribute)
            && TryGetAttributeRoute(controllerRouteAttribute.Attribute, out var controllerRouteValue)
                ? controllerRouteValue
                : string.Empty;

        var actionRoute = TryGetAttributeRoute(routeAttribute.Attribute, out var actionRouteValue)
            ? actionRouteValue
            : string.Empty;

        var route = CombineRoutes(controllerRoute, actionRoute);

        if (IsAllowedRoute(route, options))
        {
            return;
        }

        if (HasAuthorizationDecision(method) || HasAuthorizationDecisionInTypeHierarchy(containingType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            routeAttribute.Location,
            containingType.Name + "." + method.Name));
    }

    private static void AnalyzeMinimalApiInvocation(SyntaxNodeAnalysisContext context, AuthorizationRuleOptionsCache optionsCache)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method)
        {
            return;
        }

        if (!IsKnownMinimalApiMapInvocation(invocation, method, context.SemanticModel, context.CancellationToken))
        {
            return;
        }

        if (!TryGetRouteArgument(invocation, context.SemanticModel, context.CancellationToken, out var route))
        {
            route = string.Empty;
        }

        var options = optionsCache.Get(invocation.SyntaxTree);
        var containingNamespace = context.SemanticModel.GetEnclosingSymbol(invocation.SpanStart, context.CancellationToken)
            ?.ContainingNamespace
            ?.ToDisplayString();

        if (options.IsIgnoredNamespace(containingNamespace)
            || options.IsAllowedMethod(method.Name)
            || IsAllowedRoute(route, options))
        {
            return;
        }

        if (OuterChainHasAuthorizationDecision(invocation, context.SemanticModel, context.CancellationToken)
            || (invocation.Expression is MemberAccessExpressionSyntax memberAccess
                && ReceiverHasAuthorizationDecision(memberAccess.Expression, context.SemanticModel, context.CancellationToken)))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            GetInvocationNameLocation(invocation),
            method.Name + " " + route));
    }

    private static bool TryGetHttpRouteAttribute(IMethodSymbol method, out HttpRouteAttributeInfo attributeInfo)
    {
        foreach (var attribute in method.GetAttributes())
        {
            var attributeType = attribute.AttributeClass;

            while (attributeType is not null)
            {
                if (KnownHttpRouteAttributeTypeNames.Contains(attributeType.MetadataName)
                    && string.Equals(attributeType.ContainingNamespace?.ToDisplayString(), AspNetCoreMvcNamespace, StringComparison.Ordinal))
                {
                    attributeInfo = new HttpRouteAttributeInfo(attribute, GetAttributeLocation(attribute, method));
                    return true;
                }

                attributeType = attributeType.BaseType;
            }
        }

        attributeInfo = default;
        return false;
    }

    private static bool TryGetHttpRouteAttribute(INamedTypeSymbol type, out HttpRouteAttributeInfo attributeInfo)
    {
        foreach (var attribute in type.GetAttributes())
        {
            var attributeType = attribute.AttributeClass;

            while (attributeType is not null)
            {
                if (KnownHttpRouteAttributeTypeNames.Contains(attributeType.MetadataName)
                    && string.Equals(attributeType.ContainingNamespace?.ToDisplayString(), AspNetCoreMvcNamespace, StringComparison.Ordinal))
                {
                    attributeInfo = new HttpRouteAttributeInfo(attribute, GetAttributeLocation(attribute, type));
                    return true;
                }

                attributeType = attributeType.BaseType;
            }
        }

        attributeInfo = default;
        return false;
    }

    private static bool TryGetAttributeRoute(AttributeData attribute, out string route)
    {
        foreach (var constructorArgument in attribute.ConstructorArguments)
        {
            if (constructorArgument.Value is string routeValue)
            {
                route = routeValue;
                return true;
            }
        }

        route = string.Empty;
        return false;
    }

    private static string CombineRoutes(string controllerRoute, string actionRoute)
    {
        if (string.IsNullOrWhiteSpace(controllerRoute))
        {
            return actionRoute;
        }

        if (string.IsNullOrWhiteSpace(actionRoute))
        {
            return controllerRoute;
        }

        return controllerRoute.TrimEnd('/') + "/" + actionRoute.TrimStart('/');
    }

    private static bool HasAuthorizationDecision(ISymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (IsAuthorizationDecisionAttribute(attribute.AttributeClass))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAuthorizationDecisionInTypeHierarchy(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (HasAuthorizationDecision(current))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAuthorizationDecisionAttribute(INamedTypeSymbol? attributeType)
    {
        for (INamedTypeSymbol? current = attributeType; current is not null; current = current.BaseType)
        {
            if ((string.Equals(current.MetadataName, AuthorizeAttributeMetadataName, StringComparison.Ordinal)
                    || string.Equals(current.MetadataName, AllowAnonymousAttributeMetadataName, StringComparison.Ordinal))
                && string.Equals(current.ContainingNamespace?.ToDisplayString(), AspNetCoreAuthorizationNamespace, StringComparison.Ordinal))
            {
                return true;
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

    private static bool OuterChainHasAuthorizationDecision(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        foreach (var outerInvocation in EnumerateOuterInvocations(invocation))
        {
            if (semanticModel.GetSymbolInfo(outerInvocation, cancellationToken).Symbol is not IMethodSymbol method)
            {
                continue;
            }

            if ((string.Equals(method.Name, RequireAuthorizationMethodName, StringComparison.Ordinal)
                    || string.Equals(method.Name, AllowAnonymousMethodName, StringComparison.Ordinal))
                && IsEndpointConventionBuilderInvocation(outerInvocation, method, semanticModel, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ReceiverHasAuthorizationDecision(
        ExpressionSyntax receiver,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        receiver = UnwrapExpression(receiver);

        if (receiver is InvocationExpressionSyntax invocation)
        {
            return InvocationIsAuthorizationDecision(invocation, semanticModel, cancellationToken)
                || (invocation.Expression is MemberAccessExpressionSyntax memberAccess
                    && ReceiverHasAuthorizationDecision(memberAccess.Expression, semanticModel, cancellationToken));
        }

        if (semanticModel.GetSymbolInfo(receiver, cancellationToken).Symbol is not { } symbol)
        {
            return false;
        }

        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var syntax = syntaxReference.GetSyntax(cancellationToken);
            if (syntax is VariableDeclaratorSyntax { Initializer.Value: { } initializer }
                && ReceiverHasAuthorizationDecision(initializer, semanticModel, cancellationToken))
            {
                return true;
            }

            if (syntax is PropertyDeclarationSyntax { Initializer.Value: { } propertyInitializer }
                && ReceiverHasAuthorizationDecision(propertyInitializer, semanticModel, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static bool InvocationIsAuthorizationDecision(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        return semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol method
            && (string.Equals(method.Name, RequireAuthorizationMethodName, StringComparison.Ordinal)
                || string.Equals(method.Name, AllowAnonymousMethodName, StringComparison.Ordinal))
            && IsEndpointConventionBuilderInvocation(invocation, method, semanticModel, cancellationToken);
    }

    private static IEnumerable<InvocationExpressionSyntax> EnumerateOuterInvocations(InvocationExpressionSyntax invocation)
    {
        SyntaxNode? current = invocation;

        while (current?.Parent is not null)
        {
            if (current.Parent is MemberAccessExpressionSyntax memberAccess
                && memberAccess.Expression == current
                && memberAccess.Parent is InvocationExpressionSyntax outerInvocation)
            {
                yield return outerInvocation;
                current = outerInvocation;
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

    private static bool TryGetRouteArgument(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken,
        out string route)
    {
        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            var constant = semanticModel.GetConstantValue(argument.Expression, cancellationToken);

            if (constant.HasValue && constant.Value is string routeValue)
            {
                route = routeValue;
                return true;
            }
        }

        route = string.Empty;
        return false;
    }

    private static bool IsAllowedRoute(string route, AuthorizationRuleOptions options)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return false;
        }

        var normalizedRoute = NormalizeRoute(route);

        foreach (var segment in normalizedRoute.Split('/'))
        {
            if (DefaultAllowedRouteSegments.Contains(segment))
            {
                return true;
            }
        }

        return options.IsAllowedRoute(normalizedRoute);
    }

    private static string NormalizeRoute(string route)
    {
        var queryStringIndex = route.IndexOf('?');
        var path = queryStringIndex >= 0 ? route.Substring(0, queryStringIndex) : route;
        return path.Trim().Trim('/').ToLowerInvariant();
    }

    private static Location GetAttributeLocation(AttributeData attribute, ISymbol fallbackSymbol)
    {
        if (attribute.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax attributeSyntax)
        {
            return attributeSyntax.Name.GetLocation();
        }

        return fallbackSymbol.Locations.FirstOrDefault() ?? Location.None;
    }

    private static Location GetInvocationNameLocation(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression is MemberAccessExpressionSyntax memberAccess
            ? memberAccess.Name.GetLocation()
            : invocation.Expression.GetLocation();
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

    private readonly struct HttpRouteAttributeInfo
    {
        public HttpRouteAttributeInfo(AttributeData attribute, Location location)
        {
            Attribute = attribute;
            Location = location;
        }

        public AttributeData Attribute
        {
            get;
        }

        public Location Location
        {
            get;
        }
    }

    private sealed class AuthorizationRuleOptionsCache
    {
        private readonly AnalyzerConfigOptionsProvider _provider;
        private readonly ConcurrentDictionary<SyntaxTree, AuthorizationRuleOptions> _optionsBySyntaxTree = new();

        public AuthorizationRuleOptionsCache(AnalyzerConfigOptionsProvider provider)
        {
            _provider = provider;
        }

        public AuthorizationRuleOptions Get(SyntaxTree syntaxTree)
        {
            return _optionsBySyntaxTree.GetOrAdd(syntaxTree, CreateOptions);
        }

        private AuthorizationRuleOptions CreateOptions(SyntaxTree syntaxTree)
        {
            return AuthorizationRuleOptions.Create(_provider, syntaxTree);
        }
    }

    private readonly struct AuthorizationRuleOptions
    {
        private AuthorizationRuleOptions(
            ImmutableHashSet<string> allowedRoutes,
            ImmutableHashSet<string> allowedMethods,
            ImmutableArray<string> ignoredNamespaces)
        {
            AllowedRoutes = allowedRoutes;
            AllowedMethods = allowedMethods;
            IgnoredNamespaces = ignoredNamespaces;
        }

        private ImmutableHashSet<string> AllowedRoutes
        {
            get;
        }

        private ImmutableHashSet<string> AllowedMethods
        {
            get;
        }

        private ImmutableArray<string> IgnoredNamespaces
        {
            get;
        }

        public bool IsAllowedRoute(string normalizedRoute)
        {
            foreach (var allowedRoute in AllowedRoutes)
            {
                if (allowedRoute.EndsWith("*", StringComparison.Ordinal)
                    && normalizedRoute.StartsWith(allowedRoute.Substring(0, allowedRoute.Length - 1), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (string.Equals(normalizedRoute, allowedRoute, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsAllowedMethod(string methodName)
        {
            return AllowedMethods.Contains(methodName);
        }

        public bool IsIgnoredNamespace(string? namespaceName)
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                return false;
            }

            var currentNamespace = namespaceName!;

            foreach (var ignoredNamespace in IgnoredNamespaces)
            {
                var namespacePrefix = ignoredNamespace ?? string.Empty;

                if (string.IsNullOrWhiteSpace(namespacePrefix))
                {
                    continue;
                }

                if (string.Equals(currentNamespace, namespacePrefix, StringComparison.Ordinal)
                    || currentNamespace.StartsWith(namespacePrefix + ".", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static AuthorizationRuleOptions Create(AnalyzerConfigOptionsProvider provider, SyntaxTree syntaxTree)
        {
            var options = provider.GetOptions(syntaxTree);

            return new AuthorizationRuleOptions(
                ReadStringArray(options, AllowedRoutesOption, NormalizeRoute).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase),
                ReadStringArray(options, AllowedMethodsOption, static value => value.Trim()).ToImmutableHashSet(StringComparer.Ordinal),
                ReadStringArray(options, IgnoredNamespacesOption, static value => value.Trim()).ToImmutableArray());
        }

        private static IEnumerable<string> ReadStringArray(
            AnalyzerConfigOptions options,
            string optionName,
            Func<string, string> normalize)
        {
            return AnalyzerConfigOptionReader.ReadStringArrayOption(
                options,
                optionName,
                ImmutableArray<string>.Empty,
                normalize);
        }

    }
}
