using Microsoft.Extensions.Logging;

namespace Swa.Analyzers.SampleApp.Arch025;

internal sealed class LoggerCategoryInvalid
{
    private readonly ILogger<OtherService> _logger;

    public LoggerCategoryInvalid(ILogger<OtherService> logger)
    {
        _logger = logger;
    }

    public ILogger<OtherService> Logger => _logger;
}

internal sealed class OtherService
{
}
