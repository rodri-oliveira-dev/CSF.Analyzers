using Swa.Analyzers.CodeFixes;
using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch001AvoidAsyncVoidCodeFixProviderTests
{
    [Fact]
    public async Task Changes_async_void_method_to_async_task()
    {
        const string source = """
public sealed class Sample
{
    public async void DoWork()
    {
        await System.Threading.Tasks.Task.Delay(1);
    }
}
""";

        const string fixedSource = """
using System.Threading.Tasks;

public sealed class Sample
{
    public async Task DoWork()
    {
        await System.Threading.Tasks.Task.Delay(1);
    }
}
""";

        await CodeFixVerifier<Arch001AvoidAsyncVoidAnalyzer, Arch001AvoidAsyncVoidCodeFixProvider>
            .VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task Changes_async_void_local_function_to_async_task()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Sample
{
    public async Task ExecuteAsync()
    {
        async void Local()
        {
            await Task.Delay(1);
        }

        Local();
    }
}
""";

        const string fixedSource = """
using System.Threading.Tasks;

public sealed class Sample
{
    public async Task ExecuteAsync()
    {
        async Task Local()
        {
            await Task.Delay(1);
        }

        Local();
    }
}
""";

        await CodeFixVerifier<Arch001AvoidAsyncVoidAnalyzer, Arch001AvoidAsyncVoidCodeFixProvider>
            .VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task Does_not_offer_code_fix_for_async_lambda_assigned_to_action()
    {
        const string source = """
using System;
using System.Threading.Tasks;

public sealed class Sample
{
    public void Execute()
    {
        Action action = async () =>
        {
            await Task.Delay(1);
        };
    }
}
""";

        await CodeFixVerifier<Arch001AvoidAsyncVoidAnalyzer, Arch001AvoidAsyncVoidCodeFixProvider>
            .VerifyNoCodeFixAsync(source);
    }

    [Fact]
    public async Task Does_not_offer_code_fix_for_interface_implementation()
    {
        const string source = """
using System.Threading.Tasks;

public interface IWorker
{
    void DoWork();
}

public sealed class Worker : IWorker
{
    public async void DoWork()
    {
        await Task.Delay(1);
    }
}
""";

        await CodeFixVerifier<Arch001AvoidAsyncVoidAnalyzer, Arch001AvoidAsyncVoidCodeFixProvider>
            .VerifyNoCodeFixAsync(source);
    }

    [Fact]
    public async Task Does_not_offer_code_fix_for_override()
    {
        const string source = """
using System.Threading.Tasks;

public abstract class WorkerBase
{
    public abstract void DoWork();
}

public sealed class Worker : WorkerBase
{
    public override async void DoWork()
    {
        await Task.Delay(1);
    }
}
""";

        await CodeFixVerifier<Arch001AvoidAsyncVoidAnalyzer, Arch001AvoidAsyncVoidCodeFixProvider>
            .VerifyNoCodeFixAsync(source);
    }
}
