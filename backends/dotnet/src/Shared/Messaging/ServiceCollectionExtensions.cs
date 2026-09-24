using System.Threading.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace StackBraid.Shared.Messaging;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInProcessMessaging(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddSingleton(Channel.CreateUnbounded<object>());
        services.AddSingleton<IMessagePublisher, InProcessMessagePublisher>();
        services.AddHostedService<InProcessMessageDeliveryService>();

        if (configuration is not null)
        {
            services.Configure<MessageBusOptions>(configuration.GetSection(MessageBusOptions.SectionName));
            services.Configure<RabbitMqMessageBusOptions>(configuration.GetSection(RabbitMqMessageBusOptions.SectionName));
        }
        else
        {
            services.AddOptions<MessageBusOptions>();
            services.AddOptions<RabbitMqMessageBusOptions>();
        }

        services.AddSingleton<IMessageBus, InMemoryMessageBus>();
        return services;
    }

    /// <summary>
    /// Reserves the RabbitMQ registration point. There is deliberately no
    /// RabbitMQ transport compiled into this build — this method exists so
    /// selecting it fails loudly with the manual steps rather than silently
    /// falling back to the fake broker.
    /// </summary>
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services) =>
        throw new NotSupportedException(
            "The RabbitMQ message transport is not wired in this build. To verify it: install and start a " +
            "broker (e.g. `brew install rabbitmq && brew services start rabbitmq`, or run the rabbitmq:3-management " +
            "image), point Messaging:RabbitMq at it, add the RabbitMQ client package and implement IMessageBus " +
            "against a durable queue bound to Messaging:RabbitMq:Exchange, then set Messaging:Provider=RabbitMq. " +
            "Until then, InMemoryMessageBus is the only implementation.");
}
