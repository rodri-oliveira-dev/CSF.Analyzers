using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch018AvoidDirectHttpClientInstantiationAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Reliability";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.AvoidDirectHttpClientInstantiation,
        title: "Evitar instanciação direta de HttpClient",
        messageFormat: "Avoid direct HttpClient instantiation. Prefer IHttpClientFactory, typed clients, or an equivalent abstraction.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Directly instantiating HttpClient in application code can create lifetime, DNS refresh, and socket exhaustion issues. Prefer IHttpClientFactory, typed clients, or an explicit abstraction.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.AvoidDirectHttpClientInstantiation));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var httpClientType = compilationContext.Compilation.GetTypeByMetadataName("System.Net.Http.HttpClient");
            if (httpClientType is null)
            {
                return;
            }

            var testMethodAttributes = TestContextHelper.GetKnownTestMethodAttributes(compilationContext.Compilation);
            var isTestTypeCache = new ConcurrentDictionary<INamedTypeSymbol, bool>(SymbolEqualityComparer.Default);

            compilationContext.RegisterSyntaxNodeAction(
                context => AnalyzeObjectCreation(context, httpClientType, testMethodAttributes, isTestTypeCache),
                SyntaxKind.ObjectCreationExpression);
        });
    }

    private static void AnalyzeObjectCreation(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol httpClientType,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        var objectCreation = (ObjectCreationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(objectCreation, context.CancellationToken).Symbol is not IMethodSymbol constructor
            || !SymbolEqualityComparer.Default.Equals(constructor.ContainingType, httpClientType))
        {
            return;
        }

        var containingSymbol = context.SemanticModel.GetEnclosingSymbol(objectCreation.SpanStart, context.CancellationToken);
        if (containingSymbol is not null
            && !testMethodAttributes.IsDefaultOrEmpty
            && TestContextHelper.IsWithinTestContext(containingSymbol, testMethodAttributes, isTestTypeCache))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, objectCreation.GetLocation()));
    }
}
