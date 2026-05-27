namespace Swa.Analyzers.SampleApp.Arch023;

public sealed class SystemClockAccess_Invalid
{
    public DateTimeOffset CreateInvoiceTimestamp()
    {
        // ARCH023: prefira receber TimeProvider e chamar GetUtcNow().
        return DateTimeOffset.UtcNow;
    }

    public DateTimeOffset CreateLocalTimestamp()
    {
        // ARCH023: acesso direto ao relógio do sistema dificulta testes determinísticos.
        var now = DateTime.Now;
        return new DateTimeOffset(now);
    }
}
