using Mediator;
using Microsoft.EntityFrameworkCore;

namespace StackBraid.Shared.Persistence;

/// <summary>
/// The provider-agnostic base every feature's <c>DbContext</c> derives from.
/// It knows how to stamp audit timestamps and flush queued domain events —
/// nothing about which database is underneath. A provider is chosen only
/// where <c>Database/&lt;Provider&gt;</c> calls <c>UseNpgsql</c> (or the
/// equivalent) on the same <see cref="DbContextOptions"/> this type accepts.
/// </summary>
public abstract class AppDbContextBase : DbContext, IUnitOfWork
{
    private readonly IPublisher? _publisher;

    protected AppDbContextBase(DbContextOptions options, IPublisher? publisher = null)
        : base(options)
    {
        _publisher = publisher;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAuditableEntities();
        var domainEvents = CollectDomainEvents();

        var result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Events are published only once the write actually committed — a
        // subscriber must never react to a change that never happened.
        if (_publisher is not null)
        {
            foreach (var @event in domainEvents)
            {
                await PublishAsync(@event, cancellationToken).ConfigureAwait(false);
            }
        }

        return result;
    }

    private void StampAuditableEntities()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.UpdatedAtUtc = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
            }
        }
    }

    private List<IDomainEvent> CollectDomainEvents()
    {
        var entities = ChangeTracker.Entries<IHasDomainEvents>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        var events = entities.SelectMany(e => e.DomainEvents).ToList();
        foreach (var entity in entities)
        {
            entity.ClearDomainEvents();
        }

        return events;
    }

    // Reflection is confined to this one adapter call so the rest of the
    // context stays ordinary EF Core; it exists only to close over the
    // domain event's concrete type when constructing the generic
    // notification wrapper.
    private async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
        var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;
        await _publisher!.Publish(notification, cancellationToken).ConfigureAwait(false);
    }
}
