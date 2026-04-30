namespace Microsoft.Extensions.Logging;

public interface ILogger
{
}

public interface ILogger<TCategoryName> : ILogger
{
}

public readonly struct EventId
{
}

public enum LogLevel
{
    Information
}

public static class LoggerExtensions
{
    public static void LogInformation(this ILogger logger, string? message, params object?[] args)
    {
    }

    public static void LogWarning(this ILogger logger, string? message, params object?[] args)
    {
    }

    public static void LogError(this ILogger logger, Exception? exception, string? message, params object?[] args)
    {
    }
}

public static class LoggerMessage
{
    public static Action<ILogger, T1, Exception?> Define<T1>(LogLevel logLevel, EventId eventId, string formatString)
    {
        return static (logger, value, exception) => { };
    }
}
