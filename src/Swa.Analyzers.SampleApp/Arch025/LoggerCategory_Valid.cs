using Microsoft.Extensions.Logging;

namespace Swa.Analyzers.SampleApp.Arch025;

internal sealed class LoggerCategoryValid
{
    private readonly ILogger<LoggerCategoryValid> _logger;

    public LoggerCategoryValid(ILogger<LoggerCategoryValid> logger)
    {
        _logger = logger;
    }

    public ILogger<LoggerCategoryValid> Logger => _logger;
}
