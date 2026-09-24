using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using StackBraid.Shared.Messaging;

namespace StackBraid.Shared.UnitTests.Messaging;

public sealed class InMemoryMessageBusTests
{
    [Fact]
    public async Task A_handler_that_fails_then_succeeds_is_retried_with_exponential_backoff()
    {
        var attempts = new List<int>();
        var bus = CreateBus(maxAttempts: 4, baseRetryMs: 50);
        bus.Subscribe("orders.created", (envelope, _) =>
        {
            attempts.Add(envelope.DeliveryAttempt);
            return envelope.DeliveryAttempt < 3
                ? Task.FromException(new InvalidOperationException("transient"))
                : Task.CompletedTask;
        });

        await bus.PublishAsync("orders.created", "{\"id\":1}");
        await bus.DeliverPendingAsync();

        attempts.ShouldBe([1, 2, 3]);
        bus.RecordedRetryDelays.ShouldBe([TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(100)]);
        bus.DeadLetters.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_handler_that_always_fails_lands_in_the_dead_letter_path()
    {
        var bus = CreateBus(maxAttempts: 2, baseRetryMs: 50);
        bus.Subscribe("orders.created", (_, _) => Task.FromException(new InvalidOperationException("permanent")));

        await bus.PublishAsync("orders.created", "{\"id\":2}");
        await bus.DeliverPendingAsync();

        bus.RecordedRetryDelays.ShouldBe([TimeSpan.FromMilliseconds(50)]);
        var deadLetter = bus.DeadLetters.ShouldHaveSingleItem();
        deadLetter.RoutingKey.ShouldBe("orders.created");
        deadLetter.Attempts.ShouldBe(2);
        deadLetter.Error.ShouldContain("permanent");
    }

    [Fact]
    public async Task An_idempotent_handler_processes_a_redelivered_message_only_once()
    {
        var processed = new List<Guid>();
        var bus = CreateBus(maxAttempts: 3, baseRetryMs: 10);
        bus.Subscribe("orders.created", (envelope, _) =>
        {
            processed.Add(envelope.MessageId);
            return Task.CompletedTask;
        });

        var messageId = Guid.NewGuid();
        var envelope = new MessageEnvelope(messageId, "orders.created", "{\"id\":3}");
        await bus.PublishAsync(envelope);
        await bus.DeliverPendingAsync();
        await bus.PublishAsync(envelope);
        await bus.DeliverPendingAsync();

        processed.ShouldBe([messageId]);
    }

    [Fact]
    public async Task A_retried_message_is_not_processed_again_when_the_broker_redelivers_it()
    {
        var attempts = 0;
        var bus = CreateBus(maxAttempts: 3, baseRetryMs: 10);
        bus.Subscribe("orders.created", (_, _) =>
        {
            attempts++;
            return attempts == 1
                ? Task.FromException(new InvalidOperationException("transient"))
                : Task.CompletedTask;
        });

        var envelope = new MessageEnvelope(Guid.NewGuid(), "orders.created", "{}");
        await bus.PublishAsync(envelope);
        await bus.DeliverPendingAsync();
        await bus.PublishAsync(envelope);
        await bus.DeliverPendingAsync();

        attempts.ShouldBe(2);
        bus.DeadLetters.ShouldBeEmpty();
    }

    private static InMemoryMessageBus CreateBus(int maxAttempts, int baseRetryMs)
    {
        var bus = new InMemoryMessageBus(
            Options.Create(new MessageBusOptions { MaxAttempts = maxAttempts, BaseRetryDelayMilliseconds = baseRetryMs, MaxRetryDelayMilliseconds = 10_000 }),
            NullLogger<InMemoryMessageBus>.Instance);

        // Never actually sleep — the interval is asserted, not waited out.
        bus.DelayOverride = (_, _) => Task.CompletedTask;
        return bus;
    }
}
