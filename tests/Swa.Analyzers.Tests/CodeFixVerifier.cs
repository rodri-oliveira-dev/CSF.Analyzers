using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Swa.Analyzers.Tests;

internal static class CodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    public static async Task VerifyCodeFixAsync(string source, string fixedSource)
    {
        var (workspace, document, diagnostics) = await CreateDocumentAndDiagnosticsAsync(source).ConfigureAwait(false);
        using (workspace)
        {
            Assert.NotEmpty(diagnostics);

            var action = await GetCodeActionAsync(document, diagnostics[0]).ConfigureAwait(false);
            Assert.NotNull(action);

            var operations = await action.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
            var applyChanges = Assert.IsType<ApplyChangesOperation>(Assert.Single(operations));
            var fixedDocument = applyChanges.ChangedSolution.GetDocument(document.Id);

            Assert.NotNull(fixedDocument);
            var fixedText = await fixedDocument.GetTextAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(Normalize(fixedSource), Normalize(fixedText.ToString()));
        }
    }

    public static async Task VerifyNoCodeFixAsync(string source)
    {
        var (workspace, document, diagnostics) = await CreateDocumentAndDiagnosticsAsync(source).ConfigureAwait(false);
        using (workspace)
        {
            Assert.NotEmpty(diagnostics);

            var action = await GetCodeActionAsync(document, diagnostics[0]).ConfigureAwait(false);

            Assert.Null(action);
        }
    }

    private static async Task<(AdhocWorkspace Workspace, Document Document, ImmutableArray<Diagnostic> Diagnostics)> CreateDocumentAndDiagnosticsAsync(
        string source)
    {
        var workspace = new AdhocWorkspace();
        var project = workspace
            .CurrentSolution
            .AddProject("CodeFixTests", "CodeFixTests", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithParseOptions(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp13))
            .AddMetadataReferences(GetMetadataReferences());

        var document = project.AddDocument("Test0.cs", SourceText.From(source));
        var compilation = await document.Project.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false);
        Assert.NotNull(compilation);

        var analyzerDiagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new TAnalyzer()))
            .GetAnalyzerDiagnosticsAsync(CancellationToken.None)
            .ConfigureAwait(false);

        return (workspace, document, analyzerDiagnostics);
    }

    private static async Task<CodeAction?> GetCodeActionAsync(Document document, Diagnostic diagnostic)
    {
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(
            document,
            diagnostic,
            (action, _) => actions.Add(action),
            CancellationToken.None);

        await new TCodeFix().RegisterCodeFixesAsync(context).ConfigureAwait(false);

        return actions.Count == 0 ? null : Assert.Single(actions);
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        var trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        Assert.False(string.IsNullOrWhiteSpace(trustedPlatformAssemblies));

        return trustedPlatformAssemblies
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
    }

    private static string Normalize(string source) =>
        source.Replace("\r\n", "\n", StringComparison.Ordinal);
}
