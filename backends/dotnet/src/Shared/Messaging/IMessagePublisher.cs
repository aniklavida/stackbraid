namespace StackBraid.Shared.Messaging;

/// <summary>
/// Publishes an integration message for anything outside the current
/// process to react to — the cross-process counterpart to an in-process
/// domain event. One implementation ships today,
/// <see cref="InProcessMessagePublisher"/>: everything queues and is
/// delivered inside this same process, logged rather than routed anywhere.
/// A real broker (RabbitMQ, per the product's architecture) is a second
/// implementation behind this same interface — no caller changes when it
/// arrives, which is the entire point of depending on the interface.
/// </summary>
public interface IMessagePublisher
{
    ValueTask PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : notnull;
}
