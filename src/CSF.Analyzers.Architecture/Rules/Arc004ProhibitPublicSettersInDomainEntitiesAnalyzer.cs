using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using Microsoft.CodeAnalysis.CSharp;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.CodeAnalysis.Diagnostics;

using CSF.Analyzers.Common;
using CSF.Analyzers.Architecture;

namespace CSF.Analyzers.Architecture.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arc004ProhibitPublicSettersInDomainEntitiesAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Design";
    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.ProhibitPublicSettersInDomainEntities,
        title: "Proibir setters publicos em entidades de dominio",
        messageFormat: "Domain entity property '{0}' exposes a public setter. Prefer private set and behavior methods to preserve invariants.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "Domain entities should protect invariants by avoiding publicly mutable state.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.ProhibitPublicSettersInDomainEntities));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var optionsCache = new DomainEntityClassifier.DomainEntityOptionsCache(compilationContext.Options.AnalyzerConfigOptionsProvider);

            compilationContext.RegisterSyntaxNodeAction(
                context => AnalyzeProperty(context, optionsCache),
                SyntaxKind.PropertyDeclaration);
        });
    }

    private static void AnalyzeProperty(
        SyntaxNodeAnalysisContext context,
        DomainEntityClassifier.DomainEntityOptionsCache optionsCache)
    {
        var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;

        if (propertyDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
        {
            return;
        }

        if (!TryGetSetAccessor(propertyDeclaration, out var setAccessor)
            || setAccessor.IsKind(SyntaxKind.InitAccessorDeclaration))
        {
            return;
        }

        var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclaration, context.CancellationToken);
        if (propertySymbol?.ContainingType is not INamedTypeSymbol containingType)
        {
            return;
        }

        if (containingType.TypeKind != TypeKind.Class || containingType.IsRecord)
        {
            return;
        }

        if (DomainEntityClassifier.IsTestType(containingType, propertyDeclaration.SyntaxTree.FilePath))
        {
            return;
        }

        var options = optionsCache.Get(propertyDeclaration.SyntaxTree);
        if (!DomainEntityClassifier.IsDomainEntity(containingType, options))
        {
            return;
        }

        if (!ShouldReportSetter(propertySymbol.SetMethod, options))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            setAccessor.Keyword.GetLocation(),
            propertySymbol.Name));
    }

    private static bool TryGetSetAccessor(
        PropertyDeclarationSyntax propertyDeclaration,
        out AccessorDeclarationSyntax accessor)
    {
        foreach (var candidate in propertyDeclaration.AccessorList?.Accessors ?? default)
        {
            if (candidate.IsKind(SyntaxKind.SetAccessorDeclaration)
                || candidate.IsKind(SyntaxKind.InitAccessorDeclaration))
            {
                accessor = candidate;
                return true;
            }
        }

        accessor = null!;
        return false;
    }

    private static bool ShouldReportSetter(IMethodSymbol? setMethod, DomainEntityClassifier.DomainEntityOptions options)
    {
        if (setMethod is null)
        {
            return false;
        }

        if (setMethod.DeclaredAccessibility == Accessibility.Public)
        {
            return true;
        }

        if (options.AllowInternalSetters)
        {
            return false;
        }

        return setMethod.DeclaredAccessibility is Accessibility.Internal or Accessibility.ProtectedOrInternal;
    }

}
