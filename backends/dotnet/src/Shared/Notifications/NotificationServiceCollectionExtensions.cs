using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace StackBraid.Shared.Notifications;

public static class NotificationServiceCollectionExtensions
{
    public static IServiceCollection AddNotifications(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
        {
            services.Configure<FirebaseOptions>(configuration.GetSection(FirebaseOptions.SectionName));
        }

        services.AddHttpClient<IFirebaseCloudMessagingTransport, FirebaseCloudMessagingTransport>();
        services.AddSingleton<INotificationStore, InMemoryNotificationStore>();
        services.AddSingleton<IDeviceTokenStore, InMemoryDeviceTokenStore>();
        services.AddSingleton<IFirebaseCloudMessagingTransport, FirebaseCloudMessagingTransport>();
        services.AddSingleton<INotificationSender, FirebaseNotificationSender>();
        services.AddSingleton<INotificationSender, EmailNotificationSender>();
        services.AddSingleton<INotificationSender, InAppNotificationSender>();
        services.AddSingleton<NotificationDispatcher>();
        services.AddSingleton<QueuedNotificationJob>();
        return services;
    }
}
