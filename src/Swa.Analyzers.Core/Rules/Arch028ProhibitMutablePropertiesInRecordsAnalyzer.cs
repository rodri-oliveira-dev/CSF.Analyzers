using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch028ProhibitMutablePropertiesInRecordsAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Design";
    private const string AllowNonPublicSettersOption = "dotnet_diagnostic.ARCH028.allow_non_public_setters";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.ProhibitMutablePropertiesInRecords,
        title: "Proibir propriedades mutaveis em records",
        messageFormat: "Record property '{0}' has a mutable setter. Prefer init-only or immutable record state.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Records should model immutable state. Prefer init-only properties, primary constructors or read-only properties.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.ProhibitMutablePropertiesInRecords));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var optionsCache = new RecordMutabilityOptionsCache(compilationContext.Options.AnalyzerConfigOptionsProvider);

            compilationContext.RegisterSyntaxNodeAction(
                context => AnalyzeProperty(context, optionsCache),
                SyntaxKind.PropertyDeclaration);
        });
    }

    private static void AnalyzeProperty(
        SyntaxNodeAnalysisContext context,
        RecordMutabilityOptionsCache optionsCache)
    {
        var property = (PropertyDeclarationSyntax)context.Node;

        if (property.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault() is not RecordDeclarationSyntax)
        {
            return;
        }

        var setAccessor = property.AccessorList?.Accessors.FirstOrDefault(static accessor =>
            accessor.IsKind(SyntaxKind.SetAccessorDeclaration));

        if (setAccessor is null)
        {
            return;
        }

        if (optionsCache.Get(property.SyntaxTree).AllowNonPublicSetters
            && HasNonPublicModifier(setAccessor))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            property.Identifier.GetLocation(),
            property.Identifier.ValueText));
    }

    private static bool HasNonPublicModifier(AccessorDeclarationSyntax accessor)
    {
        foreach (var modifier in accessor.Modifiers)
        {
            if (modifier.IsKind(SyntaxKind.PrivateKeyword)
                || modifier.IsKind(SyntaxKind.ProtectedKeyword)
                || modifier.IsKind(SyntaxKind.InternalKeyword))
            {
                return true;
            }
        }

        return false;
    }

    private sealed class RecordMutabilityOptionsCache
    {
        private readonly AnalyzerConfigOptionsProvider _provider;
        private readonly ConcurrentDictionary<SyntaxTree, RecordMutabilityOptions> _optionsBySyntaxTree = new();

        public RecordMutabilityOptionsCache(AnalyzerConfigOptionsProvider provider)
        {
            _provider = provider;
        }

        public RecordMutabilityOptions Get(SyntaxTree syntaxTree)
        {
            return _optionsBySyntaxTree.GetOrAdd(syntaxTree, CreateOptions);
        }

        private RecordMutabilityOptions CreateOptions(SyntaxTree syntaxTree)
        {
            return RecordMutabilityOptions.Create(_provider, syntaxTree);
        }
    }

    private readonly struct RecordMutabilityOptions
    {
        private RecordMutabilityOptions(bool allowNonPublicSetters)
        {
            AllowNonPublicSetters = allowNonPublicSetters;
        }

        public bool AllowNonPublicSetters
        {
            get;
        }

        public static RecordMutabilityOptions Create(AnalyzerConfigOptionsProvider provider, SyntaxTree syntaxTree)
        {
            var options = provider.GetOptions(syntaxTree);
            var allowNonPublicSetters = true;

            if (options.TryGetValue(AllowNonPublicSettersOption, out var configuredValue)
                && bool.TryParse(configuredValue.Trim(), out var parsedValue))
            {
                allowNonPublicSetters = parsedValue;
            }

            return new RecordMutabilityOptions(allowNonPublicSetters);
        }
    }
}
