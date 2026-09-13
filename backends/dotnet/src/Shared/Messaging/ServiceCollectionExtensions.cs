using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;

namespace StackBraid.Shared.Messaging;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInProcessMessaging(this IServiceCollection services)
    {
        services.AddSingleton(Channel.CreateUnbounded<object>());
        services.AddSingleton<IMessagePublisher, InProcessMessagePublisher>();
        services.AddHostedService<InProcessMessageDeliveryService>();
        return services;
    }
}
