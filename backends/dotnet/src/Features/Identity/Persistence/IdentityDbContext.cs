using Mediator;
using Microsoft.EntityFrameworkCore;
using StackBraid.Features.Identity.Domain.Common;
using StackBraid.Features.Identity.Domain.Entities;
using StackBraid.Shared.Persistence;

namespace StackBraid.Features.Identity.Persistence;

/// <summary>
/// The Identity feature's own <c>DbContext</c> — provider-agnostic: nothing
/// here calls <c>UseNpgsql</c> or names Postgres. A provider chooses how to
/// connect this same context; today that is <c>Database/Postgres</c>. Plugs
/// this feature's own <see cref="IAuditable"/>/<see cref="IHasDomainEvents"/>
/// shapes into <see cref="AppDbContextBase"/>'s generic save pipeline.
/// </summary>
public sealed class IdentityDbContext : AppDbContextBase
{
    private readonly IPublisher? _publisher;

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options, IPublisher? publisher = null)
        : base(options)
    {
        _publisher = publisher;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }

    protected override void OnBeforeSaveChanges()
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

    protected override async Task OnAfterSaveChangesAsync(CancellationToken cancellationToken)
    {
        if (_publisher is null)
        {
            return;
        }

        var entitiesWithEvents = ChangeTracker.Entries<IHasDomainEvents>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        var events = entitiesWithEvents.SelectMany(e => e.DomainEvents).ToList();
        foreach (var entity in entitiesWithEvents)
        {
            entity.ClearDomainEvents();
        }

        foreach (var domainEvent in events)
        {
            await PublishAsync(domainEvent, cancellationToken).ConfigureAwait(false);
        }
    }

    // Reflection is confined to this one adapter call so the rest of the
    // context stays ordinary EF Core; it exists only to close over the
    // domain event's concrete type when constructing the generic
    // notification wrapper (Domain events do not implement Mediator's
    // INotification directly — Domain depends on nothing, Mediator included).
    private Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
        var notification = Activator.CreateInstance(notificationType, domainEvent)!;
        // Publish(object, ...) dispatches on the notification's runtime type — the
        // generic Publish<T> overload would bind T to the compile-time INotification
        // interface here instead, which is not what a source-generated dispatcher keys on.
        return _publisher!.Publish((object)notification, cancellationToken).AsTask();
    }
}
