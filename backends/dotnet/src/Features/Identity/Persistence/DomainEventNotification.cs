using Mediator;
using StackBraid.Features.Identity.Domain.Common;

namespace StackBraid.Features.Identity.Persistence;

/// <summary>
/// Adapts a plain <see cref="IDomainEvent"/> — which carries no framework
/// dependency, so <c>Domain</c> stays free of one — into the notification
/// shape the dispatcher understands. A handler subscribes to
/// <c>INotificationHandler&lt;DomainEventNotification&lt;TEvent&gt;&gt;</c>,
/// never to the event type directly.
/// </summary>
public sealed record DomainEventNotification<TEvent>(TEvent DomainEvent) : INotification
    where TEvent : IDomainEvent;
