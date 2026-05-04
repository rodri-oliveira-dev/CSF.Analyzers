using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Swa.Analyzers.Core.Common;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch033AvoidBuildServiceProviderDuringServiceRegistrationAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Reliability";
    private const string IgnoreTestsOption = "dotnet_diagnostic.ARCH033.ignore_tests";
    private const string BuildServiceProviderMethodName = "BuildServiceProvider";
    private const string ServiceCollectionMetadataName = "Microsoft.Extensions.DependencyInjection.IServiceCollection";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.AvoidBuildServiceProviderDuringServiceRegistration,
        title: "Evitar BuildServiceProvider durante registro de servicos",
        messageFormat: "Avoid calling BuildServiceProvider() during service registration. Resolve services through DI instead of creating a parallel provider.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Calling BuildServiceProvider() while registering services can create parallel providers, duplicated singletons, invalid scopes, and inconsistent runtime behavior.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.AvoidBuildServiceProviderDuringServiceRegistration));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var serviceCollectionType = compilationContext.Compilation.GetTypeByMetadataName(ServiceCollectionMetadataName);
            if (serviceCollectionType is null)
            {
                return;
            }

            var optionsCache = new ServiceProviderOptionsCache(compilationContext.Options.AnalyzerConfigOptionsProvider);
            var testMethodAttributes = TestContextHelper.GetKnownTestMethodAttributes(compilationContext.Compilation);
            var isTestTypeCache = new ConcurrentDictionary<INamedTypeSymbol, bool>(SymbolEqualityComparer.Default);

            compilationContext.RegisterSyntaxNodeAction(
                context => AnalyzeInvocation(context, serviceCollectionType, optionsCache, testMethodAttributes, isTestTypeCache),
                SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol serviceCollectionType,
        ServiceProviderOptionsCache optionsCache,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess
            || !string.Equals(memberAccess.Name.Identifier.ValueText, BuildServiceProviderMethodName, StringComparison.Ordinal))
        {
            return;
        }

        var options = optionsCache.Get(invocation.SyntaxTree);
        if (options.IgnoreTests
            && context.ContainingSymbol is not null
            && !testMethodAttributes.IsDefaultOrEmpty
            && TestContextHelper.IsWithinTestContext(context.ContainingSymbol, testMethodAttributes, isTestTypeCache))
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method
            || !IsDependencyInjectionBuildServiceProvider(method, serviceCollectionType))
        {
            return;
        }

        var receiverType = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type;
        if (!ImplementsServiceCollection(receiverType, serviceCollectionType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, memberAccess.Name.GetLocation()));
    }

    private static bool IsDependencyInjectionBuildServiceProvider(
        IMethodSymbol method,
        INamedTypeSymbol serviceCollectionType)
    {
        if (!string.Equals(method.Name, BuildServiceProviderMethodName, StringComparison.Ordinal)
            || method.ReducedFrom is not { } extensionMethod
            || !extensionMethod.IsExtensionMethod
            || extensionMethod.Parameters.Length == 0)
        {
            return false;
        }

        return ImplementsServiceCollection(extensionMethod.Parameters[0].Type, serviceCollectionType);
    }

    private static bool ImplementsServiceCollection(ITypeSymbol? type, INamedTypeSymbol serviceCollectionType)
    {
        if (type is null)
        {
            return false;
        }

        if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, serviceCollectionType))
        {
            return true;
        }

        foreach (var candidate in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, serviceCollectionType))
            {
                return true;
            }
        }

        return false;
    }

    private sealed class ServiceProviderOptionsCache
    {
        private readonly AnalyzerConfigOptionsProvider _provider;
        private readonly ConcurrentDictionary<SyntaxTree, ServiceProviderOptions> _optionsBySyntaxTree = new();

        public ServiceProviderOptionsCache(AnalyzerConfigOptionsProvider provider)
        {
            _provider = provider;
        }

        public ServiceProviderOptions Get(SyntaxTree syntaxTree)
        {
            return _optionsBySyntaxTree.GetOrAdd(syntaxTree, CreateOptions);
        }

        private ServiceProviderOptions CreateOptions(SyntaxTree syntaxTree)
        {
            return ServiceProviderOptions.Create(_provider, syntaxTree);
        }
    }

    private readonly struct ServiceProviderOptions
    {
        private ServiceProviderOptions(bool ignoreTests)
        {
            IgnoreTests = ignoreTests;
        }

        public bool IgnoreTests
        {
            get;
        }

        public static ServiceProviderOptions Create(AnalyzerConfigOptionsProvider provider, SyntaxTree syntaxTree)
        {
            var options = provider.GetOptions(syntaxTree);
            return new ServiceProviderOptions(
                AnalyzerConfigOptionReader.ReadBooleanOption(options, IgnoreTestsOption, defaultValue: true));
        }
    }
}
