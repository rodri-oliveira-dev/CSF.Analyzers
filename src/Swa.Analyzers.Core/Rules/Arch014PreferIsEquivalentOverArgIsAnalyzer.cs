using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch014PreferIsEquivalentOverArgIsAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "TestQuality";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.PreferIsEquivalentOverArgIs,
        title: "Prefer Is.Equivalent over Arg.Is",
        messageFormat: "Prefer Is.Equivalent from the team's standard library instead of NSubstitute Arg.Is for value matching",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Arg.Is is a matcher provided by NSubstitute. When the team has a standard library that offers Is.Equivalent, prefer using that matcher to maintain consistency and avoid unnecessary dependencies.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.PreferIsEquivalentOverArgIs));

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

        if (!string.Equals(targetMethod.Name, "Is", StringComparison.Ordinal))
        {
            return;
        }

        if (!SymbolEqualityComparer.Default.Equals(targetMethod.ContainingType, nsubstituteArgType))
        {
            // Garante que apenas NSubstitute.Arg.Is() seja alvo.
            return;
        }

        if (!TestContextHelper.IsWithinTestContext(context.ContainingSymbol, testMethodAttributes, isTestTypeCache))
        {
            return;
        }

        // Não fornece CodeFix porque a substituição (Is.Equivalent) pode não ser universalmente aplicável.
        // Por exemplo, se o predicado Arg.Is for complexo ou tiver estado, uma chamada simples a Is.Equivalent pode não ser equivalente.
        context.ReportDiagnostic(Diagnostic.Create(Rule, GetArgIsLocation(invocation.Syntax)));
    }

    private static Location GetArgIsLocation(SyntaxNode syntax)
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
