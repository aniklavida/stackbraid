using Microsoft.EntityFrameworkCore;

namespace StackBraid.Shared.Persistence;

/// <summary>
/// The common EF Core plumbing behind <see cref="IRepository{TEntity,TId}"/>.
/// Uses <see cref="DbSet{TEntity}.FindAsync(object[], CancellationToken)"/>
/// to look up by primary key via EF's own model metadata — no lambda over
/// an <c>Id</c> property is needed, so <typeparamref name="TEntity"/> is
/// never required to inherit a shared base class or implement a shared
/// interface. A feature's entity stays exactly what that feature's
/// <c>Domain</c> defines. A feature's repository inherits this for the
/// shared verbs and adds its own specific lookups (e.g.
/// <c>GetByEmailAsync</c>) directly against <see cref="Set"/>.
/// </summary>
public abstract class RepositoryBase<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    protected readonly DbContext Context;
    protected DbSet<TEntity> Set => Context.Set<TEntity>();

    protected RepositoryBase(DbContext context)
    {
        Context = context;
    }

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default) =>
        await Set.FindAsync([id], cancellationToken).ConfigureAwait(false);

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await Set.AddAsync(entity, cancellationToken).ConfigureAwait(false);

    public virtual void Remove(TEntity entity) => Set.Remove(entity);
}
