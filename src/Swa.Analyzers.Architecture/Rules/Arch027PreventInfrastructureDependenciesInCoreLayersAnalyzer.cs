using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using Microsoft.CodeAnalysis.CSharp;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.CodeAnalysis.Diagnostics;


using Swa.Analyzers.Common.Common;

using Swa.Analyzers.Common;
using Swa.Analyzers.Architecture;

namespace Swa.Analyzers.Architecture.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch027PreventInfrastructureDependenciesInCoreLayersAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Architecture";
    private const string CoreNamespacePatternsOption = "dotnet_diagnostic.ARCH027.core_namespace_patterns";
    private const string ForbiddenNamespacePatternsOption = "dotnet_diagnostic.ARCH027.forbidden_namespace_patterns";
    private const string AllowedNamespacePatternsOption = "dotnet_diagnostic.ARCH027.allowed_namespace_patterns";
    private const string IgnoreTestsOption = "dotnet_diagnostic.ARCH027.ignore_tests";
    private const int MaxConfiguredPatterns = 256;
    private const int MaxConfiguredPatternLength = 256;

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.PreventInfrastructureDependenciesInCoreLayers,
        title: "Evitar dependencias de infraestrutura em camadas de dominio/aplicacao",
        messageFormat: "Core namespace '{1}' should not depend directly on infrastructure namespace '{0}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Domain and application layers should depend on abstractions instead of infrastructure frameworks or adapters.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.PreventInfrastructureDependenciesInCoreLayers));

    private static readonly ImmutableArray<string> DefaultCoreNamespacePatterns = ImmutableArray.Create(
        "*.Domain",
        "*.Application");

    private static readonly ImmutableArray<string> DefaultForbiddenNamespacePatterns = ImmutableArray.Create(
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "StackExchange.Redis",
        "Npgsql");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var optionsCache = new InfrastructureDependencyOptionsCache(compilationContext.Options.AnalyzerConfigOptionsProvider);
            var testMethodAttributes = TestContextHelper.GetKnownTestMethodAttributes(compilationContext.Compilation);
            var isTestTypeCache = new ConcurrentDictionary<INamedTypeSymbol, bool>(SymbolEqualityComparer.Default);

            compilationContext.RegisterSyntaxNodeAction(
                context => AnalyzeUsingDirective(context, optionsCache),
                SyntaxKind.UsingDirective);

            compilationContext.RegisterSyntaxNodeAction(
                context => AnalyzeTypeName(context, testMethodAttributes, isTestTypeCache, optionsCache),
                SyntaxKind.IdentifierName,
                SyntaxKind.QualifiedName,
                SyntaxKind.AliasQualifiedName);
        });
    }

    private static void AnalyzeUsingDirective(
        SyntaxNodeAnalysisContext context,
        InfrastructureDependencyOptionsCache optionsCache)
    {
        var usingDirective = (UsingDirectiveSyntax)context.Node;

        if (usingDirective.Alias is not null || usingDirective.Name is null)
        {
            return;
        }

        var options = optionsCache.Get(usingDirective.SyntaxTree);
        var coreNamespace = GetContainingNamespaceName(usingDirective);

        if (!ShouldAnalyzeContext(coreNamespace, usingDirective.SyntaxTree, options))
        {
            return;
        }

        var dependencyNamespace = usingDirective.Name.ToString();

        if (!options.IsForbiddenNamespace(dependencyNamespace))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            usingDirective.Name.GetLocation(),
            dependencyNamespace,
            coreNamespace));
    }

    private static void AnalyzeTypeName(
        SyntaxNodeAnalysisContext context,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache,
        InfrastructureDependencyOptionsCache optionsCache)
    {
        var name = (NameSyntax)context.Node;

        if (name.Parent is QualifiedNameSyntax)
        {
            return;
        }

        if (name.Parent is AliasQualifiedNameSyntax)
        {
            return;
        }

        if (name.Parent is UsingDirectiveSyntax)
        {
            return;
        }

        var options = optionsCache.Get(name.SyntaxTree);
        var containingSymbol = context.SemanticModel.GetEnclosingSymbol(name.SpanStart, context.CancellationToken);
        var coreNamespace = containingSymbol?.ContainingNamespace?.ToDisplayString() ?? GetContainingNamespaceName(name);

        if (!ShouldAnalyzeContext(coreNamespace, name.SyntaxTree, options))
        {
            return;
        }

        if (options.IgnoreTests
            && !testMethodAttributes.IsDefaultOrEmpty
            && containingSymbol is not null
            && TestContextHelper.IsWithinTestContext(containingSymbol, testMethodAttributes, isTestTypeCache))
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(name, context.CancellationToken).Symbol is not INamedTypeSymbol type)
        {
            return;
        }

        var dependencyNamespace = type.ContainingNamespace?.ToDisplayString();

        if (string.IsNullOrWhiteSpace(dependencyNamespace)
            || !options.IsForbiddenNamespace(dependencyNamespace!))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            name.GetLocation(),
            dependencyNamespace,
            coreNamespace));
    }

    private static bool ShouldAnalyzeContext(string? namespaceName, SyntaxTree syntaxTree, InfrastructureDependencyOptions options)
    {
        if (string.IsNullOrWhiteSpace(namespaceName))
        {
            return false;
        }

        if (options.IgnoreTests && IsTestPathOrNamespace(syntaxTree.FilePath, namespaceName!))
        {
            return false;
        }

        return options.IsCoreNamespace(namespaceName!);
    }

    private static string? GetContainingNamespaceName(SyntaxNode node)
    {
        for (SyntaxNode? current = node.Parent; current is not null; current = current.Parent)
        {
            if (current is BaseNamespaceDeclarationSyntax namespaceDeclaration)
            {
                return namespaceDeclaration.Name.ToString();
            }
        }

        var root = node.SyntaxTree.GetRoot();
        var firstNamespace = root.DescendantNodes(static node => node is not BaseNamespaceDeclarationSyntax)
            .OfType<BaseNamespaceDeclarationSyntax>()
            .FirstOrDefault();

        return firstNamespace?.Name.ToString();
    }

    private static bool IsTestPathOrNamespace(string filePath, string namespaceName)
    {
        if (namespaceName.EndsWith(".Tests", StringComparison.Ordinal)
            || namespaceName.IndexOf(".Tests.", StringComparison.Ordinal) >= 0
            || namespaceName.EndsWith(".Test", StringComparison.Ordinal)
            || namespaceName.IndexOf(".Test.", StringComparison.Ordinal) >= 0)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        return filePath.IndexOf("/tests/", StringComparison.OrdinalIgnoreCase) >= 0
            || filePath.IndexOf("\\tests\\", StringComparison.OrdinalIgnoreCase) >= 0
            || filePath.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class InfrastructureDependencyOptionsCache
    {
        private readonly AnalyzerConfigOptionsProvider _provider;
        private readonly ConcurrentDictionary<SyntaxTree, InfrastructureDependencyOptions> _optionsBySyntaxTree = new();

        public InfrastructureDependencyOptionsCache(AnalyzerConfigOptionsProvider provider)
        {
            _provider = provider;
        }

        public InfrastructureDependencyOptions Get(SyntaxTree syntaxTree)
        {
            return _optionsBySyntaxTree.GetOrAdd(syntaxTree, CreateOptions);
        }

        private InfrastructureDependencyOptions CreateOptions(SyntaxTree syntaxTree)
        {
            return InfrastructureDependencyOptions.Create(_provider, syntaxTree);
        }
    }

    private readonly struct InfrastructureDependencyOptions
    {
        private InfrastructureDependencyOptions(
            ImmutableArray<string> coreNamespacePatterns,
            ImmutableArray<string> forbiddenNamespacePatterns,
            ImmutableArray<string> allowedNamespacePatterns,
            bool ignoreTests)
        {
            CoreNamespacePatterns = coreNamespacePatterns;
            ForbiddenNamespacePatterns = forbiddenNamespacePatterns;
            AllowedNamespacePatterns = allowedNamespacePatterns;
            IgnoreTests = ignoreTests;
        }

        private ImmutableArray<string> CoreNamespacePatterns
        {
            get;
        }

        private ImmutableArray<string> ForbiddenNamespacePatterns
        {
            get;
        }

        private ImmutableArray<string> AllowedNamespacePatterns
        {
            get;
        }

        public bool IgnoreTests
        {
            get;
        }

        public bool IsCoreNamespace(string namespaceName)
        {
            return MatchesAnyPattern(namespaceName, CoreNamespacePatterns);
        }

        public bool IsForbiddenNamespace(string namespaceName)
        {
            return MatchesAnyPattern(namespaceName, ForbiddenNamespacePatterns)
                && !MatchesAnyPattern(namespaceName, AllowedNamespacePatterns);
        }

        public static InfrastructureDependencyOptions Create(AnalyzerConfigOptionsProvider provider, SyntaxTree syntaxTree)
        {
            var options = provider.GetOptions(syntaxTree);

            var coreNamespacePatterns = ReadPatternList(options, CoreNamespacePatternsOption, DefaultCoreNamespacePatterns);
            var forbiddenNamespacePatterns = ReadPatternList(options, ForbiddenNamespacePatternsOption, DefaultForbiddenNamespacePatterns);
            var allowedNamespacePatterns = ReadPatternList(options, AllowedNamespacePatternsOption, ImmutableArray<string>.Empty);

            return new InfrastructureDependencyOptions(
                coreNamespacePatterns,
                forbiddenNamespacePatterns,
                allowedNamespacePatterns,
                AnalyzerConfigOptionReader.ReadBooleanOption(options, IgnoreTestsOption, defaultValue: true));
        }

        private static ImmutableArray<string> ReadPatternList(
            AnalyzerConfigOptions options,
            string optionName,
            ImmutableArray<string> defaultValue)
        {
            if (!options.TryGetValue(optionName, out var configuredValue))
            {
                return defaultValue;
            }

            var builder = ImmutableArray.CreateBuilder<string>();
            var seenPatterns = new HashSet<string>(StringComparer.Ordinal);

            configuredValue = configuredValue.Trim().Trim('"');

            foreach (var rawPattern in configuredValue.Split(';'))
            {
                var pattern = rawPattern.Trim();

                if (pattern.Length > 0
                    && pattern.Length <= MaxConfiguredPatternLength
                    && seenPatterns.Add(pattern))
                {
                    builder.Add(pattern);

                    if (builder.Count == MaxConfiguredPatterns)
                    {
                        break;
                    }
                }
            }

            return builder.ToImmutable();
        }

        private static bool MatchesAnyPattern(string namespaceName, ImmutableArray<string> patterns)
        {
            foreach (var pattern in patterns)
            {
                if (MatchesPattern(namespaceName, pattern))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesPattern(string namespaceName, string pattern)
        {
            if (pattern.StartsWith("*.", StringComparison.Ordinal))
            {
                return namespaceName.EndsWith(pattern.Substring(1), StringComparison.Ordinal);
            }

            if (pattern.IndexOf('*') >= 0)
            {
                return MatchesWildcard(namespaceName, pattern);
            }

            return string.Equals(namespaceName, pattern, StringComparison.Ordinal)
                || namespaceName.StartsWith(pattern + ".", StringComparison.Ordinal);
        }

        private static bool MatchesWildcard(string value, string pattern)
        {
            return WildcardPatternMatcher.Matches(value, pattern, StringComparison.Ordinal);
        }
    }
}
