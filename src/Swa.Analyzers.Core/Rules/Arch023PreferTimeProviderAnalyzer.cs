using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch023PreferTimeProviderAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Testability";
    private const string AllowedNamespacesOption = "dotnet_diagnostic.ARCH023.allowed_namespaces";
    private const string AllowedTypesOption = "dotnet_diagnostic.ARCH023.allowed_types";
    private const string IgnoreSimpleLoggingOption = "dotnet_diagnostic.ARCH023.ignore_simple_logging";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.PreferTimeProvider,
        title: "Preferir TimeProvider para obter data e hora",
        messageFormat: "Prefer TimeProvider instead of direct system clock access '{0}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Direct access to DateTime.Now, DateTime.UtcNow, DateTimeOffset.Now, or DateTimeOffset.UtcNow couples domain and application code to the system clock, making tests less deterministic.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.PreferTimeProvider));

    private static readonly ImmutableHashSet<string> ClockPropertyNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "Now",
        "UtcNow");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var dateTimeType = compilationContext.Compilation.GetTypeByMetadataName("System.DateTime");
            var dateTimeOffsetType = compilationContext.Compilation.GetTypeByMetadataName("System.DateTimeOffset");
            var timeProviderType = compilationContext.Compilation.GetTypeByMetadataName("System.TimeProvider");

            if (dateTimeType is null && dateTimeOffsetType is null)
            {
                return;
            }

            var testMethodAttributes = TestContextHelper.GetKnownTestMethodAttributes(compilationContext.Compilation);
            var isTestTypeCache = new ConcurrentDictionary<INamedTypeSymbol, bool>(SymbolEqualityComparer.Default);
            var optionsCache = new TimeProviderRuleOptionsCache(compilationContext.Options.AnalyzerConfigOptionsProvider);

            compilationContext.RegisterSyntaxNodeAction(
                context => AnalyzeMemberAccess(
                    context,
                    dateTimeType,
                    dateTimeOffsetType,
                    timeProviderType,
                    testMethodAttributes,
                    isTestTypeCache,
                    optionsCache),
                SyntaxKind.SimpleMemberAccessExpression);
        });
    }

    private static void AnalyzeMemberAccess(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol? dateTimeType,
        INamedTypeSymbol? dateTimeOffsetType,
        INamedTypeSymbol? timeProviderType,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache,
        TimeProviderRuleOptionsCache optionsCache)
    {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;

        if (!ClockPropertyNames.Contains(memberAccess.Name.Identifier.ValueText))
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken).Symbol is not IPropertySymbol property)
        {
            return;
        }

        if (!property.IsStatic || !IsSystemClockProperty(property, dateTimeType, dateTimeOffsetType))
        {
            return;
        }

        if (IsProgramFile(memberAccess.SyntaxTree))
        {
            return;
        }

        var containingSymbol = context.SemanticModel.GetEnclosingSymbol(memberAccess.SpanStart, context.CancellationToken);
        if (containingSymbol is null)
        {
            return;
        }

        if (!testMethodAttributes.IsDefaultOrEmpty
            && TestContextHelper.IsWithinTestContext(containingSymbol, testMethodAttributes, isTestTypeCache))
        {
            return;
        }

        var options = optionsCache.Get(memberAccess.SyntaxTree);
        if (IsAllowedContext(containingSymbol, timeProviderType, options))
        {
            return;
        }

        if (options.IgnoreSimpleLogging
            && IsInsideSimpleLoggingInvocation(memberAccess, context.SemanticModel, context.CancellationToken))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            memberAccess.Name.GetLocation(),
            property.ContainingType.Name + "." + property.Name));
    }

    private static bool IsSystemClockProperty(
        IPropertySymbol property,
        INamedTypeSymbol? dateTimeType,
        INamedTypeSymbol? dateTimeOffsetType)
    {
        return (dateTimeType is not null && SymbolEqualityComparer.Default.Equals(property.ContainingType, dateTimeType))
            || (dateTimeOffsetType is not null && SymbolEqualityComparer.Default.Equals(property.ContainingType, dateTimeOffsetType));
    }

    private static bool IsProgramFile(SyntaxTree syntaxTree)
    {
        var fileName = syntaxTree.FilePath;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var lastSeparator = Math.Max(fileName.LastIndexOf('/'), fileName.LastIndexOf('\\'));
        var shortFileName = lastSeparator >= 0 ? fileName.Substring(lastSeparator + 1) : fileName;
        return string.Equals(shortFileName, "Program.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAllowedContext(
        ISymbol containingSymbol,
        INamedTypeSymbol? timeProviderType,
        TimeProviderRuleOptions options)
    {
        for (ISymbol? current = containingSymbol; current is not null; current = current.ContainingSymbol)
        {
            if (current is INamedTypeSymbol type)
            {
                if (IsClockImplementation(type, timeProviderType)
                    || options.IsAllowedType(type.Name)
                    || options.IsAllowedNamespace(type.ContainingNamespace?.ToDisplayString()))
                {
                    return true;
                }
            }

            if (current is INamespaceSymbol namespaceSymbol
                && options.IsAllowedNamespace(namespaceSymbol.ToDisplayString()))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsClockImplementation(INamedTypeSymbol type, INamedTypeSymbol? timeProviderType)
    {
        if (type.TypeKind == TypeKind.Interface)
        {
            return false;
        }

        if (type.Name.EndsWith("Clock", StringComparison.Ordinal)
            || type.Name.EndsWith("TimeProvider", StringComparison.Ordinal))
        {
            return true;
        }

        if (timeProviderType is null)
        {
            return false;
        }

        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, timeProviderType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInsideSimpleLoggingInvocation(
        MemberAccessExpressionSyntax memberAccess,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        var argument = memberAccess.FirstAncestorOrSelf<ArgumentSyntax>();
        if (argument?.Parent?.Parent is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method)
        {
            return false;
        }

        return method.Name.StartsWith("Log", StringComparison.Ordinal)
            && invocation.ArgumentList.Arguments.Count > 1;
    }

    private sealed class TimeProviderRuleOptionsCache
    {
        private readonly AnalyzerConfigOptionsProvider _provider;
        private readonly ConcurrentDictionary<SyntaxTree, TimeProviderRuleOptions> _optionsBySyntaxTree = new();

        public TimeProviderRuleOptionsCache(AnalyzerConfigOptionsProvider provider)
        {
            _provider = provider;
        }

        public TimeProviderRuleOptions Get(SyntaxTree syntaxTree)
        {
            return _optionsBySyntaxTree.GetOrAdd(syntaxTree, CreateOptions);
        }

        private TimeProviderRuleOptions CreateOptions(SyntaxTree syntaxTree)
        {
            return TimeProviderRuleOptions.Create(_provider, syntaxTree);
        }
    }

    private readonly struct TimeProviderRuleOptions
    {
        private TimeProviderRuleOptions(
            ImmutableArray<string> allowedNamespaces,
            ImmutableHashSet<string> allowedTypes,
            bool ignoreSimpleLogging)
        {
            AllowedNamespaces = allowedNamespaces;
            AllowedTypes = allowedTypes;
            IgnoreSimpleLogging = ignoreSimpleLogging;
        }

        private ImmutableArray<string> AllowedNamespaces
        {
            get;
        }

        private ImmutableHashSet<string> AllowedTypes
        {
            get;
        }

        public bool IgnoreSimpleLogging
        {
            get;
        }

        public bool IsAllowedNamespace(string? namespaceName)
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                return false;
            }

            foreach (var allowedNamespace in AllowedNamespaces)
            {
                if (string.Equals(namespaceName, allowedNamespace, StringComparison.Ordinal)
                    || namespaceName!.StartsWith(allowedNamespace + ".", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsAllowedType(string typeName)
        {
            return AllowedTypes.Contains(typeName);
        }

        public static TimeProviderRuleOptions Create(AnalyzerConfigOptionsProvider provider, SyntaxTree syntaxTree)
        {
            var options = provider.GetOptions(syntaxTree);

            return new TimeProviderRuleOptions(
                ReadStringArray(options, AllowedNamespacesOption, static value => value.Trim()).ToImmutableArray(),
                ReadStringArray(options, AllowedTypesOption, static value => value.Trim()).ToImmutableHashSet(StringComparer.Ordinal),
                ReadBoolean(options, IgnoreSimpleLoggingOption));
        }

        private static bool ReadBoolean(AnalyzerConfigOptions options, string optionName)
        {
            if (!options.TryGetValue(optionName, out var configuredValue))
            {
                return false;
            }

            return bool.TryParse(configuredValue, out var value) && value;
        }

        private static IEnumerable<string> ReadStringArray(
            AnalyzerConfigOptions options,
            string optionName,
            Func<string, string> normalize)
        {
            if (!options.TryGetValue(optionName, out var configuredValue)
                || !TryParseJsonStringArray(configuredValue, out var parsedValues))
            {
                yield break;
            }

            foreach (var parsedValue in parsedValues)
            {
                var normalized = normalize(parsedValue);

                if (normalized.Length > 0)
                {
                    yield return normalized;
                }
            }
        }

        private static bool TryParseJsonStringArray(string value, out ImmutableArray<string> items)
        {
            var parser = new JsonStringArrayParser(value);
            return parser.TryParse(out items);
        }
    }

    private struct JsonStringArrayParser
    {
        private readonly string _value;
        private int _position;

        public JsonStringArrayParser(string value)
        {
            _value = value;
            _position = 0;
        }

        public bool TryParse(out ImmutableArray<string> items)
        {
            var builder = ImmutableArray.CreateBuilder<string>();

            SkipWhitespace();

            if (!TryRead('['))
            {
                items = ImmutableArray<string>.Empty;
                return false;
            }

            SkipWhitespace();

            if (TryRead(']'))
            {
                SkipWhitespace();
                if (_position != _value.Length)
                {
                    items = ImmutableArray<string>.Empty;
                    return false;
                }

                items = builder.ToImmutable();
                return true;
            }

            while (true)
            {
                SkipWhitespace();

                if (!TryReadString(out var item))
                {
                    items = ImmutableArray<string>.Empty;
                    return false;
                }

                builder.Add(item);
                SkipWhitespace();

                if (TryRead(']'))
                {
                    SkipWhitespace();
                    if (_position != _value.Length)
                    {
                        items = ImmutableArray<string>.Empty;
                        return false;
                    }

                    items = builder.ToImmutable();
                    return true;
                }

                if (!TryRead(','))
                {
                    items = ImmutableArray<string>.Empty;
                    return false;
                }
            }
        }

        private bool TryReadString(out string value)
        {
            var builder = new StringBuilder();

            if (!TryRead('"'))
            {
                value = string.Empty;
                return false;
            }

            while (_position < _value.Length)
            {
                var current = _value[_position++];

                if (current == '"')
                {
                    value = builder.ToString();
                    return true;
                }

                if (current == '\\')
                {
                    if (_position >= _value.Length)
                    {
                        value = string.Empty;
                        return false;
                    }

                    var escaped = _value[_position++];

                    switch (escaped)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            builder.Append(escaped);
                            break;
                        case 'b':
                            builder.Append('\b');
                            break;
                        case 'f':
                            builder.Append('\f');
                            break;
                        case 'n':
                            builder.Append('\n');
                            break;
                        case 'r':
                            builder.Append('\r');
                            break;
                        case 't':
                            builder.Append('\t');
                            break;
                        default:
                            value = string.Empty;
                            return false;
                    }

                    continue;
                }

                builder.Append(current);
            }

            value = string.Empty;
            return false;
        }

        private bool TryRead(char expected)
        {
            if (_position >= _value.Length || _value[_position] != expected)
            {
                return false;
            }

            _position++;
            return true;
        }

        private void SkipWhitespace()
        {
            while (_position < _value.Length && char.IsWhiteSpace(_value[_position]))
            {
                _position++;
            }
        }
    }
}
