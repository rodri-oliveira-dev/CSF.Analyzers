using Microsoft.CodeAnalysis.Testing;

using CSF.Analyzers.Architecture.Rules;

namespace CSF.Analyzers.Tests.FrameworkReferences;

public sealed class RealFrameworkReferenceAnalyzerTests
{
    [Fact]
    public async Task Arc001_reports_real_MVC_action_without_authorization_decision()
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

        await RealFrameworkVerifier<Arc001RequireExplicitAuthorizationOnHttpEndpointsAnalyzer>.VerifyAnalyzerAsync(
            source,
            [],
            AspNetCoreReferenceAssemblyPaths,
            Expected(0, "OrdersController.Get"));
    }

    private static DiagnosticResult Expected(int location, string endpoint)
    {
        return RealFrameworkVerifier<Arc001RequireExplicitAuthorizationOnHttpEndpointsAnalyzer>.Diagnostic("ARC001")
            .WithLocation(location)
            .WithArguments(endpoint);
    }

    private static readonly IEnumerable<string> AspNetCoreReferenceAssemblyPaths =
        RealFrameworkVerifier<Arc001RequireExplicitAuthorizationOnHttpEndpointsAnalyzer>
            .GetPackageReferenceAssemblyPaths("Microsoft.AspNetCore.App.Ref", "9.0.18", "net9.0");
}
