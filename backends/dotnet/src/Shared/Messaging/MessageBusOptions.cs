namespace StackBraid.Shared.Messaging;

public sealed class MessageBusOptions
{
    public const string SectionName = "Messaging";

    /// <summary>How many times a handler is attempted before the message is dead-lettered. 1 means "no retry".</summary>
    public int MaxAttempts { get; set; } = 3;

    public int BaseRetryDelayMilliseconds { get; set; } = 100;

    public int MaxRetryDelayMilliseconds { get; set; } = 30_000;

    /// <summary>
    /// Which transport to use. "InMemory" (the default) is the fake broker
    /// every test runs against. "RabbitMq" is reserved for a real broker,
    /// which is intentionally not wired in this build — see
    /// <see cref="RabbitMqMessageBusOptions"/>.
    /// </summary>
    public string Provider { get; set; } = "InMemory";
}

/// <summary>
/// The names a real RabbitMQ topology would bind. There is no RabbitMQ
/// transport compiled into this build — selecting it fails loudly rather than
/// pretending to broker anything.
/// </summary>
public sealed class RabbitMqMessageBusOptions
{
    public const string SectionName = "Messaging:RabbitMq";

    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string Exchange { get; set; } = "stackbraid";

    public string DeadLetterExchange { get; set; } = "stackbraid.dead-letter";
}
