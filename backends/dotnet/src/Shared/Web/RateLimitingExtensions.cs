using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using StackBraid.Shared.Localization;

namespace StackBraid.Shared.Web;

public interface IRateLimiter
{
    bool TryAcquire(string key);
}

public sealed class InMemoryRateLimiter : IRateLimiter
{
    private const int PermitLimit = 100;
    private readonly ConcurrentDictionary<string, Window> _windows = new();

    public bool TryAcquire(string key)
    {
        var now = DateTimeOffset.UtcNow;
        var window = _windows.AddOrUpdate(
            key,
            _ => new Window(now, 1),
            (_, current) => now - current.StartedAt >= TimeSpan.FromMinutes(1)
                ? new Window(now, 1)
                : current with { Count = current.Count + 1 });
        return window.Count <= PermitLimit;
    }

    private sealed record Window(DateTimeOffset StartedAt, int Count);
}

public sealed class RateLimitMiddleware
{
    private readonly RequestDelegate _next;

    public RateLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRateLimiter limiter, IAppLocalizer localizer)
    {
        if (context.Request.Method == HttpMethods.Post && context.Request.Path.StartsWithSegments("/v1/auth"))
        {
            var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (!limiter.TryAcquire(key))
            {
                var error = AppError.Validation("IDENTITY.RATE_LIMITED", "identity.rate_limited", new Dictionary<string, string[]>())
                    with { Type = AppErrorType.Failure };
                var problem = ProblemDetailsMapper.Map(error, localizer, context.Request.Headers.AcceptLanguage.ToString(), context.GetCorrelationId(), context.Request.Path);
                problem.Status = StatusCodes.Status429TooManyRequests;
                context.Response.Headers.RetryAfter = "60";
                await ProblemDetailsMapper.WriteAsync(context, problem, context.RequestAborted);
                return;
            }
        }

        await _next(context);
    }
}

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";

    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services)
    {
        services.AddSingleton<IRateLimiter, InMemoryRateLimiter>();
        return services;
    }
}
