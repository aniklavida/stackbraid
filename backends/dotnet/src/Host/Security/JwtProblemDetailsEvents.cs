using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using StackBraid.Shared.Localization;
using StackBraid.Shared.Web;

namespace StackBraid.Host.Security;

/// <summary>
/// ASP.NET Core's JWT bearer handler answers a missing, malformed, expired
/// or otherwise rejected token with a bare 401/403 and no body — its
/// default challenge/forbid behaviour predates this contract. Every
/// non-2xx response must be the same Problem envelope
/// (<c>application/problem+json</c>) everything else in this backend
/// returns, so both events are intercepted and rewritten here instead of
/// left to that default.
/// </summary>
public static class JwtProblemDetailsEvents
{
    public static async Task OnChallengeAsync(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        var error = AppError.Unauthorized("IDENTITY.UNAUTHORIZED", "identity.unauthorized");
        await WriteProblemAsync(context.HttpContext, error);
    }

    public static async Task OnForbiddenAsync(ForbiddenContext context)
    {
        var error = AppError.Forbidden("IDENTITY.FORBIDDEN", "identity.forbidden");
        await WriteProblemAsync(context.HttpContext, error);
    }

    private static async Task WriteProblemAsync(HttpContext httpContext, AppError error)
    {
        var localizer = httpContext.RequestServices.GetRequiredService<IAppLocalizer>();
        var culture = httpContext.Request.Headers.AcceptLanguage.ToString();
        var traceId = httpContext.GetCorrelationId();
        var problem = ProblemDetailsMapper.Map(error, localizer, culture, traceId, httpContext.Request.Path);
        await ProblemDetailsMapper.WriteAsync(httpContext, problem);
    }
}
