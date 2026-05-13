using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch010EnforceCancellationTokenPropagationAnalyzerTests
{
    #region Invalid scenarios

    [Fact]
    public async Task Reports_when_overload_with_cancellationtoken_exists_and_token_is_available()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;

public sealed class Service
{
    public Task DoWorkAsync(int id)
    {
        return Task.CompletedTask;
    }

    public Task DoWorkAsync(int id, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public sealed class Sample
{
    public async Task ExecuteAsync(Service service, CancellationToken cancellationToken)
    {
        await service.DoWorkAsync(1);
    }
}
""";

        var expected = Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.Diagnostic("ARCH010")
            .WithSpan(21, 23, 21, 34)
            .WithArguments("DoWorkAsync");

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_when_optional_cancellationtoken_parameter_is_not_supplied()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;

public sealed class Service
{
    public Task DoWorkAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public sealed class Sample
{
    public async Task ExecuteAsync(Service service, CancellationToken cancellationToken)
    {
        await service.DoWorkAsync(1);
    }
}
""";

        var expected = Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.Diagnostic("ARCH010")
            .WithSpan(16, 23, 16, 34)
            .WithArguments("DoWorkAsync");

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_when_valuetask_overload_with_cancellationtoken_exists_and_token_is_available()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;

public sealed class Service
{
    public ValueTask DoWorkAsync(int id)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask DoWorkAsync(int id, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}

public sealed class Sample
{
    public async ValueTask ExecuteAsync(Service service, CancellationToken cancellationToken)
    {
        await service.DoWorkAsync(1);
    }
}
""";

        var expected = Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.Diagnostic("ARCH010")
            .WithSpan(21, 23, 21, 34)
            .WithArguments("DoWorkAsync");

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_when_local_cancellationtoken_is_available()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;

public sealed class Service
{
    public Task DoWorkAsync(int id)
    {
        return Task.CompletedTask;
    }

    public Task DoWorkAsync(int id, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public sealed class Sample
{
    public async Task ExecuteAsync(Service service)
    {
        var cancellationToken = new CancellationToken();
        await service.DoWorkAsync(1);
    }
}
""";

        var expected = Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.Diagnostic("ARCH010")
            .WithSpan(22, 23, 22, 34)
            .WithArguments("DoWorkAsync");

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_when_field_cancellationtoken_is_available()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;

public sealed class Service
{
    public Task DoWorkAsync(int id)
    {
        return Task.CompletedTask;
    }

    public Task DoWorkAsync(int id, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public sealed class Sample
{
    private readonly CancellationToken _token;

    public Sample(CancellationToken token)
    {
        _token = token;
    }

    public async Task ExecuteAsync(Service service)
    {
        await service.DoWorkAsync(1);
    }
}
""";

        var expected = Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.Diagnostic("ARCH010")
            .WithSpan(28, 23, 28, 34)
            .WithArguments("DoWorkAsync");

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_when_property_cancellationtoken_is_available()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;

public sealed class Service
{
    public Task DoWorkAsync(int id)
    {
        return Task.CompletedTask;
    }

    public Task DoWorkAsync(int id, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public sealed class Sample
{
    public CancellationToken Token { get; set; }

    public async Task ExecuteAsync(Service service)
    {
        await service.DoWorkAsync(1);
    }
}
""";

        var expected = Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.Diagnostic("ARCH010")
            .WithSpan(23, 23, 23, 34)
            .WithArguments("DoWorkAsync");

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_on_static_method_with_overload()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;

public static class Service
{
    public static Task DoWorkAsync(int id)
    {
        return Task.CompletedTask;
    }

    public static Task DoWorkAsync(int id, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public sealed class Sample
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Service.DoWorkAsync(1);
    }
}
""";

        var expected = Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.Diagnostic("ARCH010")
            .WithSpan(21, 23, 21, 34)
            .WithArguments("DoWorkAsync");

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_for_ef_core_async_methods_when_token_is_available()
    {
        const string source = """
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.EntityFrameworkCore
{
    public sealed class DbContext
    {
        public Task<int> SaveChangesAsync() => Task.FromResult(0);
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(0);
    }

    public static class EntityFrameworkQueryableExtensions
    {
        public static Task<List<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source) => Task.FromResult(new List<TSource>());
        public static Task<List<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken) => Task.FromResult(new List<TSource>());
        public static Task<TSource?> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source) => Task.FromResult(default(TSource));
        public static Task<TSource?> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken) => Task.FromResult(default(TSource));
        public static Task<TSource?> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source) => Task.FromResult(default(TSource));
        public static Task<TSource?> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken) => Task.FromResult(default(TSource));
        public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source) => Task.FromResult(false);
        public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken) => Task.FromResult(false);
        public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source) => Task.FromResult(0);
        public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken) => Task.FromResult(0);
    }
}

public sealed class Customer
{
}

public sealed class Repository
{
    public async Task ExecuteAsync(Microsoft.EntityFrameworkCore.DbContext dbContext, IQueryable<Customer> customers, CancellationToken token)
    {
        await dbContext.SaveChangesAsync();
        await customers.ToListAsync();
        await customers.FirstOrDefaultAsync();
        await customers.SingleOrDefaultAsync();
        await customers.AnyAsync();
        await customers.CountAsync();
    }
}
""";

        var expectedSaveChanges = Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.Diagnostic("ARCH010")
            .WithSpan(38, 25, 38, 41)
            .WithArguments("SaveChangesAsync");
        var expectedToList = Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.Diagnostic("ARCH010")
            .WithSpan(39, 25, 39, 36)
            .WithArguments("ToListAsync");
        var expectedFirstOrDefault = Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.Diagnostic("ARCH010")
            .WithSpan(40, 25, 40, 44)
            .WithArguments("FirstOrDefaultAsync");
        var expectedSingleOrDefault = Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.Diagnostic("ARCH010")
            .WithSpan(41, 25, 41, 45)
            .WithArguments("SingleOrDefaultAsync");
        var expectedAny = Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.Diagnostic("ARCH010")
            .WithSpan(42, 25, 42, 33)
            .WithArguments("AnyAsync");
        var expectedCount = Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.Diagnostic("ARCH010")
            .WithSpan(43, 25, 43, 35)
            .WithArguments("CountAsync");

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(
            source,
            expectedSaveChanges,
            expectedToList,
            expectedFirstOrDefault,
            expectedSingleOrDefault,
            expectedAny,
            expectedCount);
    }

    [Fact]
    public async Task Reports_for_httpclient_async_methods_when_token_is_available()
    {
        const string source = """
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public sealed class ApiController
{
    public async Task ExecuteAsync(HttpClient httpClient, HttpRequestMessage request, HttpContent content, CancellationToken ct)
    {
        await httpClient.GetAsync("https://example.test");
        await httpClient.PostAsync("https://example.test", content);
        await httpClient.PutAsync("https://example.test", content);
        await httpClient.DeleteAsync("https://example.test");
        await httpClient.SendAsync(request);
    }
}
""";

        var expectedGet = Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.Diagnostic("ARCH010")
            .WithSpan(9, 26, 9, 34)
            .WithArguments("GetAsync");
        var expectedPost = Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.Diagnostic("ARCH010")
            .WithSpan(10, 26, 10, 35)
            .WithArguments("PostAsync");
        var expectedPut = Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.Diagnostic("ARCH010")
            .WithSpan(11, 26, 11, 34)
            .WithArguments("PutAsync");
        var expectedDelete = Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.Diagnostic("ARCH010")
            .WithSpan(12, 26, 12, 37)
            .WithArguments("DeleteAsync");
        var expectedSend = Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.Diagnostic("ARCH010")
            .WithSpan(13, 26, 13, 35)
            .WithArguments("SendAsync");

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(
            source,
            expectedGet,
            expectedPost,
            expectedPut,
            expectedDelete,
            expectedSend);
    }

    #endregion

    #region Valid scenarios

    [Fact]
    public async Task Does_not_report_when_cancellationtoken_is_already_passed()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;

public sealed class Service
{
    public Task DoWorkAsync(int id)
    {
        return Task.CompletedTask;
    }

    public Task DoWorkAsync(int id, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public sealed class Sample
{
    public async Task ExecuteAsync(Service service, CancellationToken cancellationToken)
    {
        await service.DoWorkAsync(1, cancellationToken);
    }
}
""";

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_optional_cancellationtoken_is_passed()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;

public sealed class Service
{
    public Task DoWorkAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public sealed class Sample
{
    public async Task ExecuteAsync(Service service, CancellationToken cancellationToken)
    {
        await service.DoWorkAsync(1, cancellationToken);
    }
}
""";

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_no_cancellationtoken_is_available_in_scope()
    {
        const string source = """
using System.Threading.Tasks;

public sealed class Service
{
    public Task DoWorkAsync(int id)
    {
        return Task.CompletedTask;
    }

    public Task DoWorkAsync(int id, System.Threading.CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public sealed class Sample
{
    public async Task ExecuteAsync(Service service)
    {
        await service.DoWorkAsync(1);
    }
}
""";

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_no_overload_with_cancellationtoken_exists()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;

public sealed class Service
{
    public Task DoWorkAsync(int id)
    {
        return Task.CompletedTask;
    }
}

public sealed class Sample
{
    public async Task ExecuteAsync(Service service, CancellationToken cancellationToken)
    {
        await service.DoWorkAsync(1);
    }
}
""";

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_overload_has_different_signature()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;

public sealed class Service
{
    public Task DoWorkAsync(int id)
    {
        return Task.CompletedTask;
    }

    public Task DoWorkAsync(string name, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public sealed class Sample
{
    public async Task ExecuteAsync(Service service, CancellationToken cancellationToken)
    {
        await service.DoWorkAsync(1);
    }
}
""";

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_cancellationtoken_default_is_passed()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;

public sealed class Service
{
    public Task DoWorkAsync(int id)
    {
        return Task.CompletedTask;
    }

    public Task DoWorkAsync(int id, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public sealed class Sample
{
    public async Task ExecuteAsync(Service service)
    {
        await service.DoWorkAsync(1, CancellationToken.None);
    }
}
""";

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_cancellationtoken_none_is_passed_even_if_token_is_available()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;

public sealed class Service
{
    public Task DoWorkAsync(int id)
    {
        return Task.CompletedTask;
    }

    public Task DoWorkAsync(int id, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public sealed class Sample
{
    public async Task ExecuteAsync(Service service, CancellationToken cancellationToken)
    {
        await service.DoWorkAsync(1, CancellationToken.None);
    }
}
""";

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_passing_default_cancellationtoken_literal()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;

public sealed class Service
{
    public Task DoWorkAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public sealed class Sample
{
    public async Task ExecuteAsync(Service service)
    {
        await service.DoWorkAsync(1, default);
    }
}
""";

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_for_non_task_methods_without_overload()
    {
        const string source = """
using System.Threading;

public sealed class Service
{
    public void DoWork(int id)
    {
    }
}

public sealed class Sample
{
    public void Execute(Service service, CancellationToken cancellationToken)
    {
        service.DoWork(1);
    }
}
""";

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_for_sync_method_with_optional_cancellationtoken()
    {
        const string source = """
using System.Threading;

public sealed class Service
{
    public void DoWork(int id, CancellationToken cancellationToken = default)
    {
    }
}

public sealed class Sample
{
    public void Execute(Service service, CancellationToken cancellationToken)
    {
        service.DoWork(1);
    }
}
""";

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_for_sync_method_with_optional_cancellationtoken_inside_async_scope()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;

public sealed class Service
{
    public void DoWork(int id, CancellationToken cancellationToken = default)
    {
    }
}

public sealed class Sample
{
    public async Task ExecuteAsync(Service service, CancellationToken cancellationToken)
    {
        service.DoWork(1);
        await Task.CompletedTask;
    }
}
""";

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_for_sync_method_with_cancellationtoken_overload()
    {
        const string source = """
using System.Threading;

public sealed class Service
{
    public void DoWork(int id)
    {
    }

    public void DoWork(int id, CancellationToken cancellationToken)
    {
    }
}

public sealed class Sample
{
    public void Execute(Service service, CancellationToken cancellationToken)
    {
        service.DoWork(1);
    }
}
""";

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Does_not_report_for_sync_method_with_cancellationtoken_overload_inside_async_scope()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;

public sealed class Service
{
    public void DoWork(int id)
    {
    }

    public void DoWork(int id, CancellationToken cancellationToken)
    {
    }
}

public sealed class Sample
{
    public async Task ExecuteAsync(Service service, CancellationToken cancellationToken)
    {
        service.DoWork(1);
        await Task.CompletedTask;
    }
}
""";

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    #endregion

    #region Edge cases

    [Fact]
    public async Task Reports_when_lambda_has_cancellationtoken_parameter()
    {
        const string source = """
using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class Service
{
    public Task DoWorkAsync(int id)
    {
        return Task.CompletedTask;
    }

    public Task DoWorkAsync(int id, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public sealed class Sample
{
    public Task ExecuteAsync(Service service)
    {
        return RunAsync(async cancellationToken =>
        {
            await service.DoWorkAsync(1);
        });
    }

    private static Task RunAsync(Func<CancellationToken, Task> action)
    {
        return action(CancellationToken.None);
    }
}
""";

        var expected = Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.Diagnostic("ARCH010")
            .WithSpan(24, 27, 24, 38)
            .WithArguments("DoWorkAsync");

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_when_local_function_has_cancellationtoken_parameter()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;

public sealed class Service
{
    public Task DoWorkAsync(int id)
    {
        return Task.CompletedTask;
    }

    public Task DoWorkAsync(int id, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public sealed class Sample
{
    public async Task ExecuteAsync(Service service, CancellationToken cancellationToken)
    {
        async Task InnerAsync(CancellationToken token)
        {
            await service.DoWorkAsync(1);
        }

        await InnerAsync(cancellationToken);
    }
}
""";

        var expected = Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.Diagnostic("ARCH010")
            .WithSpan(23, 27, 23, 38)
            .WithArguments("DoWorkAsync");

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_when_named_argument_skips_cancellationtoken()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;

public sealed class Service
{
    public Task DoWorkAsync(int id, string name = "", CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public sealed class Sample
{
    public async Task ExecuteAsync(Service service, CancellationToken cancellationToken)
    {
        await service.DoWorkAsync(id: 1);
    }
}
""";

        var expected = Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.Diagnostic("ARCH010")
            .WithSpan(16, 23, 16, 34)
            .WithArguments("DoWorkAsync");

        await Verifier<Arch010EnforceCancellationTokenPropagationAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    #endregion
}
