using Microsoft.Extensions.Logging;

namespace Swa.Analyzers.SampleApp.Arch024;

internal sealed class StructuredLoggingInvalid
{
    private readonly ILogger _logger;

    public StructuredLoggingInvalid(ILogger logger)
    {
        _logger = logger;
    }

    public void Create(int customerId)
    {
        _logger.LogInformation($"Customer {customerId} created");
        _logger.LogWarning("Customer " + customerId + " not found");
    }
}
