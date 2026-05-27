using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch006WarnOnExcludingInBeEquivalentToAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "TestQuality";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.WarnOnExcludingInBeEquivalentTo,
        title: "Warn on exclusions in BeEquivalentTo()",
        messageFormat: "Avoid using '{0}' in BeEquivalentTo() options. Exclusions can reduce test precision.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "FluentAssertions equivalency exclusions (Excluding*) can hide regressions by making tests less strict. Prefer asserting precise equivalency and use exclusions only when there is an explicit, documented reason.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.WarnOnExcludingInBeEquivalentTo));

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
                // Evita ruído fora de projetos de teste.
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

        if (!string.Equals(targetMethod.Name, "BeEquivalentTo", StringComparison.Ordinal))
        {
            return;
        }

        if (!IsFluentAssertionsMethod(targetMethod))
        {
            // Evita falsos positivos em APIs parecidas.
            return;
        }

        if (!TestContextHelper.IsWithinTestContext(context.ContainingSymbol, testMethodAttributes, isTestTypeCache))
        {
            return;
        }

        foreach (var argument in invocation.Arguments)
        {
            if (!TryGetAnonymousFunction(argument.Value, out var anonymousFunction))
            {
                continue;
            }

            ReportExcludingCallsInAnonymousFunctionBody(context, anonymousFunction);
        }
    }

    private static void ReportExcludingCallsInAnonymousFunctionBody(OperationAnalysisContext context, IAnonymousFunctionOperation anonymousFunction)
    {
        // Varre intencionalmente apenas o corpo do delegate de opções de BeEquivalentTo.
        // Isso mantém a verificação focada e evita varrer toda a syntax tree.
        var body = anonymousFunction.Body;
        if (body is null)
        {
            return;
        }

        var stack = new Stack<IOperation>();
        stack.Push(body);

        while (stack.Count > 0)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var current = stack.Pop();

            if (current is IInvocationOperation invocation
                && IsEquivalencyExcludingMethod(invocation.TargetMethod))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    GetInvocationMemberNameLocation(invocation.Syntax),
                    invocation.TargetMethod.Name));
            }

            foreach (var child in current.ChildOperations)
            {
                stack.Push(child);
            }
        }
    }

    private static bool TryGetAnonymousFunction(IOperation? operation, out IAnonymousFunctionOperation anonymousFunction)
    {
        anonymousFunction = null!;

        if (operation is null)
        {
            return false;
        }

        IOperation? current = operation;

        while (current is not null)
        {
            switch (current)
            {
                case IAnonymousFunctionOperation anon:
                    anonymousFunction = anon;
                    return true;

                case IConversionOperation conversion:
                    current = conversion.Operand;
                    continue;

                case IDelegateCreationOperation delegateCreation:
                    current = delegateCreation.Target;
                    continue;

                case IParenthesizedOperation parenthesized:
                    current = parenthesized.Operand;
                    continue;
            }

            break;
        }

        return false;
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

    private static bool IsEquivalencyExcludingMethod(IMethodSymbol method)
    {
        if (!method.Name.StartsWith("Excluding", StringComparison.Ordinal))
        {
            return false;
        }

        var containingType = method.ContainingType;
        if (containingType is null)
        {
            return false;
        }

        // Evita falsos positivos em APIs não relacionadas do FluentAssertions que usem o mesmo nome por coincidência.
        // Métodos Excluding* usados para ajustar BeEquivalentTo ficam em FluentAssertions.Equivalency.
        return IsInFluentAssertionsEquivalencyNamespace(containingType.ContainingNamespace);
    }

    private static bool IsInFluentAssertionsEquivalencyNamespace(INamespaceSymbol? @namespace)
    {
        // Corresponde a FluentAssertions.Equivalency e seus subnamespaces.
        for (var current = @namespace; current is not null && !current.IsGlobalNamespace; current = current.ContainingNamespace)
        {
            if (string.Equals(current.Name, "Equivalency", StringComparison.Ordinal)
                && current.ContainingNamespace is { IsGlobalNamespace: false } parent
                && string.Equals(parent.Name, "FluentAssertions", StringComparison.Ordinal)
                && parent.ContainingNamespace.IsGlobalNamespace)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInFluentAssertionsNamespace(INamespaceSymbol? @namespace)
    {
        // Corresponde a FluentAssertions e seus subnamespaces.
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

    private static Location GetInvocationMemberNameLocation(SyntaxNode syntax)
    {
        return syntax switch
        {
            InvocationExpressionSyntax invocation => invocation.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.GetLocation(),
                MemberBindingExpressionSyntax memberBinding => memberBinding.Name.GetLocation(),
                IdentifierNameSyntax identifierName => identifierName.Identifier.GetLocation(),
                GenericNameSyntax genericName => genericName.Identifier.GetLocation(),
                _ => invocation.GetLocation(),
            },
            _ => syntax.GetLocation(),
        };
    }
}
