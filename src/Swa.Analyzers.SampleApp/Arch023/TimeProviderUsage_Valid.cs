namespace Swa.Analyzers.SampleApp.Arch023;

public sealed class TimeProviderUsage_Valid
{
    private readonly TimeProvider _timeProvider;

    public TimeProviderUsage_Valid(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public DateTimeOffset CreateInvoiceTimestamp()
    {
        return _timeProvider.GetUtcNow();
    }
}
