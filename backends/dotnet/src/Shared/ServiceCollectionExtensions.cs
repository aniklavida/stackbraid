using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackBraid.Shared.Behaviors;
using StackBraid.Shared.Caching;
using StackBraid.Shared.Documents;
using StackBraid.Shared.Jobs;
using StackBraid.Shared.Mailing;
using StackBraid.Shared.Messaging;
using StackBraid.Shared.Notifications;
using StackBraid.Shared.Security;
using StackBraid.Shared.Storage;
using StackBraid.Shared.Web;

namespace StackBraid.Shared;

/// <summary>
/// Registers every cross-cutting concern that has swappable implementations
/// and no provider-specific database wiring.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddShared(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddSharedWeb();
        services.AddSharedPipelineBehaviors();
        services.AddInProcessMessaging();
        services.AddInProcessJobs();
        services.AddMemoryCache();
        services.AddSingleton<ICache, InMemoryCache>();
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        services.AddFileStorage(configuration);
        services.AddDocuments();
        services.AddNotifications(configuration);

        return services;
    }

    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddOptions<LocalFileStorageOptions>();
        services.AddOptions<S3FileStorageOptions>();

        var provider = configuration?["Storage:Provider"];
        if (string.Equals(provider, "S3", StringComparison.OrdinalIgnoreCase))
        {
            if (configuration is not null)
            {
                services.Configure<S3FileStorageOptions>(configuration.GetSection(S3FileStorageOptions.SectionName));
            }

            services.AddSingleton<IFileStorage, S3FileStorage>();
        }
        else
        {
            if (configuration is not null)
            {
                services.Configure<LocalFileStorageOptions>(configuration.GetSection("Storage:Local"));
            }

            services.AddSingleton<IFileStorage, LocalFileStorage>();
        }

        // Register both concrete implementations so callers can resolve either directly if needed
        services.AddSingleton<LocalFileStorage>();
        services.AddSingleton<S3FileStorage>();

        return services;
    }

    public static IServiceCollection AddDocuments(this IServiceCollection services)
    {
        services.AddSingleton<IExcelExporter, ClosedXmlExcelExporter>();
        services.AddSingleton<IExcelImporter, ClosedXmlExcelImporter>();
        services.AddSingleton<IPdfGenerator, QuestPdfGenerator>();

        // Also register minimal implementations so callers outside the free tier or
        // preferring zero-dependency CSV/PDF can resolve or swap them
        services.AddSingleton<CsvExcelExporter>();
        services.AddSingleton<MinimalPdfGenerator>();

        return services;
    }
}
