using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace StackBraid.Shared.Messaging;

/// <summary>
/// The default <see cref="IMessagePublisher"/>: an in-process, in-memory
/// queue drained by <see cref="InProcessMessageDeliveryService"/>. Nothing
/// leaves this process and nothing survives a restart — genuinely correct
/// for a single-instance deployment, and an honest placeholder rather than
/// a broker this skeleton doesn't yet run. Swap this registration for a
/// RabbitMQ-backed implementation without touching a single caller.
/// </summary>
public sealed class InProcessMessagePublisher : IMessagePublisher
{
    private readonly Channel<object> _channel;

    public InProcessMessagePublisher(Channel<object> channel)
    {
        _channel = channel;
    }

    public ValueTask PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : notnull =>
        _channel.Writer.WriteAsync(message, cancellationToken);
}

/// <summary>Drains the in-process queue and logs each delivery — the visible half of the stand-in broker.</summary>
public sealed class InProcessMessageDeliveryService : BackgroundService
{
    private readonly Channel<object> _channel;
    private readonly ILogger<InProcessMessageDeliveryService> _logger;

    public InProcessMessageDeliveryService(Channel<object> channel, ILogger<InProcessMessageDeliveryService> logger)
    {
        _channel = channel;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            _logger.LogInformation("Message delivered in-process: {MessageType}", message.GetType().Name);
        }
    }
}
