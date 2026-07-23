using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace CSF.Analyzers.Architecture.Rules;

internal static class TestContextHelper
{
    public static ImmutableArray<INamedTypeSymbol> GetKnownTestMethodAttributes(Compilation compilation)
    {
        var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();

        AddIfNotNull(builder, compilation.GetTypeByMetadataName("Xunit.FactAttribute"));
        AddIfNotNull(builder, compilation.GetTypeByMetadataName("Xunit.TheoryAttribute"));

        AddIfNotNull(builder, compilation.GetTypeByMetadataName("NUnit.Framework.TestAttribute"));
        AddIfNotNull(builder, compilation.GetTypeByMetadataName("NUnit.Framework.TestCaseAttribute"));
        AddIfNotNull(builder, compilation.GetTypeByMetadataName("NUnit.Framework.TestCaseSourceAttribute"));
        AddIfNotNull(builder, compilation.GetTypeByMetadataName("NUnit.Framework.SetUpAttribute"));
        AddIfNotNull(builder, compilation.GetTypeByMetadataName("NUnit.Framework.TearDownAttribute"));
        AddIfNotNull(builder, compilation.GetTypeByMetadataName("NUnit.Framework.OneTimeSetUpAttribute"));
        AddIfNotNull(builder, compilation.GetTypeByMetadataName("NUnit.Framework.OneTimeTearDownAttribute"));

        AddIfNotNull(builder, compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute"));
        AddIfNotNull(builder, compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.DataTestMethodAttribute"));
        AddIfNotNull(builder, compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute"));
        AddIfNotNull(builder, compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanupAttribute"));
        AddIfNotNull(builder, compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.ClassInitializeAttribute"));
        AddIfNotNull(builder, compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.ClassCleanupAttribute"));

        return builder.ToImmutable();
    }

    public static bool IsTestMethod(IMethodSymbol method, ImmutableArray<INamedTypeSymbol> testMethodAttributes)
    {
        if (testMethodAttributes.IsDefaultOrEmpty)
        {
            return false;
        }

        foreach (var attribute in method.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass is null)
            {
                continue;
            }

            foreach (var testAttribute in testMethodAttributes)
            {
                if (SymbolEqualityComparer.Default.Equals(attributeClass, testAttribute))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool IsWithinTestContext(
        ISymbol containingSymbol,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        for (ISymbol? current = containingSymbol; current is not null; current = current.ContainingSymbol)
        {
            if (current is IMethodSymbol method && IsTestMethod(method, testMethodAttributes))
            {
                return true;
            }

            if (current is INamedTypeSymbol type && IsTestType(type, testMethodAttributes, isTestTypeCache))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsTestType(
        INamedTypeSymbol type,
        ImmutableArray<INamedTypeSymbol> testMethodAttributes,
        ConcurrentDictionary<INamedTypeSymbol, bool> isTestTypeCache)
    {
        return isTestTypeCache.GetOrAdd(type, _ => ComputeIsTestType(type, testMethodAttributes));
    }

    private static bool ComputeIsTestType(INamedTypeSymbol type, ImmutableArray<INamedTypeSymbol> testMethodAttributes)
    {
        foreach (var member in type.GetMembers())
        {
            if (member is IMethodSymbol method && IsTestMethod(method, testMethodAttributes))
            {
                return true;
            }
        }

        return false;
    }

    private static void AddIfNotNull(ImmutableArray<INamedTypeSymbol>.Builder builder, INamedTypeSymbol? symbol)
    {
        if (symbol is not null)
        {
            builder.Add(symbol);
        }
    }
}
