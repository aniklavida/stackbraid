using Microsoft.EntityFrameworkCore;

namespace StackBraid.Shared.Persistence;

/// <summary>
/// The common EF Core plumbing behind <see cref="IRepository{TEntity,TId}"/>.
/// A feature's repository inherits this for the shared verbs and adds its
/// own specific lookups (e.g. <c>GetByEmailAsync</c>) directly against
/// <see cref="Set"/> — still without referencing any provider package.
/// </summary>
public abstract class RepositoryBase<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    protected readonly DbContext Context;
    protected DbSet<TEntity> Set => Context.Set<TEntity>();

    protected RepositoryBase(DbContext context)
    {
        Context = context;
    }

    public virtual Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await Set.AddAsync(entity, cancellationToken).ConfigureAwait(false);

    public virtual void Remove(TEntity entity) => Set.Remove(entity);
}
