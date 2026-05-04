using Microsoft.EntityFrameworkCore;

namespace Swa.Analyzers.SampleApp.Arch010;

internal static class TokenPropagation_Valid
{
    // Exemplos que NÃO devem gerar diagnóstico ARCH010.

    public static async Task ExecuteAsync(Service service, CancellationToken cancellationToken)
    {
        await service.DoWorkAsync(1, cancellationToken);
    }

    public static async Task ExecuteWithOptionalAsync(Service service, CancellationToken cancellationToken)
    {
        await service.DoWorkWithOptionalAsync(1, cancellationToken);
    }

    public static async Task ExecuteNoTokenAvailableAsync(Service service)
    {
        // Nenhum CancellationToken disponível no escopo — analyzer fica silencioso.
        await service.DoWorkAsync(1);
    }

    public static async Task ExecuteNoOverloadAsync(OtherService service, CancellationToken cancellationToken)
    {
        // Não há overload com CancellationToken — analyzer fica silencioso.
        await service.DoWorkAsync(1);
    }

    public static async Task ExecuteEfCoreAsync(DbContext dbContext, IQueryable<TokenPropagationCustomer> customers, CancellationToken token)
    {
        await dbContext.SaveChangesAsync(token);
        await customers.ToListAsync(token);
    }

    public static async Task ExecuteHttpClientAsync(HttpClient httpClient, CancellationToken ct)
    {
        await httpClient.GetAsync("https://example.test", ct);
    }
}

internal sealed class OtherService
{
    public Task DoWorkAsync(int id)
    {
        return Task.CompletedTask;
    }
}
