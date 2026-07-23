using Microsoft.CodeAnalysis.Testing;

using Swa.Analyzers.Reliability.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Rel006AvoidScopedDependencyCaptureInHostedServicesAnalyzerTests
{
    private const string FrameworkStubs = """
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting
{
    public interface IHostedService
    {
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }

    public abstract class BackgroundService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        protected abstract Task ExecuteAsync(CancellationToken stoppingToken);
    }
}

namespace Microsoft.EntityFrameworkCore
{
    public abstract class DbContext
    {
    }

    public interface IDbContextFactory<TContext>
        where TContext : DbContext
    {
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public interface IServiceScopeFactory
    {
    }
}

namespace Microsoft.Extensions.Options
{
    public interface IOptions<TOptions>
    {
        TOptions Value { get; }
    }

    public interface IOptionsMonitor<TOptions>
    {
        TOptions CurrentValue { get; }
    }

    public interface IOptionsSnapshot<TOptions> : IOptions<TOptions>
    {
    }
}
""";

    [Fact]
    public async Task Reports_BackgroundService_constructor_capturing_DbContext()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

public sealed class AppDbContext : DbContext
{
}

public sealed class Worker : BackgroundService
{
    private readonly AppDbContext _db;

    public Worker(AppDbContext {|#0:db|})
    {
        _db = db;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}
""";

        await VerifyAsync(source, Expected(0, "AppDbContext", "Worker"));
    }

    [Fact]
    public async Task Reports_Direct_IHostedService_constructor_capturing_IOptionsSnapshot()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

public sealed class WorkerOptions
{
}

public sealed class Worker : IHostedService
{
    private readonly IOptionsSnapshot<WorkerOptions> _options;

    public Worker(IOptionsSnapshot<WorkerOptions> {|#0:options|})
    {
        _options = options;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
""";

        await VerifyAsync(source, Expected(0, "Microsoft.Extensions.Options.IOptionsSnapshot<WorkerOptions>", "Worker"));
    }

    [Fact]
    public async Task Reports_Field_DbContext_in_hosted_service()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

public sealed class AppDbContext : DbContext
{
}

public sealed class Worker : BackgroundService
{
    private readonly AppDbContext {|#0:_db|} = null!;

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}
""";

        await VerifyAsync(source, Expected(0, "AppDbContext", "Worker"));
    }

    [Fact]
    public async Task Reports_Primary_constructor_parameter_used_in_instance_member()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

public sealed class AppDbContext : DbContext
{
}

public sealed class Worker(AppDbContext {|#0:db|}) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = db;
        return Task.CompletedTask;
    }
}
""";

        await VerifyAsync(source, Expected(0, "AppDbContext", "Worker"));
    }

    [Fact]
    public async Task Reports_Abstract_hosted_service_base_class()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

public sealed class AppDbContext : DbContext
{
}

public abstract class WorkerBase : BackgroundService
{
    private readonly AppDbContext _db;

    protected WorkerBase(AppDbContext {|#0:db|})
    {
        _db = db;
    }
}
""";

        await VerifyAsync(source, Expected(0, "AppDbContext", "WorkerBase"));
    }

    [Fact]
    public async Task Reports_Generic_DbContext_derived_type()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

public sealed class AppDbContext<TTenant> : DbContext
{
}

public sealed class Tenant
{
}

public sealed class Worker : BackgroundService
{
    private readonly AppDbContext<Tenant> _db;

    public Worker(AppDbContext<Tenant> {|#0:db|})
    {
        _db = db;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}
""";

        await VerifyAsync(source, Expected(0, "AppDbContext<Tenant>", "Worker"));
    }

    [Fact]
    public async Task Reports_Multiple_scoped_dependencies_in_same_constructor()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

public sealed class AppDbContext : DbContext
{
}

public sealed class WorkerOptions
{
}

public sealed class Worker : BackgroundService
{
    private readonly AppDbContext _db;
    private readonly IOptionsSnapshot<WorkerOptions> _options;

    public Worker(AppDbContext {|#0:db|}, IOptionsSnapshot<WorkerOptions> {|#1:options|})
    {
        _db = db;
        _options = options;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}
""";

        await VerifyAsync(
            source,
            Expected(0, "AppDbContext", "Worker"),
            Expected(1, "Microsoft.Extensions.Options.IOptionsSnapshot<WorkerOptions>", "Worker"));
    }

    [Fact]
    public async Task Reports_Custom_type_only_when_configured()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace MyApp.Data;

public sealed class UnitOfWork
{
}

public sealed class Worker : BackgroundService
{
    private readonly UnitOfWork _unitOfWork;

    public Worker(UnitOfWork {|#0:unitOfWork|})
    {
        _unitOfWork = unitOfWork;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}
""";

        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.REL006.scoped_type_patterns = MyApp.Data.*
""";

        await VerifyAsync(source, editorConfig, Expected(0, "MyApp.Data.UnitOfWork", "MyApp.Data.Worker"));
    }

    [Fact]
    public async Task Does_not_report_custom_type_without_configuration()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace MyApp.Data;

public sealed class UnitOfWork
{
}

public sealed class Worker : BackgroundService
{
    private readonly UnitOfWork _unitOfWork;

    public Worker(UnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_known_safe_types()
    {
        const string source = """
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

public sealed class AppDbContext : DbContext
{
}

public sealed class WorkerOptions
{
}

public sealed class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IOptionsMonitor<WorkerOptions> _monitor;
    private readonly IOptions<WorkerOptions> _options;
    private readonly IServiceProvider _services;

    public Worker(
        IServiceScopeFactory scopeFactory,
        IDbContextFactory<AppDbContext> dbContextFactory,
        IOptionsMonitor<WorkerOptions> monitor,
        IOptions<WorkerOptions> options,
        IServiceProvider services)
    {
        _scopeFactory = scopeFactory;
        _dbContextFactory = dbContextFactory;
        _monitor = monitor;
        _options = options;
        _services = services;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_DbContext_in_common_class()
    {
        const string source = """
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContext
{
}

public sealed class Handler
{
    private readonly AppDbContext _db;

    public Handler(AppDbContext db)
    {
        _db = db;
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_same_simple_name_in_custom_namespace()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Custom;

public sealed class DbContext
{
}

public sealed class Worker : BackgroundService
{
    private readonly DbContext _db;

    public Worker(DbContext db)
    {
        _db = db;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_when_hosting_reference_is_missing()
    {
        const string source = """
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContext
{
}

public class BackgroundService
{
}

public sealed class Worker : BackgroundService
{
    private readonly AppDbContext _db;

    public Worker(AppDbContext db)
    {
        _db = db;
    }
}
""";

        await Verifier<Rel006AvoidScopedDependencyCaptureInHostedServicesAnalyzer>.VerifyAnalyzerAsync(
            (new[]
            {
                ("EfCoreOnlyStubs.cs", """
namespace Microsoft.EntityFrameworkCore
{
    public abstract class DbContext
    {
    }
}
"""),
                ("/0/Test0.cs", source),
            }),
            Array.Empty<(string FileName, string Source)>());
    }

    [Fact]
    public async Task Ignores_invalid_empty_duplicate_and_excess_custom_patterns_but_keeps_known_types_active()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace MyApp.Data;

public sealed class UnitOfWork
{
}

public sealed class AppDbContext : DbContext
{
}

public sealed class Worker : BackgroundService
{
    private readonly UnitOfWork _unitOfWork;
    private readonly AppDbContext _db;

    public Worker(UnitOfWork unitOfWork, AppDbContext {|#0:db|})
    {
        _unitOfWork = unitOfWork;
        _db = db;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}
""";

        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.REL006.scoped_type_patterns = ; ;MyApp.Data.Unit Of Work;MyApp.Data.Unit Of Work;Invalid[Pattern]
""";

        await VerifyAsync(source, editorConfig, Expected(0, "MyApp.Data.AppDbContext", "MyApp.Data.Worker"));
    }

    [Fact]
    public async Task Matches_configured_patterns_case_sensitively_and_by_full_name()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace MyApp.Data
{
    public sealed class UnitOfWork
    {
    }
}

namespace MyApp.data
{
    public sealed class LowerCaseUnitOfWork
    {
    }

    public sealed class Worker : BackgroundService
    {
        private readonly MyApp.Data.UnitOfWork _first;
        private readonly LowerCaseUnitOfWork _second;

        public Worker(MyApp.Data.UnitOfWork {|#0:first|}, LowerCaseUnitOfWork second)
        {
            _first = first;
            _second = second;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
    }
}
""";

        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.REL006.scoped_type_patterns = MyApp.Data.*
""";

        await VerifyAsync(source, editorConfig, Expected(0, "MyApp.Data.UnitOfWork", "MyApp.data.Worker"));
    }

    [Fact]
    public async Task Honors_file_scoped_editorconfig_patterns()
    {
        const string workerWithConfig = """
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MyApp.Data;

public sealed class ConfiguredWorker : BackgroundService
{
    private readonly UnitOfWork _unitOfWork;

    public ConfiguredWorker(UnitOfWork {|#0:unitOfWork|})
    {
        _unitOfWork = unitOfWork;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}
""";

        const string workerWithoutConfig = """
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MyApp.Data;

public sealed class OtherWorker : BackgroundService
{
    private readonly UnitOfWork _unitOfWork;

    public OtherWorker(UnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}
""";

        const string customType = """
namespace MyApp.Data;

public sealed class UnitOfWork
{
}
""";

        const string editorConfig = """
root = true

[ConfiguredWorker.cs]
dotnet_diagnostic.REL006.scoped_type_patterns = MyApp.Data.*
""";

        await Verifier<Rel006AvoidScopedDependencyCaptureInHostedServicesAnalyzer>.VerifyAnalyzerAsync(
            [
                ("/0/FrameworkStubs.cs", FrameworkStubs),
                ("/0/UnitOfWork.cs", customType),
                ("/0/ConfiguredWorker.cs", workerWithConfig),
                ("/0/OtherWorker.cs", workerWithoutConfig),
            ],
            [("/.editorconfig", editorConfig)],
            Expected(0, "MyApp.Data.UnitOfWork", "ConfiguredWorker"));
    }

    [Fact]
    public async Task Does_not_report_generated_code()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

public sealed class AppDbContext : DbContext
{
}

public sealed class Worker : BackgroundService
{
    private readonly AppDbContext _db;

    public Worker(AppDbContext db)
    {
        _db = db;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}
""";

        await Verifier<Rel006AvoidScopedDependencyCaptureInHostedServicesAnalyzer>.VerifyAnalyzerAsync(
            [
                ("/0/FrameworkStubs.cs", FrameworkStubs),
                ("/0/Worker.generated.cs", source),
            ],
            Array.Empty<(string FileName, string Source)>());
    }

    [Fact]
    public async Task Does_not_report_code_in_test_type()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Xunit;

public sealed class AppDbContext : DbContext
{
}

public sealed class WorkerTests : BackgroundService
{
    private readonly AppDbContext _db;

    public WorkerTests(AppDbContext db)
    {
        _db = db;
    }

    [Fact]
    public void Test()
    {
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}

namespace Xunit
{
    public sealed class FactAttribute : System.Attribute
    {
    }
}
""";

        await VerifyAsync(source);
    }

    private static Task VerifyAsync(string source, params DiagnosticResult[] expected)
    {
        return VerifyAsync(source, editorConfig: null, expected);
    }

    private static Task VerifyAsync(string source, string? editorConfig, params DiagnosticResult[] expected)
    {
        return Verifier<Rel006AvoidScopedDependencyCaptureInHostedServicesAnalyzer>.VerifyAnalyzerAsync(
            [
                ("/0/FrameworkStubs.cs", FrameworkStubs),
                ("/0/Test0.cs", source),
            ],
            editorConfig is null
                ? Array.Empty<(string FileName, string Source)>()
                : [("/.editorconfig", editorConfig)],
            expected);
    }

    private static DiagnosticResult Expected(int location, string dependencyType, string hostedServiceType)
    {
        return Verifier<Rel006AvoidScopedDependencyCaptureInHostedServicesAnalyzer>.Diagnostic("REL006")
            .WithLocation(location)
            .WithArguments(dependencyType, hostedServiceType);
    }
}
