using Microsoft.EntityFrameworkCore;

namespace CSF.Analyzers.SampleApp.Arc002.Infrastructure;

public sealed class InfrastructureDependencyInfrastructure
{
    private readonly DbContext _dbContext;

    public InfrastructureDependencyInfrastructure(DbContext dbContext)
    {
        _dbContext = dbContext;
    }
}
