using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch024AvoidInterpolatedStringsInLoggerAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Observability";
    private const string LoggerExtensionsType = "Microsoft.Extensions.Logging.LoggerExtensions";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.AvoidInterpolatedStringsInLogger,
        title: "Evitar interpolação ou concatenação em ILogger",
        messageFormat: "Use structured logging template arguments instead of interpolated or concatenated message in '{0}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "ILogger calls should use static message templates and separate arguments so log backends can preserve structured properties.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.AvoidInterpolatedStringsInLogger));

    private static readonly ImmutableHashSet<string> LogMethodNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "LogTrace",
        "LogDebug",
        "LogInformation",
        "LogWarning",
        "LogError",
        "LogCritical");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var loggerExtensionsType = compilationContext.Compilation.GetTypeByMetadataName(LoggerExtensionsType);
            if (loggerExtensionsType is null)
            {
                return;
            }

            compilationContext.RegisterSyntaxNodeAction(
                context => AnalyzeInvocation(context, loggerExtensionsType),
                SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol loggerExtensionsType)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol targetMethod)
        {
            return;
        }

        var originalMethod = targetMethod.ReducedFrom ?? targetMethod;
        if (!LogMethodNames.Contains(originalMethod.Name)
            || !SymbolEqualityComparer.Default.Equals(originalMethod.ContainingType, loggerExtensionsType))
        {
            return;
        }

        if (!TryGetMessageArgument(invocation, targetMethod, out var messageArgument))
        {
            return;
        }

        var messageExpression = UnwrapExpression(messageArgument.Expression);
        if (!IsNonStructuredMessage(messageExpression, context.SemanticModel, context.CancellationToken))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            messageExpression.GetLocation(),
            originalMethod.Name));
    }

    private static bool TryGetMessageArgument(
        InvocationExpressionSyntax invocation,
        IMethodSymbol targetMethod,
        out ArgumentSyntax messageArgument)
    {
        var arguments = invocation.ArgumentList.Arguments;

        for (var i = 0; i < arguments.Count; i++)
        {
            var argument = arguments[i];
            if (argument.NameColon is not null)
            {
                if (string.Equals(argument.NameColon.Name.Identifier.ValueText, "message", StringComparison.Ordinal))
                {
                    messageArgument = argument;
                    return true;
                }

                continue;
            }

            if (i < targetMethod.Parameters.Length
                && string.Equals(targetMethod.Parameters[i].Name, "message", StringComparison.Ordinal))
            {
                messageArgument = argument;
                return true;
            }
        }

        messageArgument = null!;
        return false;
    }

    private static bool IsNonStructuredMessage(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        if (expression is InterpolatedStringExpressionSyntax)
        {
            return true;
        }

        if (!IsStringConcatenation(expression, semanticModel, cancellationToken))
        {
            return false;
        }

        return !semanticModel.GetConstantValue(expression, cancellationToken).HasValue;
    }

    private static bool IsStringConcatenation(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        if (expression is not BinaryExpressionSyntax binary
            || !binary.IsKind(SyntaxKind.AddExpression))
        {
            return false;
        }

        return IsStringType(semanticModel.GetTypeInfo(binary, cancellationToken).Type);
    }

    private static bool IsStringType(ITypeSymbol? type)
    {
        return type?.SpecialType == SpecialType.System_String;
    }

    private static ExpressionSyntax UnwrapExpression(ExpressionSyntax expression)
    {
        var current = expression;

        while (current is ParenthesizedExpressionSyntax parenthesized)
        {
            current = parenthesized.Expression;
        }

        return current;
    }
}
