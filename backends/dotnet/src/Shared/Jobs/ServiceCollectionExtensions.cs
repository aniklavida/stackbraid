using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;

namespace StackBraid.Shared.Jobs;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInProcessJobs(this IServiceCollection services)
    {
        services.AddSingleton(Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>());
        services.AddSingleton<IJobScheduler, InProcessJobScheduler>();
        services.AddHostedService<JobQueueHostedService>();
        return services;
    }
}
