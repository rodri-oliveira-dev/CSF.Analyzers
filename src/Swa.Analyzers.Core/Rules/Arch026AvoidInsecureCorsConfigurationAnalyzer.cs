using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch026AvoidInsecureCorsConfigurationAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Security";
    private const string DisallowAnyOriginOption = "dotnet_diagnostic.ARCH026.disallow_any_origin";
    private const string AspNetCoreCorsInfrastructureNamespace = "Microsoft.AspNetCore.Cors.Infrastructure";
    private const string CorsPolicyBuilderTypeName = "CorsPolicyBuilder";
    private const string AllowAnyOriginMethodName = "AllowAnyOrigin";
    private const string AllowCredentialsMethodName = "AllowCredentials";
    private const string PolicyArgumentName = "policy";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.AvoidInsecureCorsConfiguration,
        title: "Evitar configuracao insegura de CORS",
        messageFormat: "Avoid combining '{0}' with '{1}' in ASP.NET Core CORS policies. Prefer explicit origins when credentials are allowed.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "ASP.NET Core CORS policies should not combine wildcard origins with credentials. Use WithOrigins(...) when cookies, authorization headers, or client certificates are allowed.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.AvoidInsecureCorsConfiguration));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var optionsCache = new CorsRuleOptionsCache(compilationContext.Options.AnalyzerConfigOptionsProvider);
            var testMethodAttributes = TestContextHelper.GetKnownTestMethodAttributes(compilationContext.Compilation);
            var isTestTypeCache = new ConcurrentDictionary<INamedTypeSymbol, bool>(SymbolEqualityComparer.Default);

            compilationContext.RegisterSyntaxNodeAction(
                context => AnalyzeInvocation(context, optionsCache, testMethodAttributes, isTestTypeCache),
                SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        CorsRuleOptionsCache optionsCache,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.ContainingSymbol is not null
            && !testMethodAttributes.IsDefaultOrEmpty
            && TestContextHelper.IsWithinTestContext(context.ContainingSymbol, testMethodAttributes, isTestTypeCache))
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method)
        {
            return;
        }

        if (!IsCorsPolicyBuilderMethod(method))
        {
            return;
        }

        if (string.Equals(method.Name, AllowAnyOriginMethodName, StringComparison.Ordinal))
        {
            AnalyzeAllowAnyOrigin(context, invocation, optionsCache);
            return;
        }

        if (string.Equals(method.Name, AllowCredentialsMethodName, StringComparison.Ordinal)
            && ReceiverChainHasCorsMethod(invocation, AllowAnyOriginMethodName, context.SemanticModel, context.CancellationToken))
        {
            Report(context, invocation, AllowCredentialsMethodName, AllowAnyOriginMethodName);
        }
    }

    private static void AnalyzeAllowAnyOrigin(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        CorsRuleOptionsCache optionsCache)
    {
        if (ReceiverChainHasCorsMethod(invocation, AllowCredentialsMethodName, context.SemanticModel, context.CancellationToken))
        {
            Report(context, invocation, AllowAnyOriginMethodName, AllowCredentialsMethodName);
            return;
        }

        if (!optionsCache.Get(invocation.SyntaxTree).DisallowAnyOrigin)
        {
            return;
        }

        if (OuterChainHasCorsMethod(invocation, AllowCredentialsMethodName, context.SemanticModel, context.CancellationToken))
        {
            return;
        }

        Report(context, invocation, AllowAnyOriginMethodName, PolicyArgumentName);
    }

    private static bool ReceiverChainHasCorsMethod(
        InvocationExpressionSyntax invocation,
        string methodName,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        foreach (var receiverInvocation in EnumerateReceiverInvocations(invocation))
        {
            if (semanticModel.GetSymbolInfo(receiverInvocation, cancellationToken).Symbol is not IMethodSymbol method)
            {
                continue;
            }

            if (string.Equals(method.Name, methodName, StringComparison.Ordinal)
                && IsCorsPolicyBuilderMethod(method))
            {
                return true;
            }
        }

        return false;
    }

    private static bool OuterChainHasCorsMethod(
        InvocationExpressionSyntax invocation,
        string methodName,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        foreach (var outerInvocation in EnumerateOuterInvocations(invocation))
        {
            if (semanticModel.GetSymbolInfo(outerInvocation, cancellationToken).Symbol is not IMethodSymbol method)
            {
                continue;
            }

            if (string.Equals(method.Name, methodName, StringComparison.Ordinal)
                && IsCorsPolicyBuilderMethod(method))
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

    private static bool IsCorsPolicyBuilderMethod(IMethodSymbol method)
    {
        var containingType = method.ContainingType;

        return containingType is not null
            && string.Equals(containingType.Name, CorsPolicyBuilderTypeName, StringComparison.Ordinal)
            && string.Equals(containingType.ContainingNamespace?.ToDisplayString(), AspNetCoreCorsInfrastructureNamespace, StringComparison.Ordinal);
    }

    private static void Report(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        string currentMethod,
        string conflictingMethod)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            GetInvocationNameLocation(invocation),
            currentMethod,
            conflictingMethod));
    }

    private static Location GetInvocationNameLocation(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression is MemberAccessExpressionSyntax memberAccess
            ? memberAccess.Name.GetLocation()
            : invocation.Expression.GetLocation();
    }

    private sealed class CorsRuleOptionsCache
    {
        private readonly AnalyzerConfigOptionsProvider _provider;
        private readonly ConcurrentDictionary<SyntaxTree, CorsRuleOptions> _optionsBySyntaxTree = new();

        public CorsRuleOptionsCache(AnalyzerConfigOptionsProvider provider)
        {
            _provider = provider;
        }

        public CorsRuleOptions Get(SyntaxTree syntaxTree)
        {
            return _optionsBySyntaxTree.GetOrAdd(syntaxTree, CreateOptions);
        }

        private CorsRuleOptions CreateOptions(SyntaxTree syntaxTree)
        {
            return CorsRuleOptions.Create(_provider, syntaxTree);
        }
    }

    private readonly struct CorsRuleOptions
    {
        private CorsRuleOptions(bool disallowAnyOrigin)
        {
            DisallowAnyOrigin = disallowAnyOrigin;
        }

        public bool DisallowAnyOrigin
        {
            get;
        }

        public static CorsRuleOptions Create(AnalyzerConfigOptionsProvider provider, SyntaxTree syntaxTree)
        {
            var options = provider.GetOptions(syntaxTree);
            var disallowAnyOrigin = options.TryGetValue(DisallowAnyOriginOption, out var configuredValue)
                && string.Equals(configuredValue.Trim(), "true", StringComparison.OrdinalIgnoreCase);

            return new CorsRuleOptions(disallowAnyOrigin);
        }
    }
}
