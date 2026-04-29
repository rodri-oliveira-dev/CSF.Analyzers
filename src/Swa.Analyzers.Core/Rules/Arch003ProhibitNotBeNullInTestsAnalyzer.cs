using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch003ProhibitNotBeNullInTestsAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "TestQuality";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.ProhibitNotBeNullInTests,
        title: "Prohibit NotBeNull() in tests",
        messageFormat: "Avoid NotBeNull() in tests. Prefer a more specific assertion when possible.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "NotBeNull() is a weak assertion that often hides intent. Prefer more specific assertions (for example NotBeNullOrEmpty, BeOfType, BeAssignableTo, HaveValue) to improve test clarity.",
        helpLinkUri: "docs/rules/ARCH003.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var testMethodAttributes = TestContextHelper.GetKnownTestMethodAttributes(compilationContext.Compilation);
            if (testMethodAttributes.IsDefaultOrEmpty)
            {
                // Avoid false positives outside test projects.
                return;
            }

            var isTestTypeCache = new ConcurrentDictionary<INamedTypeSymbol, bool>(SymbolEqualityComparer.Default);

            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeInvocation(operationContext, testMethodAttributes, isTestTypeCache),
                OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var targetMethod = invocation.TargetMethod;

        if (!string.Equals(targetMethod.Name, "NotBeNull", StringComparison.Ordinal))
        {
            return;
        }

        if (!IsFluentAssertionsMethod(targetMethod))
        {
            return;
        }

        if (!TestContextHelper.IsWithinTestContext(context.ContainingSymbol, testMethodAttributes, isTestTypeCache))
        {
            // Limit the rule to actual test contexts.
            return;
        }

        var location = GetNotBeNullLocation(invocation.Syntax);
        context.ReportDiagnostic(Diagnostic.Create(Rule, location));
    }

    private static bool IsFluentAssertionsMethod(IMethodSymbol method)
    {
        var containingType = method.ContainingType;
        if (containingType is null)
        {
            return false;
        }

        return IsInFluentAssertionsNamespace(containingType.ContainingNamespace);
    }

    private static bool IsInFluentAssertionsNamespace(INamespaceSymbol? @namespace)
    {
        for (var current = @namespace; current is not null && !current.IsGlobalNamespace; current = current.ContainingNamespace)
        {
            if (string.Equals(current.Name, "FluentAssertions", StringComparison.Ordinal)
                && current.ContainingNamespace.IsGlobalNamespace)
            {
                return true;
            }
        }

        return false;
    }

    private static Location GetNotBeNullLocation(SyntaxNode syntax)
    {
        return syntax switch
        {
            InvocationExpressionSyntax invocation => invocation.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.GetLocation(),
                MemberBindingExpressionSyntax memberBinding => memberBinding.Name.GetLocation(),
                _ => invocation.GetLocation(),
            },
            _ => syntax.GetLocation(),
        };
    }
}
