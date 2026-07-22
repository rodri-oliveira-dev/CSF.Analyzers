using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;

using Microsoft.CodeAnalysis.CSharp;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.CodeAnalysis.Diagnostics;

using Microsoft.CodeAnalysis.Operations;


using Swa.Analyzers.Common.Common;

using Swa.Analyzers.Common;
using Swa.Analyzers.Architecture;

namespace Swa.Analyzers.Architecture.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch015ProhibitVerbsInHttpRoutesAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Design";
    private const string DefaultLanguage = "en-US";
    private const string PortugueseBrazilLanguage = "pt-BR";
    private const string RouteLanguageOption = "dotnet_diagnostic.ARCH015.route_language";
    private const string AdditionalVerbsOption = "dotnet_diagnostic.ARCH015.additional_verbs";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.ProhibitVerbsInHttpRoutes,
        title: "Prohibit verbs in HTTP routes",
        messageFormat: "Route segment '{0}' contains the verb '{1}' for language '{2}'. Prefer naming the resource with a noun instead of an action.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "HTTP route paths should describe resources. This rule detects conservative command-like verbs in literal route segments for MVC/Web API attributes and Minimal APIs.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.ProhibitVerbsInHttpRoutes));

    private static readonly ImmutableHashSet<string> KnownMvcRouteAttributeTypeNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "RouteAttribute",
        "HttpGetAttribute",
        "HttpPostAttribute",
        "HttpPutAttribute",
        "HttpPatchAttribute",
        "HttpDeleteAttribute",
        "HttpHeadAttribute",
        "HttpOptionsAttribute");

    private static readonly ImmutableHashSet<string> KnownMinimalApiMethodNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "MapGet",
        "MapPost",
        "MapPut",
        "MapPatch",
        "MapDelete",
        "MapMethods");

    private static readonly ImmutableHashSet<string> PortugueseBrazilVerbs = ImmutableHashSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "criar",
        "atualizar",
        "alterar",
        "excluir",
        "deletar",
        "remover",
        "buscar",
        "obter",
        "consultar",
        "listar",
        "emitir",
        "cancelar",
        "aprovar",
        "reprovar",
        "validar",
        "processar",
        "recalcular",
        "gerar",
        "enviar",
        "reenviar",
        "importar",
        "exportar");

    private static readonly ImmutableHashSet<string> EnglishUnitedStatesVerbs = ImmutableHashSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "create",
        "update",
        "change",
        "delete",
        "remove",
        "get",
        "fetch",
        "find",
        "search",
        "list",
        "issue",
        "cancel",
        "approve",
        "reject",
        "validate",
        "process",
        "recalculate",
        "generate",
        "send",
        "resend",
        "import",
        "export");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var optionsCache = new RouteRuleOptionsCache(compilationContext.Options.AnalyzerConfigOptionsProvider);

            compilationContext.RegisterSyntaxNodeAction(
                context => AnalyzeAttribute(context, optionsCache),
                SyntaxKind.Attribute);

            compilationContext.RegisterOperationAction(
                context => AnalyzeInvocation(context, optionsCache),
                OperationKind.Invocation);
        });
    }

    private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context, RouteRuleOptionsCache optionsCache)
    {
        var attribute = (AttributeSyntax)context.Node;

        if (!IsKnownRouteAttribute(attribute, context.SemanticModel, context.CancellationToken))
        {
            return;
        }

        if (attribute.ArgumentList?.Arguments.Count is not > 0)
        {
            return;
        }

        foreach (var argument in attribute.ArgumentList.Arguments)
        {
            if (argument.NameEquals is not null || argument.NameColon is not null)
            {
                continue;
            }

            if (!TryGetStringConstant(argument.Expression, context.SemanticModel, context.CancellationToken, out var route))
            {
                continue;
            }

            AnalyzeRoute(context, route, argument.Expression.GetLocation(), optionsCache);
            return;
        }
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, RouteRuleOptionsCache optionsCache)
    {
        var invocation = (IInvocationOperation)context.Operation;

        if (!IsKnownMinimalApiInvocation(invocation))
        {
            return;
        }

        foreach (var argument in invocation.Arguments)
        {
            if (argument.Value is null || !TryGetStringConstant(argument.Value, out var route))
            {
                continue;
            }

            AnalyzeRoute(context, route, argument.Value.Syntax.GetLocation(), optionsCache);
            return;
        }
    }

    private static void AnalyzeRoute(
        SyntaxNodeAnalysisContext context,
        string route,
        Location location,
        RouteRuleOptionsCache optionsCache)
    {
        var options = optionsCache.Get(context.Node.SyntaxTree);
        AnalyzeRoute(context.ReportDiagnostic, route, location, options);
    }

    private static void AnalyzeRoute(
        OperationAnalysisContext context,
        string route,
        Location location,
        RouteRuleOptionsCache optionsCache)
    {
        var options = optionsCache.Get(context.Operation.Syntax.SyntaxTree);
        AnalyzeRoute(context.ReportDiagnostic, route, location, options);
    }

    private static void AnalyzeRoute(Action<Diagnostic> reportDiagnostic, string route, Location location, RouteRuleOptions options)
    {
        foreach (var segment in GetRouteSegments(route))
        {
            if (TryFindVerb(segment, options.Verbs, out var verb))
            {
                reportDiagnostic(Diagnostic.Create(Rule, location, segment, verb, options.Language));
                return;
            }
        }
    }

    private static IEnumerable<string> GetRouteSegments(string route)
    {
        var queryStringIndex = route.IndexOf('?');
        var path = queryStringIndex >= 0 ? route.Substring(0, queryStringIndex) : route;

        foreach (var rawSegment in path.Split('/'))
        {
            var segment = rawSegment.Trim();

            if (segment.Length == 0)
            {
                continue;
            }

            if (ShouldIgnoreSegment(segment))
            {
                continue;
            }

            yield return segment;
        }
    }

    private static bool ShouldIgnoreSegment(string segment)
    {
        if (segment.IndexOf('{') >= 0 || segment.IndexOf('}') >= 0)
        {
            return true;
        }

        if (segment.IndexOf('[') >= 0 || segment.IndexOf(']') >= 0)
        {
            return true;
        }

        if (IsVersionSegment(segment))
        {
            return true;
        }

        return false;
    }

    private static bool IsVersionSegment(string segment)
    {
        if (segment.Length < 2 || (segment[0] != 'v' && segment[0] != 'V'))
        {
            return false;
        }

        for (var i = 1; i < segment.Length; i++)
        {
            if (!char.IsDigit(segment[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryFindVerb(string segment, ImmutableHashSet<string> verbs, out string verb)
    {
        foreach (var word in SplitSegmentWords(segment))
        {
            if (verbs.Contains(word))
            {
                verb = word;
                return true;
            }
        }

        verb = string.Empty;
        return false;
    }

    private static IEnumerable<string> SplitSegmentWords(string segment)
    {
        var builder = new StringBuilder();

        for (var i = 0; i < segment.Length; i++)
        {
            var current = segment[i];

            if (current is '-' or '_' or '.')
            {
                yield return builder.ToString();
                builder.Clear();
                continue;
            }

            if (char.IsUpper(current) && builder.Length > 0)
            {
                yield return builder.ToString();
                builder.Clear();
            }

            builder.Append(char.ToLowerInvariant(current));
        }

        yield return builder.ToString();
    }

    private static bool IsKnownRouteAttribute(
        AttributeSyntax attribute,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        var symbol = semanticModel.GetSymbolInfo(attribute, cancellationToken).Symbol as IMethodSymbol;
        var attributeType = symbol?.ContainingType;

        while (attributeType is not null)
        {
            if (IsKnownAspNetCoreMvcRouteAttribute(attributeType))
            {
                return true;
            }

            attributeType = attributeType.BaseType;
        }

        return false;
    }

    private static bool IsKnownAspNetCoreMvcRouteAttribute(INamedTypeSymbol attributeType)
    {
        return KnownMvcRouteAttributeTypeNames.Contains(attributeType.MetadataName)
            && string.Equals(attributeType.ContainingNamespace?.ToDisplayString(), "Microsoft.AspNetCore.Mvc", StringComparison.Ordinal);
    }

    private static bool IsKnownMinimalApiInvocation(IInvocationOperation invocation)
    {
        var targetMethod = invocation.TargetMethod;

        if (!KnownMinimalApiMethodNames.Contains(targetMethod.Name))
        {
            return false;
        }

        var originalDefinition = targetMethod.ReducedFrom ?? targetMethod;

        if (!IsKnownAspNetCoreBuilderNamespace(originalDefinition.ContainingNamespace))
        {
            return false;
        }

        var receiverType = invocation.Instance?.Type ?? GetExtensionReceiverType(originalDefinition);

        return IsEndpointRouteBuilderCompatible(receiverType);
    }

    private static ITypeSymbol? GetExtensionReceiverType(IMethodSymbol method)
    {
        return method.IsExtensionMethod && method.Parameters.Length > 0
            ? method.Parameters[0].Type
            : null;
    }

    private static bool IsKnownAspNetCoreBuilderNamespace(INamespaceSymbol? namespaceSymbol)
    {
        return string.Equals(namespaceSymbol?.ToDisplayString(), "Microsoft.AspNetCore.Builder", StringComparison.Ordinal);
    }

    private static bool IsEndpointRouteBuilderCompatible(ITypeSymbol? type)
    {
        if (type is null)
        {
            return false;
        }

        if (IsEndpointRouteBuilder(type))
        {
            return true;
        }

        foreach (var interfaceType in type.AllInterfaces)
        {
            if (IsEndpointRouteBuilder(interfaceType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsEndpointRouteBuilder(ITypeSymbol type)
    {
        return string.Equals(type.Name, "IEndpointRouteBuilder", StringComparison.Ordinal)
            && (string.Equals(type.ContainingNamespace?.ToDisplayString(), "Microsoft.AspNetCore.Routing", StringComparison.Ordinal)
                || string.Equals(type.ContainingNamespace?.ToDisplayString(), "Microsoft.AspNetCore.Builder", StringComparison.Ordinal));
    }

    private static bool TryGetStringConstant(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken,
        out string value)
    {
        var constant = semanticModel.GetConstantValue(expression, cancellationToken);

        if (constant.HasValue && constant.Value is string stringValue)
        {
            value = stringValue;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryGetStringConstant(IOperation operation, out string value)
    {
        operation = Unwrap(operation);

        if (operation.ConstantValue.HasValue && operation.ConstantValue.Value is string stringValue)
        {
            value = stringValue;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static IOperation Unwrap(IOperation operation)
    {
        IOperation? current = operation;

        while (current is not null)
        {
            switch (current)
            {
                case IConversionOperation conversion:
                    current = conversion.Operand;
                    continue;

                case IParenthesizedOperation parenthesized:
                    current = parenthesized.Operand;
                    continue;
            }

            break;
        }

        return current ?? operation;
    }

    private sealed class RouteRuleOptionsCache
    {
        private readonly AnalyzerConfigOptionsProvider _provider;
        private readonly ConcurrentDictionary<SyntaxTree, RouteRuleOptions> _optionsBySyntaxTree = new();

        public RouteRuleOptionsCache(AnalyzerConfigOptionsProvider provider)
        {
            _provider = provider;
        }

        public RouteRuleOptions Get(SyntaxTree syntaxTree)
        {
            return _optionsBySyntaxTree.GetOrAdd(syntaxTree, CreateOptions);
        }

        private RouteRuleOptions CreateOptions(SyntaxTree syntaxTree)
        {
            return RouteRuleOptions.Create(_provider, syntaxTree);
        }
    }

    private readonly struct RouteRuleOptions
    {
        private RouteRuleOptions(string language, ImmutableHashSet<string> verbs)
        {
            Language = language;
            Verbs = verbs;
        }

        public string Language
        {
            get;
        }

        public ImmutableHashSet<string> Verbs
        {
            get;
        }

        public static RouteRuleOptions Create(AnalyzerConfigOptionsProvider provider, SyntaxTree syntaxTree)
        {
            var options = provider.GetOptions(syntaxTree);
            var language = DefaultLanguage;

            if (options.TryGetValue(RouteLanguageOption, out var configuredLanguage))
            {
                configuredLanguage = configuredLanguage.Trim();

                if (string.Equals(configuredLanguage, PortugueseBrazilLanguage, StringComparison.OrdinalIgnoreCase))
                {
                    language = PortugueseBrazilLanguage;
                }
                else if (string.Equals(configuredLanguage, DefaultLanguage, StringComparison.OrdinalIgnoreCase))
                {
                    language = DefaultLanguage;
                }
            }

            var verbs = GetNativeVerbs(language).ToBuilder();

            if (options.TryGetValue(AdditionalVerbsOption, out var additionalVerbs)
                && JsonStringArrayOptionParser.TryParse(additionalVerbs, out var parsedVerbs))
            {
                foreach (var verb in parsedVerbs)
                {
                    var normalized = verb.Trim();

                    if (normalized.Length > 0)
                    {
                        verbs.Add(normalized);
                    }
                }
            }

            return new RouteRuleOptions(language, verbs.ToImmutable());
        }

        private static ImmutableHashSet<string> GetNativeVerbs(string language)
        {
            return string.Equals(language, PortugueseBrazilLanguage, StringComparison.Ordinal)
                ? PortugueseBrazilVerbs
                : EnglishUnitedStatesVerbs;
        }

    }
}
