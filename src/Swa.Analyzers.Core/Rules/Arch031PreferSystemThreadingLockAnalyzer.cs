using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch031PreferSystemThreadingLockAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Performance";
    private const string MinimumTargetFrameworkOption = "dotnet_diagnostic.ARCH031.minimum_target_framework";
    private const string ReportLocalVariablesOption = "dotnet_diagnostic.ARCH031.report_local_variables";
    private const string TargetFrameworkBuildProperty = "build_property.TargetFramework";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.PreferSystemThreadingLock,
        title: "Preferir System.Threading.Lock para sincronizacao",
        messageFormat: "Prefer System.Threading.Lock instead of locking on object '{0}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Use System.Threading.Lock instead of object when a value is used only as a monitor for lock statements.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.PreferSystemThreadingLock));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var objectType = compilationContext.Compilation.GetSpecialType(SpecialType.System_Object);
            var optionsCache = new LockRuleOptionsCache(compilationContext.Options.AnalyzerConfigOptionsProvider);

            compilationContext.RegisterSyntaxNodeAction(
                context => AnalyzeLockStatement(context, objectType, optionsCache),
                SyntaxKind.LockStatement);
        });
    }

    private static void AnalyzeLockStatement(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol objectType,
        LockRuleOptionsCache optionsCache)
    {
        var lockStatement = (LockStatementSyntax)context.Node;
        var options = optionsCache.Get(lockStatement.SyntaxTree);

        if (!options.ShouldRunForTargetFramework)
        {
            return;
        }

        var typeInfo = context.SemanticModel.GetTypeInfo(lockStatement.Expression, context.CancellationToken);
        if (typeInfo.Type is null
            || !SymbolEqualityComparer.Default.Equals(typeInfo.Type, objectType))
        {
            return;
        }

        var isObjectCreation = IsObjectCreation(lockStatement.Expression);
        var symbol = context.SemanticModel.GetSymbolInfo(lockStatement.Expression, context.CancellationToken).Symbol;
        if (!options.ReportLocalVariables && symbol is ILocalSymbol)
        {
            return;
        }

        if (!isObjectCreation
            && symbol is not null
            && symbol.Kind != SymbolKind.Field
            && symbol.Kind != SymbolKind.Property
            && symbol.Kind != SymbolKind.Local)
        {
            return;
        }

        if (symbol is null && !isObjectCreation)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            lockStatement.Expression.GetLocation(),
            GetDisplayName(lockStatement.Expression, symbol, isObjectCreation)));
    }

    private static bool IsObjectCreation(ExpressionSyntax expression)
    {
        return expression.IsKind(SyntaxKind.ObjectCreationExpression)
            || expression.IsKind(SyntaxKind.ImplicitObjectCreationExpression);
    }

    private static string GetDisplayName(ExpressionSyntax expression, ISymbol? symbol, bool isObjectCreation)
    {
        if (!isObjectCreation && symbol is not null)
        {
            return symbol.Name;
        }

        return expression.ToString();
    }

    private sealed class LockRuleOptionsCache
    {
        private readonly AnalyzerConfigOptionsProvider _provider;
        private readonly ConcurrentDictionary<SyntaxTree, LockRuleOptions> _optionsBySyntaxTree = new();

        public LockRuleOptionsCache(AnalyzerConfigOptionsProvider provider)
        {
            _provider = provider;
        }

        public LockRuleOptions Get(SyntaxTree syntaxTree)
        {
            return _optionsBySyntaxTree.GetOrAdd(syntaxTree, CreateOptions);
        }

        private LockRuleOptions CreateOptions(SyntaxTree syntaxTree)
        {
            return LockRuleOptions.Create(_provider, syntaxTree);
        }
    }

    private readonly struct LockRuleOptions
    {
        private LockRuleOptions(bool shouldRunForTargetFramework, bool reportLocalVariables)
        {
            ShouldRunForTargetFramework = shouldRunForTargetFramework;
            ReportLocalVariables = reportLocalVariables;
        }

        public bool ShouldRunForTargetFramework
        {
            get;
        }

        public bool ReportLocalVariables
        {
            get;
        }

        public static LockRuleOptions Create(AnalyzerConfigOptionsProvider provider, SyntaxTree syntaxTree)
        {
            var treeOptions = provider.GetOptions(syntaxTree);
            var minimumTargetFramework = ReadMinimumTargetFramework(provider, treeOptions);
            var targetFramework = ReadTargetFramework(provider, treeOptions);

            return new LockRuleOptions(
                IsTargetFrameworkSupported(targetFramework, minimumTargetFramework),
                ReadBoolean(treeOptions, ReportLocalVariablesOption, defaultValue: true));
        }

        private static TargetFrameworkVersion ReadMinimumTargetFramework(
            AnalyzerConfigOptionsProvider provider,
            AnalyzerConfigOptions options)
        {
            if (options.TryGetValue(MinimumTargetFrameworkOption, out var configuredValue)
                && TargetFrameworkVersion.TryParse(configuredValue, out var parsedValue))
            {
                return parsedValue;
            }

            if (provider.GlobalOptions.TryGetValue(MinimumTargetFrameworkOption, out configuredValue)
                && TargetFrameworkVersion.TryParse(configuredValue, out parsedValue))
            {
                return parsedValue;
            }

            return new TargetFrameworkVersion(9, 0);
        }

        private static string? ReadTargetFramework(
            AnalyzerConfigOptionsProvider provider,
            AnalyzerConfigOptions treeOptions)
        {
            if (treeOptions.TryGetValue(TargetFrameworkBuildProperty, out var treeTargetFramework))
            {
                return treeTargetFramework;
            }

            return provider.GlobalOptions.TryGetValue(TargetFrameworkBuildProperty, out var globalTargetFramework)
                ? globalTargetFramework
                : null;
        }

        private static bool IsTargetFrameworkSupported(string? targetFramework, TargetFrameworkVersion minimumTargetFramework)
        {
            if (!TargetFrameworkVersion.TryParse(targetFramework, out var parsedTargetFramework))
            {
                return true;
            }

            return parsedTargetFramework.CompareTo(minimumTargetFramework) >= 0;
        }

        private static bool ReadBoolean(AnalyzerConfigOptions options, string optionName, bool defaultValue)
        {
            if (options.TryGetValue(optionName, out var configuredValue)
                && bool.TryParse(configuredValue.Trim(), out var parsedValue))
            {
                return parsedValue;
            }

            return defaultValue;
        }
    }

    private readonly struct TargetFrameworkVersion : IComparable<TargetFrameworkVersion>
    {
        public TargetFrameworkVersion(int major, int minor)
        {
            Major = major;
            Minor = minor;
        }

        private int Major
        {
            get;
        }

        private int Minor
        {
            get;
        }

        public int CompareTo(TargetFrameworkVersion other)
        {
            var majorComparison = Major.CompareTo(other.Major);
            return majorComparison != 0 ? majorComparison : Minor.CompareTo(other.Minor);
        }

        public static bool TryParse(string? value, out TargetFrameworkVersion version)
        {
            version = default;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var normalizedValue = value!.Trim();
            if (!normalizedValue.StartsWith("net", StringComparison.OrdinalIgnoreCase)
                || normalizedValue.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)
                || normalizedValue.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var versionText = normalizedValue.Substring(3);
            var dashIndex = versionText.IndexOf('-');
            if (dashIndex >= 0)
            {
                versionText = versionText.Substring(0, dashIndex);
            }

            var parts = versionText.Split('.');
            if (parts.Length == 0 || !int.TryParse(parts[0], out var major))
            {
                return false;
            }

            var minor = 0;
            if (parts.Length == 1 && versionText.Length > 1)
            {
                major = versionText[0] - '0';
                if (major < 0 || major > 9 || !int.TryParse(versionText.Substring(1), out minor))
                {
                    return false;
                }
            }
            else if (parts.Length > 1 && !int.TryParse(parts[1], out minor))
            {
                return false;
            }

            version = new TargetFrameworkVersion(major, minor);
            return true;
        }
    }
}
