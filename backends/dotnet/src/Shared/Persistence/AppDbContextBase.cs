using Microsoft.EntityFrameworkCore;

namespace StackBraid.Shared.Persistence;

/// <summary>
/// The provider-agnostic base every feature's <c>DbContext</c> derives from
/// — a template-method save pipeline, nothing about which database is
/// underneath. It knows nothing about any feature's entity types or marker
/// interfaces on purpose: <c>Domain</c> depends on nothing, so a feature's
/// own audit-timestamp and domain-event shapes live in that feature's
/// <c>Domain</c>, not here. A derived context (e.g. Identity's
/// <c>IdentityDbContext</c>) overrides <see cref="OnBeforeSaveChanges"/> and
/// <see cref="OnAfterSaveChangesAsync"/> to plug its own feature-local
/// shapes into this same save pipeline.
/// </summary>
public abstract class AppDbContextBase : DbContext, IUnitOfWork
{
    protected AppDbContextBase(DbContextOptions options)
        : base(options)
    {
    }

    public sealed override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        OnBeforeSaveChanges();
        var result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await OnAfterSaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    /// <summary>Called just before the write hits the database — a derived context stamps audit timestamps here.</summary>
    protected virtual void OnBeforeSaveChanges()
    {
    }

    /// <summary>
    /// Called once the write has committed — a derived context flushes and
    /// publishes queued domain events here, never before the commit, so a
    /// subscriber never reacts to a change that was then rolled back.
    /// </summary>
    protected virtual Task OnAfterSaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
