using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using StackBraid.Shared.Localization;

namespace StackBraid.Shared.Web;

/// <summary>
/// The one rate-limit policy every backend needs on day one: authentication
/// endpoints, keyed by client IP, so credential stuffing against
/// <c>/v1/auth/login</c> gets a 429 (see <c>contract/openapi.yaml</c>'s
/// <c>TooManyRequests</c> response) instead of unlimited attempts.
/// A feature opts an endpoint in with <c>.RequireRateLimiting(RateLimitingExtensions.AuthPolicy)</c>.
/// </summary>
public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";

    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                var localizer = context.HttpContext.RequestServices.GetRequiredService<IAppLocalizer>();
                var culture = context.HttpContext.Request.Headers.AcceptLanguage.ToString();
                var traceId = context.HttpContext.GetCorrelationId();
                var error = AppError.Validation("IDENTITY.RATE_LIMITED", "identity.rate_limited", new Dictionary<string, string[]>())
                    with { Type = AppErrorType.Failure };
                var problem = ProblemDetailsMapper.Map(error, localizer, culture, traceId, context.HttpContext.Request.Path);
                problem.Status = StatusCodes.Status429TooManyRequests;

                await ProblemDetailsMapper.WriteAsync(context.HttpContext, problem, cancellationToken).ConfigureAwait(false);
            };

            options.AddPolicy(AuthPolicy, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    // High enough that a legitimate burst — a test suite, or one
                    // user's browser retrying login/refresh/register in quick
                    // succession — never trips it, while still bounding genuine
                    // credential-stuffing volume. The conformance suite alone
                    // issues dozens of auth calls in its own single run.
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));
        });

        return services;
    }
}
