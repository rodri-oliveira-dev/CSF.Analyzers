using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore;

public abstract class DbContext
{
    public int SaveChanges() => 0;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }
}

public abstract class DbSet<TEntity> : IQueryable<TEntity>
{
    public Type ElementType => typeof(TEntity);

    public Expression Expression => throw new NotImplementedException();

    public IQueryProvider Provider => throw new NotImplementedException();

    public IEnumerator<TEntity> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public enum QueryTrackingBehavior
{
    TrackAll,
    NoTracking,
}

public static class EntityFrameworkQueryableExtensions
{
    public static IQueryable<TEntity> AsNoTracking<TEntity>(this IQueryable<TEntity> source) => source;

    public static IQueryable<TEntity> AsTracking<TEntity>(this IQueryable<TEntity> source) => source;

    public static Task<List<TEntity>> ToListAsync<TEntity>(
        this IQueryable<TEntity> source,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<TEntity>());
    }

    public static Task<TEntity?> FirstOrDefaultAsync<TEntity>(
        this IQueryable<TEntity> source,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(default(TEntity));
    }

    public static Task<TEntity?> SingleOrDefaultAsync<TEntity>(
        this IQueryable<TEntity> source,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(default(TEntity));
    }
}
