namespace CSF.Analyzers.SampleApp.Arc002.Domain;

public interface IInvoiceRepository
{
    Task<Invoice?> FindAsync(Guid id, CancellationToken cancellationToken);
}

public sealed record Invoice(Guid Id);
