using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using StackBraid.Shared.Caching;
using StackBraid.Shared.Documents;
using StackBraid.Shared.Jobs;
using StackBraid.Shared.Mailing;
using StackBraid.Shared.Messaging;
using StackBraid.Shared.Storage;
using StackBraid.Shared.Web;

namespace StackBraid.Shared;

/// <summary>
/// Registers every cross-cutting concern that has exactly one
/// implementation and no provider-specific wiring. Persistence is
/// deliberately not here — a <c>DbContext</c> only exists once a provider
/// (<c>Database/Postgres</c>, at present) has chosen how to connect one.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddShared(this IServiceCollection services)
    {
        services.AddSharedWeb();
        services.AddInProcessMessaging();
        services.AddInProcessJobs();
        services.AddMemoryCache();
        services.AddSingleton<ICache, InMemoryCache>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<IPdfGenerator, MinimalPdfGenerator>();
        services.AddSingleton<IExcelExporter, CsvExcelExporter>();
        return services;
    }
}
