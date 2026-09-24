using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackBraid.Shared.Jobs;

namespace StackBraid.Shared.Messaging;

/// <summary>
/// The fake broker every test in this repository uses: a publish/subscribe
/// bus with the exact semantics a real broker must provide — at-least-once
/// delivery, per-handler retry with exponential backoff, a dead-letter path
/// once the attempt budget is spent, and idempotent handlers so a redelivery
/// of the same message is not processed twice. It is deliberately
/// deterministic: <see cref="DeliverPendingAsync"/> drains the queue inline
/// and <see cref="RecordedRetryDelays"/> records the intervals it would have
/// waited, so a test can assert the backoff sequence without sleeping.
/// </summary>
public sealed class InMemoryMessageBus : IMessageBus
{
    private sealed record Subscriber(Guid Id, Func<MessageEnvelope, CancellationToken, Task> Handler, bool Idempotent);

    private readonly MessageBusOptions _options;
    private readonly ILogger<InMemoryMessageBus> _logger;
    private readonly ConcurrentDictionary<string, List<Subscriber>> _subscribers = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<MessageEnvelope> _pending = new();
    private readonly ConcurrentDictionary<string, byte> _processed = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<DeadLetter> _deadLetters = new();
    private readonly ConcurrentQueue<TimeSpan> _retryDelays = new();

    public InMemoryMessageBus(IOptions<MessageBusOptions> options, ILogger<InMemoryMessageBus> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Overridable wait between retries. Tests replace it with a no-op so a run never actually sleeps.</summary>
    public Func<TimeSpan, CancellationToken, Task>? DelayOverride { get; set; }

    public IReadOnlyCollection<DeadLetter> DeadLetters => _deadLetters.ToArray();

    public IReadOnlyList<TimeSpan> RecordedRetryDelays => _retryDelays.ToArray();

    public Task PublishAsync(string routingKey, string payloadJson, CancellationToken cancellationToken = default) =>
        PublishAsync(new MessageEnvelope(Guid.NewGuid(), routingKey, payloadJson), cancellationToken);

    public Task PublishAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default)
    {
        _pending.Enqueue(envelope);
        return Task.CompletedTask;
    }

    public IDisposable Subscribe(string routingKey, Func<MessageEnvelope, CancellationToken, Task> handler, bool idempotent = true)
    {
        var subscriber = new Subscriber(Guid.NewGuid(), handler, idempotent);
        var list = _subscribers.GetOrAdd(routingKey, _ => []);
        lock (list)
        {
            list.Add(subscriber);
        }

        return new Subscription(() =>
        {
            lock (list)
            {
                list.RemoveAll(candidate => candidate.Id == subscriber.Id);
            }
        });
    }

    /// <summary>Delivers every pending message, retrying failed handlers, and returns how many deliveries were attempted.</summary>
    public async Task<int> DeliverPendingAsync(CancellationToken cancellationToken = default)
    {
        var deliveries = 0;
        while (_pending.TryDequeue(out var envelope))
        {
            if (!_subscribers.TryGetValue(envelope.RoutingKey, out var list))
            {
                DeadLetter(envelope, 0, $"No subscriber is registered for routing key '{envelope.RoutingKey}'.");
                continue;
            }

            Subscriber[] snapshot;
            lock (list)
            {
                if (list.Count == 0)
                {
                    DeadLetter(envelope, 0, $"No subscriber is registered for routing key '{envelope.RoutingKey}'.");
                    continue;
                }

                snapshot = list.ToArray();
            }

            foreach (var subscriber in snapshot)
            {
                await DeliverAsync(envelope, subscriber, cancellationToken).ConfigureAwait(false);
                deliveries++;
            }
        }

        return deliveries;
    }

    private async Task DeliverAsync(MessageEnvelope envelope, Subscriber subscriber, CancellationToken cancellationToken)
    {
        var dedupeKey = $"{envelope.RoutingKey}:{envelope.MessageId}";
        if (subscriber.Idempotent && _processed.ContainsKey(dedupeKey))
        {
            _logger.LogDebug("Skipping already-processed message {MessageId} on {RoutingKey}.", envelope.MessageId, envelope.RoutingKey);
            return;
        }

        var attempt = envelope.DeliveryAttempt < 1 ? 1 : envelope.DeliveryAttempt;
        while (true)
        {
            try
            {
                await subscriber.Handler(envelope with { DeliveryAttempt = attempt }, cancellationToken).ConfigureAwait(false);
                if (subscriber.Idempotent)
                {
                    _processed[dedupeKey] = 1;
                }

                return;
            }
            catch (Exception ex)
            {
                if (attempt >= _options.MaxAttempts)
                {
                    DeadLetter(envelope, attempt, ex.Message);
                    return;
                }

                var delay = BackoffPolicy.Compute(
                    attempt,
                    TimeSpan.FromMilliseconds(_options.BaseRetryDelayMilliseconds),
                    TimeSpan.FromMilliseconds(_options.MaxRetryDelayMilliseconds));
                _retryDelays.Enqueue(delay);
                _logger.LogWarning(
                    "Message {MessageId} on {RoutingKey} failed on attempt {Attempt}; retrying in {Delay}: {Error}",
                    envelope.MessageId, envelope.RoutingKey, attempt, delay, ex.Message);

                if (DelayOverride is not null)
                {
                    await DelayOverride(delay, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }

                attempt++;
            }
        }
    }

    private void DeadLetter(MessageEnvelope envelope, int attempts, string error)
    {
        _deadLetters.Enqueue(new DeadLetter(envelope.MessageId, envelope.RoutingKey, envelope.PayloadJson, attempts, error));
        _logger.LogError(
            "Message {MessageId} on {RoutingKey} was dead-lettered after {Attempts} attempt(s): {Error}",
            envelope.MessageId, envelope.RoutingKey, attempts, error);
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                unsubscribe();
            }
        }
    }
}
