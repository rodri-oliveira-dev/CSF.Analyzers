using Microsoft.EntityFrameworkCore;

namespace CSF.Analyzers.SampleApp.Arc002.Domain;

public sealed class InfrastructureDependencyInvalid
{
    // ARC002: código de domínio não deve depender diretamente de EF Core.
    private readonly DbContext _dbContext;

    public InfrastructureDependencyInvalid(DbContext dbContext)
    {
        _dbContext = dbContext;
    }
}
