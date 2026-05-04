using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch010EnforceCancellationTokenPropagationAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Reliability";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.EnforceCancellationTokenPropagation,
        title: "Enforce CancellationToken propagation",
        messageFormat: "Pass the available CancellationToken to '{0}'. This method has an overload or optional parameter that accepts CancellationToken.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When a CancellationToken is available in the current scope and the invoked method can accept one, the token should be passed to enable cooperative cancellation and improve responsiveness.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.EnforceCancellationTokenPropagation));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var cancellationTokenType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
            if (cancellationTokenType is null)
            {
                return;
            }

            compilationContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeInvocation(syntaxContext, cancellationTokenType),
                SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, INamedTypeSymbol cancellationTokenType)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        if (semanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol targetMethod)
        {
            return;
        }

        // Check if the invoked method has an optional/unsupplied CancellationToken parameter,
        // or if there's an overload that accepts CancellationToken before inspecting arguments.
        if (!CanAcceptCancellationToken(targetMethod, invocation, cancellationTokenType))
        {
            return;
        }

        // Skip if the invocation already passes a CancellationToken.
        if (HasCancellationTokenArgument(invocation, targetMethod, semanticModel, cancellationTokenType, context.CancellationToken))
        {
            return;
        }

        // Check if a CancellationToken is available in the current scope.
        if (!HasAvailableCancellationTokenInScope(context, invocation, cancellationTokenType))
        {
            return;
        }

        var location = GetMethodNameLocation(invocation);
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, targetMethod.Name));
    }

    private static bool CanAcceptCancellationToken(
        IMethodSymbol targetMethod,
        InvocationExpressionSyntax invocation,
        INamedTypeSymbol cancellationTokenType)
    {
        return HasUnsuppliedCancellationTokenParameter(targetMethod, invocation, cancellationTokenType)
            || HasOverloadWithCancellationToken(targetMethod, cancellationTokenType);
    }

    private static bool HasCancellationTokenArgument(
        InvocationExpressionSyntax invocation,
        IMethodSymbol targetMethod,
        SemanticModel semanticModel,
        INamedTypeSymbol cancellationTokenType,
        CancellationToken cancellationToken)
    {
        if (invocation.ArgumentList is null)
        {
            return false;
        }

        var arguments = invocation.ArgumentList.Arguments;

        for (int i = 0; i < arguments.Count; i++)
        {
            var argument = arguments[i];
            var argumentTypeInfo = semanticModel.GetTypeInfo(argument.Expression, cancellationToken);
            var argumentType = argumentTypeInfo.Type;

            if (argumentType is not null && SymbolEqualityComparer.Default.Equals(argumentType, cancellationTokenType))
            {
                return true;
            }

            // Also check if this argument maps to a CancellationToken parameter.
            var parameter = GetParameterForArgument(targetMethod, invocation, i);
            if (parameter is not null && SymbolEqualityComparer.Default.Equals(parameter.Type, cancellationTokenType))
            {
                return true;
            }
        }

        return false;
    }

    private static IParameterSymbol? GetParameterForArgument(
        IMethodSymbol method,
        InvocationExpressionSyntax invocation,
        int argumentIndex)
    {
        var argument = invocation.ArgumentList!.Arguments[argumentIndex];

        // If named argument, find parameter by name.
        if (argument.NameColon is not null)
        {
            var name = argument.NameColon.Name.Identifier.ValueText;
            foreach (var parameter in method.Parameters)
            {
                if (parameter.Name == name)
                {
                    return parameter;
                }
            }

            return null;
        }

        // Positional argument.
        if (argumentIndex < method.Parameters.Length)
        {
            return method.Parameters[argumentIndex];
        }

        // Could be params parameter.
        if (method.Parameters.Length > 0 && method.Parameters[method.Parameters.Length - 1].IsParams)
        {
            return method.Parameters[method.Parameters.Length - 1];
        }

        return null;
    }

    private static bool HasUnsuppliedCancellationTokenParameter(
        IMethodSymbol method,
        InvocationExpressionSyntax invocation,
        INamedTypeSymbol cancellationTokenType)
    {
        foreach (var parameter in method.Parameters)
        {
            if (!SymbolEqualityComparer.Default.Equals(parameter.Type, cancellationTokenType))
            {
                continue;
            }

            bool supplied = IsParameterSupplied(parameter, method, invocation);
            if (!supplied)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsParameterSupplied(
        IParameterSymbol parameter,
        IMethodSymbol method,
        InvocationExpressionSyntax invocation)
    {
        if (invocation.ArgumentList is null)
        {
            return false;
        }

        var arguments = invocation.ArgumentList.Arguments;

        for (int i = 0; i < arguments.Count; i++)
        {
            var mappedParameter = GetParameterForArgument(method, invocation, i);
            if (mappedParameter is not null && SymbolEqualityComparer.Default.Equals(mappedParameter, parameter))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasOverloadWithCancellationToken(IMethodSymbol method, INamedTypeSymbol cancellationTokenType)
    {
        var methodToCheck = method.ReducedFrom ?? method;
        var containingType = methodToCheck.ContainingType;
        if (containingType is null)
        {
            return false;
        }

        foreach (var member in containingType.GetMembers(methodToCheck.Name))
        {
            if (member is not IMethodSymbol candidate)
            {
                continue;
            }

            if (candidate.Equals(methodToCheck, SymbolEqualityComparer.Default))
            {
                continue;
            }

            var methodParams = method.Parameters;
            if (method.ReducedFrom is not null)
            {
                methodParams = methodToCheck.Parameters;
            }

            var candidateParams = candidate.Parameters;

            // Heuristic: overload has exactly one more parameter and the last one is CancellationToken.
            if (candidateParams.Length != methodParams.Length + 1)
            {
                continue;
            }

            bool prefixMatches = true;
            for (int i = 0; i < methodParams.Length; i++)
            {
                if (!ParameterTypesMatch(methodParams[i].Type, candidateParams[i].Type))
                {
                    prefixMatches = false;
                    break;
                }
            }

            if (prefixMatches
                && SymbolEqualityComparer.Default.Equals(candidateParams[candidateParams.Length - 1].Type, cancellationTokenType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ParameterTypesMatch(ITypeSymbol left, ITypeSymbol right)
    {
        if (SymbolEqualityComparer.Default.Equals(left, right))
        {
            return true;
        }

        return left is INamedTypeSymbol leftNamed
            && right is INamedTypeSymbol rightNamed
            && SymbolEqualityComparer.Default.Equals(leftNamed.OriginalDefinition, rightNamed.OriginalDefinition);
    }

    private static bool HasAvailableCancellationTokenInScope(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        INamedTypeSymbol cancellationTokenType)
    {
        var semanticModel = context.SemanticModel;

        if (HasCancellationTokenParameterInEnclosingDeclaration(
            invocation,
            semanticModel,
            cancellationTokenType,
            context.CancellationToken))
        {
            return true;
        }

        var symbols = semanticModel.LookupSymbols(invocation.SpanStart);

        foreach (var symbol in symbols)
        {
            if (symbol is IParameterSymbol parameter && SymbolEqualityComparer.Default.Equals(parameter.Type, cancellationTokenType))
            {
                return true;
            }

            if (symbol is ILocalSymbol local && SymbolEqualityComparer.Default.Equals(local.Type, cancellationTokenType))
            {
                return true;
            }

            if (symbol is IFieldSymbol field && SymbolEqualityComparer.Default.Equals(field.Type, cancellationTokenType))
            {
                return true;
            }

            if (symbol is IPropertySymbol property && SymbolEqualityComparer.Default.Equals(property.Type, cancellationTokenType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasCancellationTokenParameterInEnclosingDeclaration(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        INamedTypeSymbol cancellationTokenType,
        CancellationToken cancellationToken)
    {
        foreach (var ancestor in invocation.Ancestors())
        {
            switch (ancestor)
            {
                case MethodDeclarationSyntax methodDeclaration
                    when HasCancellationTokenParameter(
                        methodDeclaration.ParameterList.Parameters,
                        semanticModel,
                        cancellationTokenType,
                        cancellationToken):
                case LocalFunctionStatementSyntax localFunction
                    when HasCancellationTokenParameter(
                        localFunction.ParameterList.Parameters,
                        semanticModel,
                        cancellationTokenType,
                        cancellationToken):
                case ParenthesizedLambdaExpressionSyntax parenthesizedLambda
                    when HasCancellationTokenParameter(
                        parenthesizedLambda.ParameterList.Parameters,
                        semanticModel,
                        cancellationTokenType,
                        cancellationToken):
                case SimpleLambdaExpressionSyntax simpleLambda
                    when HasCancellationTokenParameter(
                        SyntaxFactory.SingletonSeparatedList(simpleLambda.Parameter),
                        semanticModel,
                        cancellationTokenType,
                        cancellationToken):
                case AnonymousMethodExpressionSyntax anonymousMethod
                    when anonymousMethod.ParameterList is not null
                        && HasCancellationTokenParameter(
                            anonymousMethod.ParameterList.Parameters,
                            semanticModel,
                            cancellationTokenType,
                            cancellationToken):
                    return true;
            }
        }

        return false;
    }

    private static bool HasCancellationTokenParameter(
        SeparatedSyntaxList<ParameterSyntax> parameters,
        SemanticModel semanticModel,
        INamedTypeSymbol cancellationTokenType,
        CancellationToken cancellationToken)
    {
        foreach (var parameter in parameters)
        {
            if (semanticModel.GetDeclaredSymbol(parameter, cancellationToken) is IParameterSymbol parameterSymbol
                && SymbolEqualityComparer.Default.Equals(parameterSymbol.Type, cancellationTokenType))
            {
                return true;
            }
        }

        return false;
    }

    private static Location GetMethodNameLocation(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.GetLocation(),
            MemberBindingExpressionSyntax memberBinding => memberBinding.Name.GetLocation(),
            _ => invocation.GetLocation(),
        };
    }
}
