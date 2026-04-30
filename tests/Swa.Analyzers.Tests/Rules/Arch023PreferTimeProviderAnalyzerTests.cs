using Microsoft.CodeAnalysis.Testing;

using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch023PreferTimeProviderAnalyzerTests
{
    [Fact]
    public async Task Reports_DateTime_Now()
    {
        const string source = """
using System;

public sealed class InvoiceService
{
    public DateTime Create()
    {
        return DateTime.{|#0:Now|};
    }
}
""";

        await VerifyAsync(source, Expected(0, "DateTime.Now"));
    }

    [Fact]
    public async Task Reports_DateTime_UtcNow()
    {
        const string source = """
using System;

public sealed class InvoiceService
{
    public DateTime Create()
    {
        return DateTime.{|#0:UtcNow|};
    }
}
""";

        await VerifyAsync(source, Expected(0, "DateTime.UtcNow"));
    }

    [Fact]
    public async Task Reports_DateTimeOffset_Now()
    {
        const string source = """
using System;

public sealed class InvoiceService
{
    public DateTimeOffset Create()
    {
        return DateTimeOffset.{|#0:Now|};
    }
}
""";

        await VerifyAsync(source, Expected(0, "DateTimeOffset.Now"));
    }

    [Fact]
    public async Task Reports_DateTimeOffset_UtcNow()
    {
        const string source = """
using System;

public sealed class InvoiceService
{
    public DateTimeOffset Create()
    {
        return DateTimeOffset.{|#0:UtcNow|};
    }
}
""";

        await VerifyAsync(source, Expected(0, "DateTimeOffset.UtcNow"));
    }

    [Fact]
    public async Task Does_not_report_TimeProvider_GetUtcNow()
    {
        const string source = """
using System;

public sealed class InvoiceService
{
    private readonly TimeProvider _timeProvider;

    public InvoiceService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public DateTimeOffset Create()
    {
        return _timeProvider.GetUtcNow();
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_inside_configured_clock_type()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH023.allowed_types = ["MachineTimeSource"]
""";

        const string source = """
using System;

public sealed class MachineTimeSource
{
    public DateTimeOffset GetUtcNow()
    {
        return DateTimeOffset.UtcNow;
    }
}
""";

        await VerifyAsync(source, editorConfig);
    }

    [Fact]
    public async Task Does_not_report_inside_default_clock_or_time_provider_implementation()
    {
        const string source = """
using System;

public sealed class UtcClock
{
    public DateTimeOffset GetUtcNow()
    {
        return DateTimeOffset.UtcNow;
    }
}

public sealed class AppTimeProvider : TimeProvider
{
    public override DateTimeOffset GetUtcNow()
    {
        return DateTimeOffset.UtcNow;
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_inside_tests()
    {
        const string source = """
using System;
using Xunit;

public sealed class InvoiceServiceTests
{
    [Fact]
    public void Creates_invoice()
    {
        var now = DateTimeOffset.UtcNow;
    }
}

namespace Xunit
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class FactAttribute : Attribute
    {
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_in_Program_cs()
    {
        const string source = """
using System;

public sealed class Startup
{
    public DateTimeOffset GetTimestamp()
    {
        return DateTimeOffset.UtcNow;
    }
}
""";

        await Verifier<Arch023PreferTimeProviderAnalyzer>.VerifyAnalyzerAsync(
            (new[] { ("Program.cs", source) }),
            Array.Empty<(string FileName, string Source)>());
    }

    [Fact]
    public async Task Respects_allowed_namespaces_configuration()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH023.allowed_namespaces = ["Infrastructure.Time"]
""";

        const string source = """
using System;

namespace Infrastructure.Time
{
    public sealed class MachineTimeSource
    {
        public DateTimeOffset GetUtcNow()
        {
            return DateTimeOffset.UtcNow;
        }
    }
}
""";

        await VerifyAsync(source, editorConfig);
    }

    [Fact]
    public async Task Invalid_allowed_namespaces_configuration_does_not_suppress_diagnostic()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH023.allowed_namespaces = Infrastructure.Time
""";

        const string source = """
using System;

namespace Infrastructure.Time
{
    public sealed class MachineClockReader
    {
        public DateTimeOffset GetUtcNow()
        {
            return DateTimeOffset.{|#0:UtcNow|};
        }
    }
}
""";

        await VerifyAsync(source, editorConfig, Expected(0, "DateTimeOffset.UtcNow"));
    }

    [Fact]
    public async Task Respects_ignore_simple_logging_configuration()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH023.ignore_simple_logging = true
""";

        const string source = """
using System;

public sealed class InvoiceService
{
    public void Execute(ILogger logger)
    {
        logger.LogInformation("Processing at {Now}", DateTimeOffset.UtcNow);
        var now = DateTimeOffset.{|#0:UtcNow|};
    }
}

public interface ILogger
{
    void LogInformation(string message, object value);
}
""";

        await VerifyAsync(source, editorConfig, Expected(0, "DateTimeOffset.UtcNow"));
    }

    [Fact]
    public async Task Invalid_ignore_simple_logging_configuration_does_not_suppress_diagnostic()
    {
        const string editorConfig = """
root = true

[*.cs]
dotnet_diagnostic.ARCH023.ignore_simple_logging = yes
""";

        const string source = """
using System;

public sealed class InvoiceService
{
    public void Execute(ILogger logger)
    {
        logger.LogInformation("Processing at {Now}", DateTimeOffset.{|#0:UtcNow|});
    }
}

public interface ILogger
{
    void LogInformation(string message, object value);
}
""";

        await VerifyAsync(source, editorConfig, Expected(0, "DateTimeOffset.UtcNow"));
    }

    private static Task VerifyAsync(string source, params DiagnosticResult[] expected)
    {
        return Verifier<Arch023PreferTimeProviderAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    private static Task VerifyAsync(string source, string editorConfig, params DiagnosticResult[] expected)
    {
        return Verifier<Arch023PreferTimeProviderAnalyzer>.VerifyAnalyzerAsync(source, editorConfig, expected);
    }

    private static DiagnosticResult Expected(int location, string clockAccess)
    {
        return Verifier<Arch023PreferTimeProviderAnalyzer>.Diagnostic("ARCH023")
            .WithLocation(location)
            .WithArguments(clockAccess);
    }
}
