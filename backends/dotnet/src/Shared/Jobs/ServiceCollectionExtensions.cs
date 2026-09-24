using System.Threading.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace StackBraid.Shared.Jobs;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-process scheduler only — real asynchronous execution
    /// with no durability, the right fit for tests and for work whose loss on
    /// restart is acceptable.
    /// </summary>
    public static IServiceCollection AddInProcessJobs(this IServiceCollection services)
    {
        services.AddSingleton(Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>());
        services.AddSingleton<InProcessJobScheduler>();
        services.AddSingleton<IJobScheduler>(sp => sp.GetRequiredService<InProcessJobScheduler>());
        services.AddHostedService<JobQueueHostedService>();
        return services;
    }

    /// <summary>
    /// Registers the persisted scheduler as the default <see cref="IJobScheduler"/>.
    /// The store defaults to in-memory here; <c>Database/Postgres</c> replaces
    /// it with the Postgres-backed store when it is registered, which is what
    /// makes a queued job survive a restart in a real deployment.
    /// </summary>
    public static IServiceCollection AddPersistentJobs(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddInProcessJobs();
        services.AddSingleton<IJobStore, InMemoryJobStore>();
        services.AddSingleton<IJobHandlerRegistry, JobHandlerRegistry>();
        services.TryAddSingleton(TimeProvider.System);

        if (configuration is not null)
        {
            services.Configure<JobWorkerOptions>(configuration.GetSection(JobWorkerOptions.SectionName));
        }
        else
        {
            services.AddOptions<JobWorkerOptions>();
        }

        var conformanceEnabled = configuration?.GetValue<bool>(
            $"{JobWorkerOptions.SectionName}:{nameof(JobWorkerOptions.ConformanceEnabled)}") ?? false;
        if (conformanceEnabled)
        {
            services.AddSingleton<IJobHandler, ConformanceSucceedsJobHandler>();
            services.AddSingleton<IJobHandler, ConformanceRetriesJobHandler>();
            services.AddSingleton<IJobHandler, ConformanceDeadLetterJobHandler>();
        }

        services.AddSingleton<JobWorker>();
        services.AddSingleton<PersistentJobScheduler>();
        services.AddSingleton<IJobScheduler>(sp => sp.GetRequiredService<PersistentJobScheduler>());
        services.AddHostedService<JobWorkerHostedService>();
        return services;
    }
}
