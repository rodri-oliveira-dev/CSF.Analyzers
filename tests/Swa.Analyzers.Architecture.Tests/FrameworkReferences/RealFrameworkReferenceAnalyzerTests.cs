using Microsoft.CodeAnalysis.Testing;

using Swa.Analyzers.Architecture.Rules;

namespace Swa.Analyzers.Tests.FrameworkReferences;

public sealed class RealFrameworkReferenceAnalyzerTests
{
    [Fact]
    public async Task Arch020_reports_real_MVC_action_without_authorization_decision()
    {
        const string source = """
using Microsoft.AspNetCore.Mvc;

public sealed class OrdersController : ControllerBase
{
    [{|#0:HttpGet|}("orders")]
    public void Get()
    {
    }
}
""";

        await RealFrameworkVerifier<Arch020RequireExplicitAuthorizationOnHttpEndpointsAnalyzer>.VerifyAnalyzerAsync(
            source,
            [],
            AspNetCoreReferenceAssemblyPaths,
            Expected(0, "OrdersController.Get"));
    }

    private static DiagnosticResult Expected(int location, string endpoint)
    {
        return RealFrameworkVerifier<Arch020RequireExplicitAuthorizationOnHttpEndpointsAnalyzer>.Diagnostic("ARCH020")
            .WithLocation(location)
            .WithArguments(endpoint);
    }

    private static readonly IEnumerable<string> AspNetCoreReferenceAssemblyPaths =
        RealFrameworkVerifier<Arch020RequireExplicitAuthorizationOnHttpEndpointsAnalyzer>
            .GetPackageReferenceAssemblyPaths("Microsoft.AspNetCore.App.Ref", "9.0.16", "net9.0");
}
