using Microsoft.CodeAnalysis.Testing;

using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch031PreferSystemThreadingLockAnalyzerTests
{
    [Fact]
    public async Task Reports_lock_on_object_field()
    {
        const string source = """
public sealed class Worker
{
    private readonly object _gate = new();

    public void Execute()
    {
        lock ({|#0:_gate|})
        {
        }
    }
}
""";

        await VerifyAsync(source, Expected(0, "_gate"));
    }

    [Fact]
    public async Task Reports_lock_on_object_property()
    {
        const string source = """
public sealed class Worker
{
    private object Gate { get; } = new();

    public void Execute()
    {
        lock ({|#0:Gate|})
        {
        }
    }
}
""";

        await VerifyAsync(source, Expected(0, "Gate"));
    }

    [Fact]
    public async Task Reports_lock_on_object_local_variable()
    {
        const string source = """
public sealed class Worker
{
    public void Execute()
    {
        object gate = new();

        lock ({|#0:gate|})
        {
        }
    }
}
""";

        await VerifyAsync(source, Expected(0, "gate"));
    }

    [Fact]
    public async Task Reports_lock_on_new_object()
    {
        const string source = """
public sealed class Worker
{
    public void Execute()
    {
        lock ({|#0:new object()|})
        {
        }
    }
}
""";

        await VerifyAsync(source, Expected(0, "new object()"));
    }

    [Fact]
    public async Task Does_not_report_System_Threading_Lock()
    {
        const string source = """
public sealed class Worker
{
    private readonly System.Threading.Lock _gate = new();

    public void Execute()
    {
        lock (_gate)
        {
        }
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_SemaphoreSlim_usage()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;

public sealed class Worker
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task ExecuteAsync()
    {
        await _semaphore.WaitAsync();
        _semaphore.Release();
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_lock_on_custom_type()
    {
        const string source = """
public sealed class Worker
{
    private readonly Gate _gate = new();

    public void Execute()
    {
        lock (_gate)
        {
        }
    }

    private sealed class Gate
    {
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_lock_on_type_parameter()
    {
        const string source = """
public sealed class Worker
{
    public void Execute<T>(T gate)
    {
        lock (gate)
        {
        }
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_expression_type_is_not_resolved()
    {
        const string source = """
public sealed class Worker
{
    public void Execute()
    {
        lock (MissingGate)
        {
        }
    }
}
""";

        await VerifyAsync(
            source,
            DiagnosticResult.CompilerError("CS0103").WithSpan(5, 15, 5, 26).WithArguments("MissingGate"));
    }

    [Fact]
    public async Task Respects_report_local_variables_false()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH031.report_local_variables = false
""";

        const string source = """
public sealed class Worker
{
    private readonly object _gate = new();

    public void Execute()
    {
        object localGate = new();

        lock (localGate)
        {
        }

        lock ({|#0:_gate|})
        {
        }
    }
}
""";

        await VerifyAsync(source, editorConfig, Expected(0, "_gate"));
    }

    [Fact]
    public async Task Uses_default_report_local_variables_when_configuration_is_invalid()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH031.report_local_variables = maybe
""";

        const string source = """
public sealed class Worker
{
    public void Execute()
    {
        object gate = new();

        lock ({|#0:gate|})
        {
        }
    }
}
""";

        await VerifyAsync(source, editorConfig, Expected(0, "gate"));
    }

    [Fact]
    public async Task Suppresses_diagnostic_when_target_framework_is_below_minimum()
    {
        const string globalConfig = """
is_global = true
build_property.TargetFramework = net8.0
dotnet_diagnostic.ARCH031.minimum_target_framework = net9.0
""";

        const string source = """
public sealed class Worker
{
    private readonly object _gate = new();

    public void Execute()
    {
        lock (_gate)
        {
        }
    }
}
""";

        await VerifyWithGlobalConfigAsync(source, globalConfig);
    }

    [Fact]
    public async Task Suppresses_diagnostic_for_legacy_net_framework_target()
    {
        const string globalConfig = """
is_global = true
build_property.TargetFramework = net48
""";

        const string source = """
public sealed class Worker
{
    private readonly object _gate = new();

    public void Execute()
    {
        lock (_gate)
        {
        }
    }
}
""";

        await VerifyWithGlobalConfigAsync(source, globalConfig);
    }

    [Fact]
    public async Task Invalid_minimum_target_framework_uses_default()
    {
        const string globalConfig = """
is_global = true
build_property.TargetFramework = net8.0
dotnet_diagnostic.ARCH031.minimum_target_framework = latest
""";

        const string source = """
public sealed class Worker
{
    private readonly object _gate = new();

    public void Execute()
    {
        lock (_gate)
        {
        }
    }
}
""";

        await VerifyWithGlobalConfigAsync(source, globalConfig);
    }

    private static Task VerifyAsync(string source, params DiagnosticResult[] expected)
    {
        return VerifyAsync(source, editorConfig: null, expected);
    }

    private static Task VerifyAsync(string source, string? editorConfig, params DiagnosticResult[] expected)
    {
        return Verifier<Arch031PreferSystemThreadingLockAnalyzer>.VerifyAnalyzerAsync(source, editorConfig, expected);
    }

    private static Task VerifyWithGlobalConfigAsync(string source, string globalConfig, params DiagnosticResult[] expected)
    {
        return Verifier<Arch031PreferSystemThreadingLockAnalyzer>.VerifyAnalyzerAsync(
            new[] { ("Test0.cs", source) },
            new[] { ("/.globalconfig", globalConfig) },
            expected);
    }

    private static DiagnosticResult Expected(int location, string lockExpression)
    {
        return Verifier<Arch031PreferSystemThreadingLockAnalyzer>.Diagnostic("ARCH031")
            .WithLocation(location)
            .WithArguments(lockExpression);
    }
}
