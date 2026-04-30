using Microsoft.EntityFrameworkCore;

namespace Swa.Analyzers.SampleApp.Arch027.Infrastructure;

public sealed class InfrastructureDependencyInfrastructure
{
    private readonly DbContext _dbContext;

    public InfrastructureDependencyInfrastructure(DbContext dbContext)
    {
        _dbContext = dbContext;
    }
}
