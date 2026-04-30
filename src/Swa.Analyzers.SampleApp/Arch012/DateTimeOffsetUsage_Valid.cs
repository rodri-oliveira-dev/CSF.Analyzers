namespace Swa.Analyzers.SampleApp.Arch012;

internal static class DateTimeOffsetUsage_Valid
{
    // Exemplos que NÃO devem gerar diagnóstico ARCH012.

    public static DateTimeOffset GetTimestamp(TimeProvider timeProvider)
    {
        return timeProvider.GetUtcNow();
    }

    public static void Process(DateTimeOffset timestamp)
    {
        _ = timestamp;
    }

    public static DateTimeOffset[] GetTimestamps()
    {
        return [];
    }
}
