namespace Swa.Analyzers.SampleApp.Arch027.Domain;

public interface IInvoiceRepository
{
    Task<Invoice?> FindAsync(Guid id, CancellationToken cancellationToken);
}

public sealed record Invoice(Guid Id);
