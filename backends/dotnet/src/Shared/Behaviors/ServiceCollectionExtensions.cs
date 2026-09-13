using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace StackBraid.Shared.Behaviors;

public static class ServiceCollectionExtensions
{
    /// <summary>Wraps every command and query dispatch in the shared logging behavior.</summary>
    public static IServiceCollection AddSharedPipelineBehaviors(this IServiceCollection services)
    {
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(LoggingPipelineBehavior<,>));
        return services;
    }
}
