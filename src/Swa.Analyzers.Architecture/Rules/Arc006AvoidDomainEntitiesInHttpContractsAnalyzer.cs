using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Swa.Analyzers.Common;

namespace Swa.Analyzers.Architecture.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arc006AvoidDomainEntitiesInHttpContractsAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Architecture";
    private const string AspNetCoreBuilderNamespace = "Microsoft.AspNetCore.Builder";
    private const string AspNetCoreMvcNamespace = "Microsoft.AspNetCore.Mvc";
    private const string AspNetCoreHttpNamespace = "Microsoft.AspNetCore.Http";
    private const string AspNetCoreHttpResultsNamespace = "Microsoft.AspNetCore.Http.HttpResults";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.AvoidDomainEntitiesInHttpContracts,
        title: "Evitar entidades de dominio diretamente em contratos HTTP",
        messageFormat: "HTTP contract '{0}' exposes domain entity type '{1}'. Prefer an explicit request or response contract when this architecture policy applies.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "HTTP endpoints should not expose domain entities directly when the project separates domain model from external contracts.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.AvoidDomainEntitiesInHttpContracts));

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
        "MapDelete");

    private static readonly ImmutableHashSet<string> KnownCollectionTypeNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "IEnumerable",
        "IReadOnlyCollection",
        "IReadOnlyList",
        "ICollection",
        "IList",
        "List");

    private static readonly ImmutableHashSet<string> KnownTypedResultTypeNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "Ok",
        "Created",
        "CreatedAtAction",
        "CreatedAtRoute");

    private static readonly ImmutableHashSet<string> KnownInfrastructureParameterTypeNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "CancellationToken",
        "HttpContext",
        "HttpRequest",
        "HttpResponse",
        "ClaimsPrincipal",
        "IFormFile",
        "IFormFileCollection",
        "IServiceProvider");

    private static readonly ImmutableHashSet<string> KnownServiceBindingAttributeNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "FromServicesAttribute",
        "FromKeyedServicesAttribute");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var controllerBaseType = compilationContext.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerBase");
            var controllerType = compilationContext.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Controller");
            var optionsCache = new DomainEntityClassifier.DomainEntityOptionsCache(compilationContext.Options.AnalyzerConfigOptionsProvider);

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
        DomainEntityClassifier.DomainEntityOptionsCache optionsCache)
    {
        var method = (IMethodSymbol)context.Symbol;

        if (method.MethodKind != MethodKind.Ordinary
            || method.ContainingType is null
            || method.DeclaredAccessibility != Accessibility.Public
            || method.ContainingType.IsAbstract
            || !IsControllerType(method.ContainingType, controllerBaseType, controllerType)
            || !IsMvcAction(method))
        {
            return;
        }

        var syntaxReference = method.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxReference?.GetSyntax(context.CancellationToken) is not MethodDeclarationSyntax methodDeclaration)
        {
            return;
        }

        var options = optionsCache.Get(methodDeclaration.SyntaxTree);

        foreach (var parameter in method.Parameters)
        {
            if (IsInfrastructureParameter(parameter)
                || !TryFindDomainEntity(parameter.Type, options, out var entityType))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                parameter.Locations.FirstOrDefault() ?? methodDeclaration.Identifier.GetLocation(),
                method.ContainingType.Name + "." + method.Name + " parameter '" + parameter.Name + "'",
                entityType.Name));
        }

        if (TryFindDomainEntity(method.ReturnType, options, out var returnEntityType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                methodDeclaration.ReturnType.GetLocation(),
                method.ContainingType.Name + "." + method.Name + " return value",
                returnEntityType.Name));
        }
    }

    private static void AnalyzeMinimalApiInvocation(
        SyntaxNodeAnalysisContext context,
        DomainEntityClassifier.DomainEntityOptionsCache optionsCache)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method
            || !IsKnownMinimalApiMapInvocation(invocation, method, context.SemanticModel, context.CancellationToken)
            || !TryGetMinimalApiHandler(invocation, context.SemanticModel, context.CancellationToken, out var handler))
        {
            return;
        }

        var options = optionsCache.Get(invocation.SyntaxTree);

        foreach (var parameter in handler.Parameters)
        {
            if (IsInfrastructureParameter(parameter.Symbol)
                || !TryFindDomainEntity(parameter.Type, options, out var entityType))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                parameter.Location,
                method.Name + " handler parameter '" + parameter.Symbol.Name + "'",
                entityType.Name));
        }

        if (handler.ReturnType is not null
            && TryFindDomainEntity(handler.ReturnType, options, out var returnEntityType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                handler.ReturnLocation,
                method.Name + " handler return value",
                returnEntityType.Name));
        }
    }

    private static bool IsMvcAction(IMethodSymbol method)
    {
        if (HasKnownHttpRouteAttribute(method))
        {
            return true;
        }

        return HasKnownHttpRouteAttribute(method.ContainingType)
            && method.DeclaredAccessibility == Accessibility.Public
            && !method.IsStatic
            && !method.IsGenericMethod;
    }

    private static bool HasKnownHttpRouteAttribute(ISymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            for (INamedTypeSymbol? current = attribute.AttributeClass; current is not null; current = current.BaseType)
            {
                if (KnownHttpRouteAttributeTypeNames.Contains(current.MetadataName)
                    && string.Equals(current.ContainingNamespace?.ToDisplayString(), AspNetCoreMvcNamespace, StringComparison.Ordinal))
                {
                    return true;
                }
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

    private static bool TryGetMinimalApiHandler(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken,
        out HandlerSignature handler)
    {
        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            var expression = argument.Expression;

            if (semanticModel.GetConstantValue(expression, cancellationToken) is { HasValue: true, Value: string })
            {
                continue;
            }

            if (expression is LambdaExpressionSyntax lambda)
            {
                return TryCreateHandlerFromLambda(lambda, semanticModel, cancellationToken, out handler);
            }

            if (semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol is IMethodSymbol method)
            {
                handler = CreateHandlerFromMethod(method, expression.GetLocation());
                return true;
            }

            var convertedType = semanticModel.GetTypeInfo(expression, cancellationToken).ConvertedType;
            if (convertedType is INamedTypeSymbol { DelegateInvokeMethod: { } invokeMethod })
            {
                handler = CreateHandlerFromMethod(invokeMethod, expression.GetLocation());
                return true;
            }
        }

        handler = default;
        return false;
    }

    private static bool TryCreateHandlerFromLambda(
        LambdaExpressionSyntax lambda,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken,
        out HandlerSignature handler)
    {
        var parameters = ImmutableArray.CreateBuilder<HandlerParameter>();

        foreach (var parameterSyntax in GetLambdaParameters(lambda))
        {
            if (semanticModel.GetDeclaredSymbol(parameterSyntax, cancellationToken) is IParameterSymbol parameterSymbol)
            {
                parameters.Add(new HandlerParameter(
                    parameterSymbol,
                    parameterSymbol.Type,
                    parameterSyntax.GetLocation()));
            }
        }

        var returnType = TryGetLambdaReturnType(lambda, semanticModel, cancellationToken);
        handler = new HandlerSignature(parameters.ToImmutable(), returnType, GetLambdaReturnLocation(lambda));
        return parameters.Count > 0 || returnType is not null;
    }

    private static IEnumerable<ParameterSyntax> GetLambdaParameters(LambdaExpressionSyntax lambda)
    {
        if (lambda is SimpleLambdaExpressionSyntax simpleLambda)
        {
            yield return simpleLambda.Parameter;
            yield break;
        }

        if (lambda is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
        {
            foreach (var parameter in parenthesizedLambda.ParameterList.Parameters)
            {
                yield return parameter;
            }
        }
    }

    private static ITypeSymbol? TryGetLambdaReturnType(
        LambdaExpressionSyntax lambda,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        if (semanticModel.GetSymbolInfo(lambda, cancellationToken).Symbol is IMethodSymbol lambdaSymbol
            && !IsVoid(lambdaSymbol.ReturnType))
        {
            return lambdaSymbol.ReturnType;
        }

        var convertedType = semanticModel.GetTypeInfo(lambda, cancellationToken).ConvertedType;
        if (convertedType is INamedTypeSymbol { DelegateInvokeMethod: { } invokeMethod }
            && !IsVoid(invokeMethod.ReturnType))
        {
            return invokeMethod.ReturnType;
        }

        return null;
    }

    private static Location GetLambdaReturnLocation(LambdaExpressionSyntax lambda)
    {
        return lambda.ExpressionBody?.GetLocation()
            ?? lambda.Block?.GetLocation()
            ?? lambda.GetLocation();
    }

    private static HandlerSignature CreateHandlerFromMethod(IMethodSymbol method, Location location)
    {
        var parameters = method.Parameters
            .Select(static parameter => new HandlerParameter(
                parameter,
                parameter.Type,
                parameter.Locations.FirstOrDefault() ?? Location.None))
            .ToImmutableArray();

        return new HandlerSignature(parameters, IsVoid(method.ReturnType) ? null : method.ReturnType, location);
    }

    private static bool TryFindDomainEntity(
        ITypeSymbol type,
        DomainEntityClassifier.DomainEntityOptions options,
        out INamedTypeSymbol entityType)
    {
        if (type is IArrayTypeSymbol arrayType)
        {
            return TryFindDomainEntity(arrayType.ElementType, options, out entityType);
        }

        if (type is not INamedTypeSymbol namedType)
        {
            entityType = null!;
            return false;
        }

        if (DomainEntityClassifier.IsDomainEntity(namedType, options))
        {
            entityType = namedType;
            return true;
        }

        if (TryGetKnownWrapperTypeArguments(namedType, out var typeArguments))
        {
            foreach (var typeArgument in typeArguments)
            {
                if (TryFindDomainEntity(typeArgument, options, out entityType))
                {
                    return true;
                }
            }
        }

        entityType = null!;
        return false;
    }

    private static bool TryGetKnownWrapperTypeArguments(
        INamedTypeSymbol type,
        out ImmutableArray<ITypeSymbol> typeArguments)
    {
        typeArguments = type.TypeArguments;

        if (typeArguments.Length == 0)
        {
            return false;
        }

        var namespaceName = type.ContainingNamespace?.ToDisplayString();

        if ((string.Equals(type.Name, "Task", StringComparison.Ordinal)
                || string.Equals(type.Name, "ValueTask", StringComparison.Ordinal))
            && string.Equals(namespaceName, "System.Threading.Tasks", StringComparison.Ordinal)
            && typeArguments.Length == 1)
        {
            return true;
        }

        if (string.Equals(type.Name, "ActionResult", StringComparison.Ordinal)
            && string.Equals(namespaceName, AspNetCoreMvcNamespace, StringComparison.Ordinal)
            && typeArguments.Length == 1)
        {
            return true;
        }

        if (KnownCollectionTypeNames.Contains(type.Name)
            && string.Equals(namespaceName, "System.Collections.Generic", StringComparison.Ordinal)
            && typeArguments.Length == 1)
        {
            return true;
        }

        if (KnownTypedResultTypeNames.Contains(type.Name)
            && string.Equals(namespaceName, AspNetCoreHttpResultsNamespace, StringComparison.Ordinal)
            && typeArguments.Length == 1)
        {
            return true;
        }

        return string.Equals(type.Name, "Results", StringComparison.Ordinal)
            && string.Equals(namespaceName, AspNetCoreHttpResultsNamespace, StringComparison.Ordinal);
    }

    private static bool IsInfrastructureParameter(IParameterSymbol parameter)
    {
        if (HasServiceBindingAttribute(parameter))
        {
            return true;
        }

        var type = parameter.Type;

        if (type.SpecialType != SpecialType.None)
        {
            return true;
        }

        var namespaceName = type.ContainingNamespace?.ToDisplayString();

        return KnownInfrastructureParameterTypeNames.Contains(type.Name)
            || string.Equals(namespaceName, AspNetCoreHttpNamespace, StringComparison.Ordinal)
            || string.Equals(namespaceName, AspNetCoreMvcNamespace, StringComparison.Ordinal)
            || string.Equals(namespaceName, "System.Security.Claims", StringComparison.Ordinal)
            || string.Equals(namespaceName, "Microsoft.Extensions.DependencyInjection", StringComparison.Ordinal);
    }

    private static bool HasServiceBindingAttribute(IParameterSymbol parameter)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            if (attribute.AttributeClass is { } attributeType
                && KnownServiceBindingAttributeNames.Contains(attributeType.MetadataName))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsVoid(ITypeSymbol type)
    {
        return type.SpecialType == SpecialType.System_Void;
    }

    private readonly struct HandlerSignature
    {
        public HandlerSignature(
            ImmutableArray<HandlerParameter> parameters,
            ITypeSymbol? returnType,
            Location returnLocation)
        {
            Parameters = parameters;
            ReturnType = returnType;
            ReturnLocation = returnLocation;
        }

        public ImmutableArray<HandlerParameter> Parameters
        {
            get;
        }

        public ITypeSymbol? ReturnType
        {
            get;
        }

        public Location ReturnLocation
        {
            get;
        }
    }

    private readonly struct HandlerParameter
    {
        public HandlerParameter(IParameterSymbol symbol, ITypeSymbol type, Location location)
        {
            Symbol = symbol;
            Type = type;
            Location = location;
        }

        public IParameterSymbol Symbol
        {
            get;
        }

        public ITypeSymbol Type
        {
            get;
        }

        public Location Location
        {
            get;
        }
    }
}
