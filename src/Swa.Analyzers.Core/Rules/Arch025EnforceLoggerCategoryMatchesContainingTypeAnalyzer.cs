using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch025EnforceLoggerCategoryMatchesContainingTypeAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Observability";
    private const string GenericLoggerType = "Microsoft.Extensions.Logging.ILogger`1";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.EnforceLoggerCategoryMatchesContainingType,
        title: "ILogger<T> deve usar o tipo da classe atual",
        messageFormat: "Use ILogger<{1}> instead of ILogger<{0}> in this class",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "ILogger<T> should use the containing class as the logging category so log events are attributed to the component that emits them.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.EnforceLoggerCategoryMatchesContainingType));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var loggerType = compilationContext.Compilation.GetTypeByMetadataName(GenericLoggerType);
            if (loggerType is null)
            {
                return;
            }

            var testMethodAttributes = TestContextHelper.GetKnownTestMethodAttributes(compilationContext.Compilation);
            var isTestTypeCache = new ConcurrentDictionary<INamedTypeSymbol, bool>(SymbolEqualityComparer.Default);

            compilationContext.RegisterSyntaxNodeAction(
                context => AnalyzeField(context, loggerType, testMethodAttributes, isTestTypeCache),
                SyntaxKind.FieldDeclaration);

            compilationContext.RegisterSyntaxNodeAction(
                context => AnalyzeProperty(context, loggerType, testMethodAttributes, isTestTypeCache),
                SyntaxKind.PropertyDeclaration);

            compilationContext.RegisterSyntaxNodeAction(
                context => AnalyzeConstructor(context, loggerType, testMethodAttributes, isTestTypeCache),
                SyntaxKind.ConstructorDeclaration);
        });
    }

    private static void AnalyzeField(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol loggerType,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        var field = (FieldDeclarationSyntax)context.Node;
        if (field.Declaration.Variables.Count == 0)
        {
            return;
        }

        var symbol = context.SemanticModel.GetDeclaredSymbol(field.Declaration.Variables[0], context.CancellationToken);
        if (symbol is not IFieldSymbol fieldSymbol)
        {
            return;
        }

        AnalyzeType(
            context,
            field.Declaration.Type,
            fieldSymbol.Type,
            fieldSymbol.ContainingType,
            loggerType,
            testMethodAttributes,
            isTestTypeCache);
    }

    private static void AnalyzeProperty(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol loggerType,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        var property = (PropertyDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(property, context.CancellationToken) is not IPropertySymbol propertySymbol)
        {
            return;
        }

        AnalyzeType(
            context,
            property.Type,
            propertySymbol.Type,
            propertySymbol.ContainingType,
            loggerType,
            testMethodAttributes,
            isTestTypeCache);
    }

    private static void AnalyzeConstructor(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol loggerType,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        var constructor = (ConstructorDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(constructor, context.CancellationToken) is not IMethodSymbol constructorSymbol)
        {
            return;
        }

        if (ShouldIgnoreTestContext(constructorSymbol, testMethodAttributes, isTestTypeCache))
        {
            return;
        }

        foreach (var parameter in constructor.ParameterList.Parameters)
        {
            if (parameter.Type is null
                || context.SemanticModel.GetDeclaredSymbol(parameter, context.CancellationToken) is not IParameterSymbol parameterSymbol)
            {
                continue;
            }

            AnalyzeType(
                context,
                parameter.Type,
                parameterSymbol.Type,
                constructorSymbol.ContainingType,
                loggerType,
                testMethodAttributes,
                isTestTypeCache);
        }
    }

    private static void AnalyzeType(
        SyntaxNodeAnalysisContext context,
        TypeSyntax syntax,
        ITypeSymbol type,
        INamedTypeSymbol containingType,
        INamedTypeSymbol loggerType,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        if (containingType.TypeKind != TypeKind.Class
            || ShouldIgnoreTestContext(containingType, testMethodAttributes, isTestTypeCache)
            || !TryGetLoggerCategory(type, loggerType, out var loggerCategory)
            || SymbolEqualityComparer.Default.Equals(loggerCategory, containingType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            syntax.GetLocation(),
            FormatType(loggerCategory),
            FormatType(containingType)));
    }

    private static bool TryGetLoggerCategory(
        ITypeSymbol type,
        INamedTypeSymbol loggerType,
        out ITypeSymbol loggerCategory)
    {
        if (type is INamedTypeSymbol namedType
            && SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, loggerType)
            && namedType.TypeArguments.Length == 1)
        {
            loggerCategory = namedType.TypeArguments[0];
            return true;
        }

        loggerCategory = null!;
        return false;
    }

    private static bool ShouldIgnoreTestContext(
        ISymbol symbol,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        return !testMethodAttributes.IsDefaultOrEmpty
            && TestContextHelper.IsWithinTestContext(symbol, testMethodAttributes, isTestTypeCache);
    }

    private static string FormatType(ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
    }
}
