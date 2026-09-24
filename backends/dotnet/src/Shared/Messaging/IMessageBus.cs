namespace StackBraid.Shared.Messaging;

/// <summary>
/// A message as it travels across a broker: a stable identity (so a duplicate
/// delivery can be recognised), a routing key (the broker's own word — the
/// exchange/queue binding selects on it), the JSON payload, and how many
/// times delivery has been attempted. This is the shape a RabbitMQ adapter
/// maps onto: routing key to an exchange binding, <see cref="MessageId"/> to
/// the AMQP message id, a nack/requeue to the retry path.
/// </summary>
public sealed record MessageEnvelope(Guid MessageId, string RoutingKey, string PayloadJson, int DeliveryAttempt = 1);

/// <summary>A message the broker gave up on after exhausting its attempt budget, kept so it is not silently lost.</summary>
public sealed record DeadLetter(Guid MessageId, string RoutingKey, string PayloadJson, int Attempts, string Error);

/// <summary>
/// Publish/subscribe over a broker. <see cref="InMemoryMessageBus"/> is the
/// dependency-free fake every test in this repository runs against; a
/// RabbitMQ-backed implementation is a second registration behind this same
/// interface, so no caller changes when it arrives. Nothing here connects to
/// a real broker.
/// </summary>
public interface IMessageBus
{
    Task PublishAsync(string routingKey, string payloadJson, CancellationToken cancellationToken = default);

    Task PublishAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers <paramref name="handler"/> for <paramref name="routingKey"/>.
    /// When <paramref name="idempotent"/> is true the bus delivers a given
    /// <see cref="MessageEnvelope.MessageId"/> to that handler at most once,
    /// which is what makes redelivery safe.
    /// </summary>
    IDisposable Subscribe(string routingKey, Func<MessageEnvelope, CancellationToken, Task> handler, bool idempotent = true);

    IReadOnlyCollection<DeadLetter> DeadLetters { get; }

    /// <summary>The backoff intervals the last delivery run waited between attempts, in order.</summary>
    IReadOnlyList<TimeSpan> RecordedRetryDelays { get; }
}
