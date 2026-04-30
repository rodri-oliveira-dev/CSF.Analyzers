using Microsoft.CodeAnalysis.Testing;

using Swa.Analyzers.Core.Rules;

namespace Swa.Analyzers.Tests.Rules;

public sealed class Arch024AvoidInterpolatedStringsInLoggerAnalyzerTests
{
    private const string LoggingStubs = """
using System;

namespace Microsoft.Extensions.Logging
{
    public interface ILogger
    {
    }

    public readonly struct EventId
    {
    }

    public static class LoggerExtensions
    {
        public static void LogTrace(this ILogger logger, string? message, params object?[] args)
        {
        }

        public static void LogDebug(this ILogger logger, string? message, params object?[] args)
        {
        }

        public static void LogInformation(this ILogger logger, string? message, params object?[] args)
        {
        }

        public static void LogWarning(this ILogger logger, string? message, params object?[] args)
        {
        }

        public static void LogError(this ILogger logger, string? message, params object?[] args)
        {
        }

        public static void LogError(this ILogger logger, Exception? exception, string? message, params object?[] args)
        {
        }

        public static void LogCritical(this ILogger logger, string? message, params object?[] args)
        {
        }
    }

    public static class LoggerMessage
    {
        public static Action<ILogger, int, Exception?> Define<T1>(LogLevel logLevel, EventId eventId, string formatString)
        {
            return static (logger, value, exception) => { };
        }
    }

    public enum LogLevel
    {
        Information
    }
}
""";

    [Fact]
    public async Task Reports_interpolated_string_in_ILogger_call()
    {
        const string source = """
using Microsoft.Extensions.Logging;

public sealed class CustomerService
{
    private readonly ILogger _logger;

    public CustomerService(ILogger logger)
    {
        _logger = logger;
    }

    public void Create(int id)
    {
        _logger.LogInformation({|#0:$"Customer {id} created"|});
    }
}
""";

        await VerifyAsync(source, Expected(0, "LogInformation"));
    }

    [Fact]
    public async Task Reports_string_concatenation_in_ILogger_call()
    {
        const string source = """
using Microsoft.Extensions.Logging;

public sealed class CustomerService
{
    private readonly ILogger _logger;

    public CustomerService(ILogger logger)
    {
        _logger = logger;
    }

    public void Find(int id)
    {
        _logger.LogWarning({|#0:"Customer " + id + " not found"|});
    }
}
""";

        await VerifyAsync(source, Expected(0, "LogWarning"));
    }

    [Fact]
    public async Task Reports_main_log_levels()
    {
        const string source = """
using Microsoft.Extensions.Logging;

public sealed class CustomerService
{
    private readonly ILogger _logger;

    public CustomerService(ILogger logger)
    {
        _logger = logger;
    }

    public void Execute(int id)
    {
        _logger.LogTrace({|#0:$"Trace {id}"|});
        _logger.LogDebug({|#1:"Debug " + id|});
        _logger.LogError({|#2:$"Error {id}"|});
        _logger.LogCritical({|#3:"Critical " + id|});
    }
}
""";

        await VerifyAsync(
            source,
            Expected(0, "LogTrace"),
            Expected(1, "LogDebug"),
            Expected(2, "LogError"),
            Expected(3, "LogCritical"));
    }

    [Fact]
    public async Task Reports_exception_overload_message_argument()
    {
        const string source = """
using System;
using Microsoft.Extensions.Logging;

public sealed class CustomerService
{
    private readonly ILogger _logger;

    public CustomerService(ILogger logger)
    {
        _logger = logger;
    }

    public void Execute(Exception ex)
    {
        _logger.LogError(ex, {|#0:$"Error: {ex.Message}"|});
    }
}
""";

        await VerifyAsync(source, Expected(0, "LogError"));
    }

    [Fact]
    public async Task Does_not_report_structured_logging_template()
    {
        const string source = """
using Microsoft.Extensions.Logging;

public sealed class CustomerService
{
    private readonly ILogger _logger;

    public CustomerService(ILogger logger)
    {
        _logger = logger;
    }

    public void Create(int id)
    {
        _logger.LogInformation("Customer {CustomerId} created", id);
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_constant_message_without_parameters()
    {
        const string source = """
using Microsoft.Extensions.Logging;

public sealed class CustomerService
{
    private readonly ILogger _logger;

    public CustomerService(ILogger logger)
    {
        _logger = logger;
    }

    public void Create()
    {
        _logger.LogInformation("Customer created");
        _logger.LogInformation("Customer " + "created");
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_interpolation_outside_ILogger()
    {
        const string source = """
public sealed class CustomerService
{
    public string Create(int id)
    {
        return $"Customer {id} created";
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_non_logger_method_with_same_name()
    {
        const string source = """
public sealed class CustomLogger
{
    public void LogInformation(string message)
    {
    }
}

public sealed class CustomerService
{
    private readonly CustomLogger _logger = new();

    public void Create(int id)
    {
        _logger.LogInformation($"Customer {id} created");
    }
}
""";

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Does_not_report_LoggerMessage_source_generator_pattern()
    {
        const string source = """
using System;
using Microsoft.Extensions.Logging;

public static partial class CustomerLogs
{
    private static readonly Action<ILogger, int, Exception?> CustomerCreated =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(), "Customer {CustomerId} created");

    public static void Created(ILogger logger, int id)
    {
        CustomerCreated(logger, id, null);
    }
}
""";

        await VerifyAsync(source);
    }

    private static Task VerifyAsync(string source, params DiagnosticResult[] expected)
    {
        return Verifier<Arch024AvoidInterpolatedStringsInLoggerAnalyzer>.VerifyAnalyzerAsync(
            (new[]
            {
                ("LoggingStubs.cs", LoggingStubs),
                ("Test0.cs", source),
            }),
            Array.Empty<(string FileName, string Source)>(),
            expected);
    }

    private static DiagnosticResult Expected(int location, string methodName)
    {
        return Verifier<Arch024AvoidInterpolatedStringsInLoggerAnalyzer>.Diagnostic("ARCH024")
            .WithLocation(location)
            .WithArguments(methodName);
    }
}
