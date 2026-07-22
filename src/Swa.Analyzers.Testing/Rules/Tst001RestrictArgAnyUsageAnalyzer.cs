using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.CodeAnalysis.Diagnostics;

using Microsoft.CodeAnalysis.Operations;

using Swa.Analyzers.Common;
using Swa.Analyzers.Testing;

namespace Swa.Analyzers.Testing.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Tst001RestrictArgAnyUsageAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "TestQuality";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.RestrictArgAnyUsage,
        title: "Restrict usage of NSubstitute Arg.Any()",
        messageFormat: "Avoid NSubstitute Arg.Any() outside the allowed convention. Use DidNotReceive/DidNotReceiveWithAnyArgs instead.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "Arg.Any() is a very broad matcher that can hide intent and make tests less precise. This rule restricts Arg.Any() usage to specific negative-assertion conventions (DidNotReceive/DidNotReceiveWithAnyArgs), where broad matching is explicitly accepted.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.RestrictArgAnyUsage));

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

            var nsubstituteArgType = compilationContext.Compilation.GetTypeByMetadataName("NSubstitute.Arg");
            if (nsubstituteArgType is null)
            {
                // Evita falsos positivos quando NSubstitute não é referenciado.
                return;
            }

            var isTestTypeCache = new ConcurrentDictionary<INamedTypeSymbol, bool>(SymbolEqualityComparer.Default);

            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeInvocation(operationContext, nsubstituteArgType, testMethodAttributes, isTestTypeCache),
                OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        INamedTypeSymbol nsubstituteArgType,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var targetMethod = invocation.TargetMethod;

        if (!string.Equals(targetMethod.Name, "Any", StringComparison.Ordinal))
        {
            return;
        }

        if (!SymbolEqualityComparer.Default.Equals(targetMethod.ContainingType, nsubstituteArgType))
        {
            // Garante que apenas NSubstitute.Arg.Any() seja alvo.
            return;
        }

        if (!TestContextHelper.IsWithinTestContext(context.ContainingSymbol, testMethodAttributes, isTestTypeCache))
        {
            return;
        }

        if (IsAllowedByConvention(invocation))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, GetArgAnyLocation(invocation.Syntax)));
    }

    private static bool IsAllowedByConvention(IInvocationOperation argAnyInvocation)
    {
        // Convenção: permite Arg.Any() apenas quando usado diretamente como argumento
        // de uma invocação em cadeia precedida por DidNotReceive()/DidNotReceiveWithAnyArgs().
        // Exemplo:
        //   substitute.DidNotReceive().Foo(Arg.Any<int>());

        // Usa intencionalmente a árvore de *operation* (semântica) para localizar a invocação dona do argumento,
        // o que é robusto a casts/conversões em torno de Arg.Any().

        IOperation? current = argAnyInvocation;
        while (current is not null)
        {
            if (current.Parent is IArgumentOperation argumentOperation)
            {
                // O próximo pai deve ser a invocação que recebe o argumento.
                if (argumentOperation.Parent is IInvocationOperation receivingInvocation)
                {
                    if (!IsDirectArgumentValue(argumentOperation, argAnyInvocation))
                    {
                        return false;
                    }

                    return HasDidNotReceiveInReceiverChain(receivingInvocation);
                }
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool IsDirectArgumentValue(IArgumentOperation argumentOperation, IInvocationOperation argAnyInvocation)
    {
        // Permite apenas quando Arg.Any() é o próprio valor do argumento, ignorando conversões implícitas.
        // Isso evita permitir uso de "matcher" dentro de expressões como: Foo(Arg.Any<int>() + 1).
        IOperation? value = argumentOperation.Value;

        while (value is IConversionOperation conversion)
        {
            value = conversion.Operand;
        }

        return ReferenceEquals(value, argAnyInvocation);
    }

    private static bool HasDidNotReceiveInReceiverChain(IInvocationOperation receivingInvocation)
    {
        // Queremos: X.DidNotReceive().Foo(...Arg.Any...)
        // Portanto, a instância de Foo é uma operação de invocação chamada DidNotReceive ou DidNotReceiveWithAnyArgs.
        var instance = receivingInvocation.Instance;
        if (instance is null)
        {
            return false;
        }

        if (instance is IInvocationOperation didNotReceiveInvocation)
        {
            return IsDidNotReceiveMethod(didNotReceiveInvocation.TargetMethod);
        }

        // Acesso condicional: `sub.DidNotReceive()?.Foo(Arg.Any<int>())`
        // Nesse formato, a Instance da invocação Foo é um placeholder (IConditionalAccessInstanceOperation)
        // e a invocação DidNotReceive fica disponível como Operation do condicional.
        if (instance is IConditionalAccessInstanceOperation
            && receivingInvocation.Parent is IConditionalAccessOperation conditionalAccess
            && conditionalAccess.Operation is IInvocationOperation conditionalReceiverInvocation)
        {
            return IsDidNotReceiveMethod(conditionalReceiverInvocation.TargetMethod);
        }

        return false;
    }

    private static bool IsDidNotReceiveMethod(IMethodSymbol method)
    {
        if (!string.Equals(method.Name, "DidNotReceive", StringComparison.Ordinal)
            && !string.Equals(method.Name, "DidNotReceiveWithAnyArgs", StringComparison.Ordinal))
        {
            return false;
        }

        // Evita permitir APIs customizadas parecidas.
        return IsInNSubstituteNamespace(method.ContainingNamespace);
    }

    private static bool IsInNSubstituteNamespace(INamespaceSymbol? @namespace)
    {
        for (var current = @namespace; current is not null && !current.IsGlobalNamespace; current = current.ContainingNamespace)
        {
            if (string.Equals(current.Name, "NSubstitute", StringComparison.Ordinal)
                && current.ContainingNamespace.IsGlobalNamespace)
            {
                return true;
            }
        }

        return false;
    }

    private static Location GetArgAnyLocation(SyntaxNode syntax)
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
