namespace StackBraid.Shared.Persistence;

/// <summary>
/// One transaction boundary per use case. A command handler collects every
/// change it needs through one or more repositories, then calls this once —
/// never <c>SaveChanges</c> on a repository directly, which is how two
/// handlers end up committing half of each other's work.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
