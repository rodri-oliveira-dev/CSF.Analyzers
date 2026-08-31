using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace CSF.Analyzers.Tests.FrameworkReferences;

internal static class RealFrameworkVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    private static readonly ReferenceAssemblies TargetReferenceAssemblies = new(
        "net10.0",
        new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.11"),
        Path.Combine("ref", "net10.0"));

    public static DiagnosticResult Diagnostic(string diagnosticId) =>
        CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);

    public static Task VerifyAnalyzerAsync(
        string source,
        IEnumerable<Assembly> frameworkAssemblies,
        params DiagnosticResult[] expected)
    {
        return VerifyAnalyzerAsync(source, frameworkAssemblies, Array.Empty<string>(), expected);
    }

    public static Task VerifyAnalyzerAsync(
        string source,
        IEnumerable<Assembly> frameworkAssemblies,
        IEnumerable<string> referenceAssemblyPaths,
        params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = TargetReferenceAssemblies,
        };

        // Estes testes complementam a suíte rápida baseada em stubs compilando contra metadados reais
        // de frameworks, incluindo extension methods, namespaces e overloads.
        foreach (var reference in CreateReferences(frameworkAssemblies).Concat(CreateReferences(referenceAssemblyPaths)))
        {
            test.TestState.AdditionalReferences.Add(reference);
        }

        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    public static IEnumerable<string> GetPackageReferenceAssemblyPaths(string packageId, string packageVersion, string targetFramework)
    {
        var userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
        if (string.IsNullOrWhiteSpace(userProfile))
        {
            userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        if (string.IsNullOrWhiteSpace(userProfile))
        {
            yield break;
        }

        var referenceRoot = Path.Combine(
            userProfile,
            ".nuget",
            "packages",
            packageId.ToLowerInvariant(),
            packageVersion,
            "ref",
            targetFramework);

        if (!Directory.Exists(referenceRoot))
        {
            throw new DirectoryNotFoundException($"Reference assemblies not found: {referenceRoot}");
        }

        foreach (var referenceAssemblyPath in Directory.EnumerateFiles(referenceRoot, "*.dll"))
        {
            yield return referenceAssemblyPath;
        }
    }

    private static IEnumerable<MetadataReference> CreateReferences(IEnumerable<string> referenceAssemblyPaths)
    {
        foreach (var referenceAssemblyPath in referenceAssemblyPaths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            yield return MetadataReference.CreateFromFile(referenceAssemblyPath);
        }
    }

    private static IEnumerable<MetadataReference> CreateReferences(IEnumerable<Assembly> rootAssemblies)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Queue<Assembly>(rootAssemblies.Where(static assembly => !assembly.IsDynamic));

        while (pending.Count > 0)
        {
            var assembly = pending.Dequeue();

            if (assembly.IsDynamic || string.IsNullOrWhiteSpace(assembly.Location) || !visited.Add(assembly.FullName ?? assembly.Location))
            {
                continue;
            }

            yield return MetadataReference.CreateFromFile(assembly.Location);

            foreach (var dependencyName in assembly.GetReferencedAssemblies())
            {
                if (ShouldIncludeDependency(dependencyName) && TryLoad(dependencyName, out var dependency))
                {
                    pending.Enqueue(dependency);
                }
            }
        }
    }

    private static bool ShouldIncludeDependency(AssemblyName dependencyName)
    {
        return dependencyName.Name is not null
            && (dependencyName.Name.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal)
                || dependencyName.Name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                || dependencyName.Name.StartsWith("Microsoft.Extensions.", StringComparison.Ordinal));
    }

    private static bool TryLoad(AssemblyName assemblyName, out Assembly assembly)
    {
        try
        {
            assembly = Assembly.Load(assemblyName);
            return true;
        }
        catch (FileNotFoundException)
        {
            assembly = null!;
            return false;
        }
        catch (FileLoadException)
        {
            assembly = null!;
            return false;
        }
    }
}
