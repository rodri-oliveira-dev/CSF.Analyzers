using Microsoft.EntityFrameworkCore;

namespace Swa.Analyzers.SampleApp.Arch027.Domain;

public sealed class InfrastructureDependencyInvalid
{
    // ARCH027: domain code should not depend directly on EF Core.
    private readonly DbContext _dbContext;

    public InfrastructureDependencyInvalid(DbContext dbContext)
    {
        _dbContext = dbContext;
    }
}
