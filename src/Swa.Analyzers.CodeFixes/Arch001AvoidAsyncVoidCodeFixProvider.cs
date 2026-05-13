using System.Collections.Immutable;
using System.Composition;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Swa.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Arch001AvoidAsyncVoidCodeFixProvider))]
[Shared]
public sealed class Arch001AvoidAsyncVoidCodeFixProvider : CodeFixProvider
{
    private const string DiagnosticId = "ARCH001";
    private const string Title = "Use async Task";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticId);

    public override FixAllProvider? GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics[0];
        var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
        var declaration = await GetFixableDeclarationAsync(
                context.Document,
                node,
                context.CancellationToken)
            .ConfigureAwait(false);
        if (declaration is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                cancellationToken => UseAsyncTaskAsync(context.Document, declaration, cancellationToken),
                equivalenceKey: Title),
            diagnostic);
    }

    private static async Task<SyntaxNode?> GetFixableDeclarationAsync(
        Document document,
        SyntaxNode node,
        CancellationToken cancellationToken)
    {
        var method = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (method is not null &&
            CanReplaceReturnType(method.Modifiers, method.ReturnType) &&
            await IsSafeMethodDeclarationAsync(document, method, cancellationToken).ConfigureAwait(false))
        {
            return method;
        }

        var localFunction = node.FirstAncestorOrSelf<LocalFunctionStatementSyntax>();
        if (localFunction is not null && CanReplaceReturnType(localFunction.Modifiers, localFunction.ReturnType))
        {
            return localFunction;
        }

        return null;
    }

    private static async Task<bool> IsSafeMethodDeclarationAsync(
        Document document,
        MethodDeclarationSyntax method,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var methodSymbol = semanticModel?.GetDeclaredSymbol(method, cancellationToken);
        if (methodSymbol is null)
        {
            return false;
        }

        if (methodSymbol.IsOverride || methodSymbol.ExplicitInterfaceImplementations.Length > 0)
        {
            return false;
        }

        return !ImplementsInterfaceMember(methodSymbol);
    }

    private static bool ImplementsInterfaceMember(IMethodSymbol methodSymbol)
    {
        foreach (var interfaceType in methodSymbol.ContainingType.AllInterfaces)
        {
            foreach (var interfaceMember in interfaceType.GetMembers(methodSymbol.Name))
            {
                var implementation = methodSymbol.ContainingType.FindImplementationForInterfaceMember(interfaceMember);
                if (SymbolEqualityComparer.Default.Equals(implementation, methodSymbol))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool CanReplaceReturnType(SyntaxTokenList modifiers, TypeSyntax returnType) =>
        modifiers.Any(SyntaxKind.AsyncKeyword) &&
        returnType is PredefinedTypeSyntax predefinedType &&
        predefinedType.Keyword.IsKind(SyntaxKind.VoidKeyword);

    private static async Task<Document> UseAsyncTaskAsync(
        Document document,
        SyntaxNode declaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var newDeclaration = declaration switch
        {
            MethodDeclarationSyntax method => method.WithReturnType(CreateTaskReturnType(method.ReturnType)),
            LocalFunctionStatementSyntax localFunction => localFunction.WithReturnType(CreateTaskReturnType(localFunction.ReturnType)),
            _ => declaration,
        };

        var newRoot = root.ReplaceNode(declaration, newDeclaration);
        newRoot = AddSystemThreadingTasksUsingIfMissing(newRoot);

        var newDocument = document.WithSyntaxRoot(newRoot);
        return await Formatter
            .FormatAsync(newDocument, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private static TypeSyntax CreateTaskReturnType(TypeSyntax previousReturnType) =>
        SyntaxFactory.IdentifierName("Task")
            .WithLeadingTrivia(previousReturnType.GetLeadingTrivia())
            .WithTrailingTrivia(previousReturnType.GetTrailingTrivia())
            .WithAdditionalAnnotations(Formatter.Annotation);

    private static SyntaxNode AddSystemThreadingTasksUsingIfMissing(SyntaxNode root)
    {
        if (root is not CompilationUnitSyntax compilationUnit)
        {
            return root;
        }

        if (compilationUnit.Usings.Any(IsSystemThreadingTasksUsing))
        {
            return root;
        }

        var usingDirective = SyntaxFactory.UsingDirective(
                SyntaxFactory.ParseName("System.Threading.Tasks"))
            .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
            .WithAdditionalAnnotations(Formatter.Annotation);

        return compilationUnit.AddUsings(usingDirective);
    }

    private static bool IsSystemThreadingTasksUsing(UsingDirectiveSyntax usingDirective) =>
        usingDirective.StaticKeyword == default &&
        usingDirective.Alias is null &&
        usingDirective.Name?.ToString() == "System.Threading.Tasks";
}
