namespace StackBraid.Shared.Persistence;

/// <summary>
/// Base type for every persisted entity, in every feature. Carries only what
/// every row needs — an identity and equality by that identity — never a
/// business concept. A feature's own base type (if it wants one) lives in
/// that feature's <c>Domain</c>, not here.
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    protected Entity()
    {
    }

    protected Entity(TId id)
    {
        Id = id;
    }

    public bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj) => Equals(obj as Entity<TId>);

    public override int GetHashCode() => (GetType(), Id).GetHashCode();

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}

/// <summary>
/// Marker for entities that track when they were created and last changed.
/// <see cref="AppDbContextBase"/> stamps both fields on every save — a
/// feature never sets them by hand.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAtUtc { get; set; }
    DateTime UpdatedAtUtc { get; set; }
}

/// <summary>
/// Marker for an entity that raises domain events. Events queue up on the
/// entity during a use case and are published, via the configured
/// dispatcher, after <see cref="AppDbContextBase.SaveChangesAsync"/> commits
/// — never before, so a subscriber never observes an event for a change that
/// was then rolled back.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}

/// <summary>
/// A fact that already happened inside the domain. Carries no framework
/// dependency of its own — <c>IDomainEvent</c> is the whole contract, and a
/// feature's own events are plain records implementing it.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredAtUtc { get; }
}
