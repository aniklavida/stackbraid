namespace StackBraid.Features.Identity.Domain.Common;

/// <summary>A fact that already happened inside this feature's domain. Carries no framework dependency of its own.</summary>
public interface IDomainEvent
{
    DateTime OccurredAtUtc { get; }
}

/// <summary>
/// Marks an entity that raises domain events. Events queue up during a use
/// case and are published, by <c>IdentityDbContext</c>, only after
/// <c>SaveChangesAsync</c> commits — never before, so a subscriber never
/// observes an event for a change that was then rolled back.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}

/// <summary>Shared plumbing for raising and clearing domain events — an entity opts in by inheriting this alongside <see cref="Entity{TId}"/>.</summary>
public abstract class DomainEventEntity<TId> : Entity<TId>, IHasDomainEvents
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected DomainEventEntity()
    {
    }

    protected DomainEventEntity(TId id)
        : base(id)
    {
    }

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
