using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using StackBraid.Shared.Localization;

namespace StackBraid.Shared.Web;

/// <summary>
/// Wires every cross-cutting <c>Shared/Web</c> concern into one call from
/// <c>Host</c> — the global exception handler, the localizer, and rate
/// limiting. Kept as one extension so a feature never has to remember the
/// individual pieces or their order.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedWeb(this IServiceCollection services)
    {
        services.AddSingleton<IAppLocalizer, JsonAppLocalizer>();
        services.AddHttpContextAccessor();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddAppRateLimiting();
        return services;
    }

    /// <summary>Call early in the pipeline — correlation ID and exception handling wrap everything after them.</summary>
    public static IApplicationBuilder UseSharedWeb(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseExceptionHandler();
        app.UseMiddleware<RateLimitMiddleware>();
        return app;
    }
}
