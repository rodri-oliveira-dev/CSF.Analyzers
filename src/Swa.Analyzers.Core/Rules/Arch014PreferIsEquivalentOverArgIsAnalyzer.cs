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
        helpLinkUri: "docs/rules/ARCH014.md");

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
                // Avoid noise outside test projects.
                return;
            }

            var nsubstituteArgType = compilationContext.Compilation.GetTypeByMetadataName("NSubstitute.Arg");
            if (nsubstituteArgType is null)
            {
                // Avoid false positives when NSubstitute isn't referenced.
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
            // Ensure we only target NSubstitute.Arg.Is()
            return;
        }

        if (!TestContextHelper.IsWithinTestContext(context.ContainingSymbol, testMethodAttributes, isTestTypeCache))
        {
            return;
        }

        // Do not provide a CodeFix because the replacement (Is.Equivalent) may not be universally applicable.
        // For example, if the Arg.Is predicate is complex or stateful, a simple Is.Equivalent call might not be equivalent.
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
