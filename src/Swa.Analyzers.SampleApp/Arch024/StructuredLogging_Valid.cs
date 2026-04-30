using System;

using Microsoft.Extensions.Logging;

namespace Swa.Analyzers.SampleApp.Arch024;

internal sealed class StructuredLoggingValid
{
    private static readonly Action<ILogger, int, Exception?> _customerCreated =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(), "Customer {CustomerId} created");

    private readonly ILogger _logger;

    public StructuredLoggingValid(ILogger logger)
    {
        _logger = logger;
    }

    public void Create(int customerId)
    {
        _logger.LogInformation("Customer {CustomerId} created", customerId);
        _customerCreated(_logger, customerId, null);
    }
}
