using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch013RestrictMockingFrameworksToNSubstituteAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "TestQuality";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.RestrictMockingFrameworksToNSubstitute,
        title: "Restrict mocking frameworks to NSubstitute",
        messageFormat: "Mocking framework '{0}' is not allowed by policy. Use NSubstitute instead.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "To keep tests consistent and reduce maintenance cost, teams often standardize on a single mocking framework. This rule reports usages of known alternative mocking frameworks when the policy standard is NSubstitute.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.RestrictMockingFrameworksToNSubstitute));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var testMethodAttributes = TestContextHelper.GetKnownTestMethodAttributes(compilationContext.Compilation);
            if (testMethodAttributes.IsDefaultOrEmpty)
            {
                // Evita ruído fora de projetos de teste.
                return;
            }

            var presentFrameworks = GetPresentDisallowedFrameworks(compilationContext.Compilation);
            if (presentFrameworks.IsDefaultOrEmpty)
            {
                // Saída rápida quando nenhum framework de mock proibido conhecido é referenciado.
                return;
            }

            var frameworksByRootNamespace = presentFrameworks
                .ToImmutableDictionary(static x => x.RootNamespace, static x => x.Name, StringComparer.Ordinal);

            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeInvocation(operationContext, frameworksByRootNamespace),
                OperationKind.Invocation);

            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeObjectCreation(operationContext, frameworksByRootNamespace),
                OperationKind.ObjectCreation);

            compilationContext.RegisterSymbolAction(
                symbolContext => AnalyzeField(symbolContext, frameworksByRootNamespace),
                SymbolKind.Field);

            compilationContext.RegisterSymbolAction(
                symbolContext => AnalyzeProperty(symbolContext, frameworksByRootNamespace),
                SymbolKind.Property);

            compilationContext.RegisterSymbolAction(
                symbolContext => AnalyzeMethod(symbolContext, frameworksByRootNamespace),
                SymbolKind.Method);

            compilationContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeVariableDeclaration(syntaxContext, frameworksByRootNamespace),
                SyntaxKind.VariableDeclaration);

            compilationContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeUsingDirective(syntaxContext, frameworksByRootNamespace),
                SyntaxKind.UsingDirective);
        });
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        ImmutableDictionary<string, string> frameworksByRootNamespace)
    {
        if (IsDeclaredInsideDisallowedFramework(context.ContainingSymbol, frameworksByRootNamespace))
        {
            // Evita reportar dentro do próprio framework, relevante para testes com stubs no código fonte.
            return;
        }

        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        if (method.ContainingType is null)
        {
            return;
        }

        if (!TryGetDisallowedFrameworkName(method.ContainingType.ContainingNamespace, frameworksByRootNamespace, out var frameworkName))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, GetInvocationMemberNameLocation(invocation.Syntax), frameworkName));
    }

    private static void AnalyzeObjectCreation(
        OperationAnalysisContext context,
        ImmutableDictionary<string, string> frameworksByRootNamespace)
    {
        if (IsDeclaredInsideDisallowedFramework(context.ContainingSymbol, frameworksByRootNamespace))
        {
            return;
        }

        var creation = (IObjectCreationOperation)context.Operation;
        var createdType = creation.Type;

        if (createdType is null)
        {
            return;
        }

        if (!TryGetDisallowedFrameworkName(createdType.ContainingNamespace, frameworksByRootNamespace, out var frameworkName))
        {
            return;
        }

        if (creation.Syntax is ImplicitObjectCreationExpressionSyntax && ShouldSkipImplicitNewDiagnostic(creation.Syntax))
        {
            // Evita diagnóstico duplicado quando o tipo já está explícito no ponto de declaração
            // (ex.: `Moq.Mock<IFoo> mock = new();`). Nesses casos, o tipo da declaração já é reportado.
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, GetObjectCreationTypeLocation(creation.Syntax), frameworkName));
    }

    private static void AnalyzeField(SymbolAnalysisContext context, ImmutableDictionary<string, string> frameworksByRootNamespace)
    {
        var field = (IFieldSymbol)context.Symbol;

        if (IsDeclaredInsideDisallowedFramework(field, frameworksByRootNamespace))
        {
            return;
        }

        if (!TryGetDisallowedFrameworkName(field.Type, frameworksByRootNamespace, out var frameworkName))
        {
            return;
        }

        var location = GetFieldTypeLocation(field);
        if (location is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, location, frameworkName));
        }
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context, ImmutableDictionary<string, string> frameworksByRootNamespace)
    {
        var property = (IPropertySymbol)context.Symbol;

        if (IsDeclaredInsideDisallowedFramework(property, frameworksByRootNamespace))
        {
            return;
        }

        if (!TryGetDisallowedFrameworkName(property.Type, frameworksByRootNamespace, out var frameworkName))
        {
            return;
        }

        var location = GetPropertyTypeLocation(property);
        if (location is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, location, frameworkName));
        }
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, ImmutableDictionary<string, string> frameworksByRootNamespace)
    {
        var method = (IMethodSymbol)context.Symbol;

        if (IsDeclaredInsideDisallowedFramework(method, frameworksByRootNamespace))
        {
            return;
        }

        if (method.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet)
        {
            // Tipos de propriedade são tratados por AnalyzeProperty.
            return;
        }

        if (method.MethodKind is MethodKind.EventAdd or MethodKind.EventRemove)
        {
            // Accessors de evento não interessam para esta regra.
            return;
        }

        // Tipo de retorno
        if (TryGetDisallowedFrameworkName(method.ReturnType, frameworksByRootNamespace, out var returnFrameworkName))
        {
            var location = GetReturnTypeLocation(method);
            if (location is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, location, returnFrameworkName));
            }
        }

        // Parâmetros
        foreach (var parameter in method.Parameters)
        {
            if (!TryGetDisallowedFrameworkName(parameter.Type, frameworksByRootNamespace, out var frameworkName))
            {
                continue;
            }

            var location = GetParameterTypeLocation(parameter);
            if (location is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, location, frameworkName));
            }
        }
    }

    private static void AnalyzeVariableDeclaration(
        SyntaxNodeAnalysisContext context,
        ImmutableDictionary<string, string> frameworksByRootNamespace)
    {
        if (IsDeclaredInsideDisallowedFramework(context.ContainingSymbol, frameworksByRootNamespace))
        {
            return;
        }

        var declaration = (VariableDeclarationSyntax)context.Node;

        // Apenas declarações de variáveis locais; campos são tratados pela análise de símbolos
        if (declaration.Parent is not (LocalDeclarationStatementSyntax or ForStatementSyntax))
        {
            return;
        }

        // Ignora declarações `var` para evitar ruído quando o tipo é inferido
        if (declaration.Type is IdentifierNameSyntax identifierName && identifierName.Identifier.Text == "var")
        {
            return;
        }

        var typeSymbol = context.SemanticModel
            .GetSymbolInfo(declaration.Type, context.CancellationToken)
            .Symbol as ITypeSymbol;

        if (typeSymbol is null)
        {
            return;
        }

        if (!TryGetDisallowedFrameworkName(typeSymbol, frameworksByRootNamespace, out var frameworkName))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, declaration.Type.GetLocation(), frameworkName));
    }

    private static void AnalyzeUsingDirective(
        SyntaxNodeAnalysisContext context,
        ImmutableDictionary<string, string> frameworksByRootNamespace)
    {
        if (IsDeclaredInsideDisallowedFramework(context.ContainingSymbol, frameworksByRootNamespace))
        {
            return;
        }

        var usingDirective = (UsingDirectiveSyntax)context.Node;

        if (usingDirective.Name is null)
        {
            return;
        }

        // `using X = ...;` ainda usa Name para representar o namespace/tipo importado.
        var symbol = context.SemanticModel.GetSymbolInfo(usingDirective.Name, context.CancellationToken).Symbol;
        if (symbol is null)
        {
            return;
        }

        string? frameworkName = null;

        switch (symbol)
        {
            case INamespaceSymbol ns:
                if (!TryGetDisallowedFrameworkName(ns, frameworksByRootNamespace, out var nsFramework))
                {
                    return;
                }

                frameworkName = nsFramework;
                break;

            case ITypeSymbol type:
                if (!TryGetDisallowedFrameworkName(type, frameworksByRootNamespace, out var typeFramework))
                {
                    return;
                }

                frameworkName = typeFramework;
                break;

            default:
                return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            GetUsingDirectiveRootNameLocation(usingDirective.Name),
            frameworkName));
    }

    private static bool TryGetDisallowedFrameworkName(
        ITypeSymbol? type,
        ImmutableDictionary<string, string> frameworksByRootNamespace,
        out string frameworkName)
    {
        frameworkName = null!;

        if (type is null)
        {
            return false;
        }

        if (TryGetDisallowedFrameworkName(type.ContainingNamespace, frameworksByRootNamespace, out frameworkName))
        {
            return true;
        }

        // Arrays (ex.: Mock<T>[] ou List<Mock<T>>[])
        if (type is IArrayTypeSymbol arrayType)
        {
            return TryGetDisallowedFrameworkName(arrayType.ElementType, frameworksByRootNamespace, out frameworkName);
        }

        if (type is INamedTypeSymbol compositeType)
        {
            if (compositeType.IsTupleType)
            {
                foreach (var tupleElement in compositeType.TupleElements)
                {
                    if (TryGetDisallowedFrameworkName(tupleElement.Type, frameworksByRootNamespace, out frameworkName))
                    {
                        return true;
                    }
                }
            }

            foreach (var typeArgument in compositeType.TypeArguments)
            {
                if (TryGetDisallowedFrameworkName(typeArgument, frameworksByRootNamespace, out frameworkName))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryGetDisallowedFrameworkName(
        INamespaceSymbol? @namespace,
        ImmutableDictionary<string, string> frameworksByRootNamespace,
        out string frameworkName)
    {
        frameworkName = null!;

        for (var current = @namespace; current is not null && !current.IsGlobalNamespace; current = current.ContainingNamespace)
        {
            if (!current.ContainingNamespace.IsGlobalNamespace)
            {
                continue;
            }

            if (frameworksByRootNamespace.TryGetValue(current.Name, out var name) && name is not null)
            {
                frameworkName = name;
                return true;
            }

            return false;
        }

        return false;
    }

    private static bool IsDeclaredInsideDisallowedFramework(ISymbol? symbol, ImmutableDictionary<string, string> frameworksByRootNamespace)
    {
        if (symbol is null)
        {
            return false;
        }

        return TryGetDisallowedFrameworkName(symbol.ContainingNamespace, frameworksByRootNamespace, out _);
    }

    private static Location? GetFieldTypeLocation(IFieldSymbol field)
    {
        if (field.DeclaringSyntaxReferences.IsEmpty)
        {
            return null;
        }

        foreach (var syntaxRef in field.DeclaringSyntaxReferences)
        {
            var syntax = syntaxRef.GetSyntax();
            if (syntax is VariableDeclaratorSyntax declarator && declarator.Parent is VariableDeclarationSyntax declaration)
            {
                return declaration.Type.GetLocation();
            }

            if (syntax is FieldDeclarationSyntax fieldDecl)
            {
                return fieldDecl.Declaration.Type.GetLocation();
            }
        }

        return null;
    }

    private static Location? GetPropertyTypeLocation(IPropertySymbol property)
    {
        if (property.DeclaringSyntaxReferences.IsEmpty)
        {
            return null;
        }

        foreach (var syntaxRef in property.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is PropertyDeclarationSyntax propDecl)
            {
                return propDecl.Type.GetLocation();
            }
        }

        return null;
    }

    private static Location? GetReturnTypeLocation(IMethodSymbol method)
    {
        if (method.DeclaringSyntaxReferences.IsEmpty)
        {
            return null;
        }

        foreach (var syntaxRef in method.DeclaringSyntaxReferences)
        {
            var syntax = syntaxRef.GetSyntax();
            if (syntax is MethodDeclarationSyntax methodDecl)
            {
                return methodDecl.ReturnType.GetLocation();
            }

            if (syntax is LocalFunctionStatementSyntax localFunction)
            {
                return localFunction.ReturnType.GetLocation();
            }
        }

        return null;
    }

    private static Location? GetParameterTypeLocation(IParameterSymbol parameter)
    {
        if (parameter.DeclaringSyntaxReferences.IsEmpty)
        {
            return null;
        }

        foreach (var syntaxRef in parameter.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is ParameterSyntax paramSyntax)
            {
                return paramSyntax.Type?.GetLocation();
            }
        }

        return null;
    }

    private static ImmutableArray<DisallowedMockFramework> GetPresentDisallowedFrameworks(Compilation compilation)
    {
        var builder = ImmutableArray.CreateBuilder<DisallowedMockFramework>();

        foreach (var framework in DisallowedMockFramework.All)
        {
            if (framework.IsPresent(compilation))
            {
                builder.Add(framework);
            }
        }

        return builder.ToImmutable();
    }

    private static Location GetInvocationMemberNameLocation(SyntaxNode syntax)
    {
        return syntax switch
        {
            InvocationExpressionSyntax invocation => invocation.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name switch
                {
                    IdentifierNameSyntax identifierName => identifierName.Identifier.GetLocation(),
                    GenericNameSyntax genericName => genericName.Identifier.GetLocation(),
                    _ => memberAccess.Name.GetLocation(),
                },
                MemberBindingExpressionSyntax memberBinding => memberBinding.Name switch
                {
                    IdentifierNameSyntax identifierName => identifierName.Identifier.GetLocation(),
                    GenericNameSyntax genericName => genericName.Identifier.GetLocation(),
                    _ => memberBinding.Name.GetLocation(),
                },
                IdentifierNameSyntax identifierName => identifierName.Identifier.GetLocation(),
                GenericNameSyntax genericName => genericName.Identifier.GetLocation(),
                _ => invocation.GetLocation(),
            },
            _ => syntax.GetLocation(),
        };
    }

    private static bool ShouldSkipImplicitNewDiagnostic(SyntaxNode implicitObjectCreationSyntax)
    {
        // Só queremos ignorar quando há declaração explícita de tipo perto do ponto de `new()`,
        // porque os analyzers de símbolo/análise de declaração de variável já reportam o mesmo framework.

        // Declaração de variável local/campo: `SomeType x = new();`
        if (implicitObjectCreationSyntax.Parent is EqualsValueClauseSyntax equalsValue
            && equalsValue.Parent is VariableDeclaratorSyntax declarator
            && declarator.Parent is VariableDeclarationSyntax variableDeclaration)
        {
            return !IsVarTypeSyntax(variableDeclaration.Type);
        }

        // Inicializador de propriedade: `SomeType X { get; } = new();`
        if (implicitObjectCreationSyntax.Parent is EqualsValueClauseSyntax propertyEqualsValue
            && propertyEqualsValue.Parent is PropertyDeclarationSyntax propertyDeclaration)
        {
            return !IsVarTypeSyntax(propertyDeclaration.Type);
        }

        // Valor padrão de parâmetro: `SomeType x = new()`
        if (implicitObjectCreationSyntax.Parent is EqualsValueClauseSyntax parameterEqualsValue
            && parameterEqualsValue.Parent is ParameterSyntax parameterSyntax
            && parameterSyntax.Type is not null)
        {
            return !IsVarTypeSyntax(parameterSyntax.Type);
        }

        return false;
    }

    private static bool IsVarTypeSyntax(TypeSyntax typeSyntax)
    {
        return typeSyntax is IdentifierNameSyntax identifierName
            && string.Equals(identifierName.Identifier.Text, "var", StringComparison.Ordinal);
    }

    private static Location GetObjectCreationTypeLocation(SyntaxNode syntax)
    {
        // Para criação regular de objeto, reporta no tipo explícito.
        // Para `new()` com tipo alvo (criação implícita), usa a palavra-chave `new` como fallback.
        return syntax switch
        {
            ObjectCreationExpressionSyntax creation => GetTypeSyntaxNameLocation(creation.Type),
            ImplicitObjectCreationExpressionSyntax implicitCreation => implicitCreation.NewKeyword.GetLocation(),
            _ => syntax.GetLocation(),
        };
    }

    private static Location GetTypeSyntaxNameLocation(TypeSyntax typeSyntax)
    {
        return typeSyntax switch
        {
            QualifiedNameSyntax qualified => GetTypeSyntaxNameLocation(qualified.Right),
            AliasQualifiedNameSyntax aliasQualified => GetTypeSyntaxNameLocation(aliasQualified.Name),
            IdentifierNameSyntax identifier => identifier.Identifier.GetLocation(),
            GenericNameSyntax generic => generic.Identifier.GetLocation(),
            _ => typeSyntax.GetLocation(),
        };
    }

    private static Location GetUsingDirectiveRootNameLocation(NameSyntax nameSyntax)
    {
        // Para `using Moq.Language.Flow;`, queremos destacar a raiz `Moq`.
        return nameSyntax switch
        {
            QualifiedNameSyntax qualified => GetUsingDirectiveRootNameLocation(qualified.Left),
            AliasQualifiedNameSyntax aliasQualified => aliasQualified.Alias.Identifier.GetLocation(),
            IdentifierNameSyntax identifier => identifier.Identifier.GetLocation(),
            GenericNameSyntax generic => generic.Identifier.GetLocation(),
            _ => nameSyntax.GetLocation(),
        };
    }

    private readonly struct DisallowedMockFramework
    {
        public DisallowedMockFramework(
            string name,
            string rootNamespace,
            ImmutableArray<string> knownAssemblyNames,
            ImmutableArray<string> knownTypeMetadataNames)
        {
            Name = name;
            RootNamespace = rootNamespace;
            KnownAssemblyNames = knownAssemblyNames;
            KnownTypeMetadataNames = knownTypeMetadataNames;
        }

        public string Name
        {
            get;
        }

        public string RootNamespace
        {
            get;
        }

        public ImmutableArray<string> KnownAssemblyNames
        {
            get;
        }

        public ImmutableArray<string> KnownTypeMetadataNames
        {
            get;
        }

        public static ImmutableArray<DisallowedMockFramework> All =>
            ImmutableArray.Create(
                new DisallowedMockFramework(
                    name: "Moq",
                    rootNamespace: "Moq",
                    knownAssemblyNames: ImmutableArray.Create("Moq"),
                    knownTypeMetadataNames: ImmutableArray.Create("Moq.Mock`1", "Moq.It", "Moq.ItExpr", "Moq.Mock")),
                new DisallowedMockFramework(
                    name: "FakeItEasy",
                    rootNamespace: "FakeItEasy",
                    knownAssemblyNames: ImmutableArray.Create("FakeItEasy"),
                    knownTypeMetadataNames: ImmutableArray.Create("FakeItEasy.A", "FakeItEasy.Fake", "FakeItEasy.CallTo")));

        public bool IsPresent(Compilation compilation)
        {
            foreach (var reference in compilation.ReferencedAssemblyNames)
            {
                foreach (var knownAssembly in KnownAssemblyNames)
                {
                    if (string.Equals(reference.Name, knownAssembly, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            foreach (var metadataName in KnownTypeMetadataNames)
            {
                if (compilation.GetTypeByMetadataName(metadataName) is not null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
