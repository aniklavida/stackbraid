using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using StackBraid.Shared.Localization;

namespace StackBraid.Shared.Web;

/// <summary>
/// The last line of defence: any exception a handler did not turn into a
/// <see cref="Result{T}"/> failure lands here and still comes back as a
/// contract-shaped Problem, never a bare ASP.NET Core stack trace. Expected
/// failures (validation, not-found, ...) should never reach this — they are
/// <see cref="Result{T}"/> values mapped by the endpoint itself.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IAppLocalizer _localizer;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IAppLocalizer localizer, ILogger<GlobalExceptionHandler> logger)
    {
        _localizer = localizer;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = httpContext.GetCorrelationId();
        _logger.LogError(exception, "Unhandled exception. traceId={TraceId}", traceId);

        var culture = httpContext.Request.Headers.AcceptLanguage.ToString();
        var error = AppError.Validation("SYSTEM.UNEXPECTED_ERROR", "problem.server_error.detail", new Dictionary<string, string[]>())
            with { Type = AppErrorType.Failure };

        var problem = ProblemDetailsMapper.Map(error, _localizer, culture, traceId, httpContext.Request.Path);

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken).ConfigureAwait(false);

        return true;
    }
}
