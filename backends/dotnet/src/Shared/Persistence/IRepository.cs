namespace StackBraid.Shared.Persistence;

/// <summary>
/// The read/write shape every feature repository starts from. Deliberately
/// narrow and free of any query-provider type (no <c>IQueryable</c> leaking
/// through) — a feature adds its own specific lookups on top of this, and
/// nothing here assumes a relational store, which is what keeps a future
/// MongoDB repository able to implement the same contract. Not constrained
/// to any base entity type: <c>Domain</c> depends on nothing, so this
/// abstraction never forces a feature's entities to inherit from a type
/// defined outside their own project.
/// </summary>
public interface IRepository<TEntity, in TId>
    where TEntity : class
    where TId : notnull
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Remove(TEntity entity);
}
