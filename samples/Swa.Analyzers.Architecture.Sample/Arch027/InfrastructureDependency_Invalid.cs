using Microsoft.EntityFrameworkCore;

namespace Swa.Analyzers.SampleApp.Arch027.Domain;

public sealed class InfrastructureDependencyInvalid
{
    // ARCH027: código de domínio não deve depender diretamente de EF Core.
    private readonly DbContext _dbContext;

    public InfrastructureDependencyInvalid(DbContext dbContext)
    {
        _dbContext = dbContext;
    }
}
