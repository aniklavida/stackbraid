using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StackBraid.Shared.Localization;

namespace StackBraid.Shared.Web;

/// <summary>
/// Turns an <see cref="AppError"/> into the RFC 9457 envelope
/// <c>contract/openapi.yaml</c>'s <c>Problem</c> schema pins: the required
/// <c>type</c>/<c>title</c>/<c>status</c> core, plus this contract's
/// <c>code</c>/<c>traceId</c>/<c>errors</c> extension. <c>title</c> and
/// <c>detail</c> are localized; <c>code</c> never is — a client branches on
/// <c>code</c>, never on the sentence.
/// </summary>
public static class ProblemDetailsMapper
{
    public static ProblemDetails Map(AppError error, IAppLocalizer localizer, string? culture, string traceId, string instance)
    {
        var status = ToStatusCode(error.Type);
        var problem = new ProblemDetails
        {
            Type = "about:blank",
            Title = localizer.GetString(TitleKey(error.Type), culture),
            Status = status,
            Detail = localizer.GetString(error.MessageKey, culture, error.Arguments),
            Instance = instance,
        };

        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = traceId;

        if (error.Type == AppErrorType.Validation && error.FieldErrors is not null)
        {
            var localizedErrors = error.FieldErrors.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(msgKey => localizer.GetString(msgKey, culture)).ToArray());
            problem.Extensions["errors"] = localizedErrors;
        }

        return problem;
    }

    public static int ToStatusCode(AppErrorType type) => type switch
    {
        AppErrorType.Validation => StatusCodes.Status400BadRequest,
        AppErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        AppErrorType.Forbidden => StatusCodes.Status403Forbidden,
        AppErrorType.NotFound => StatusCodes.Status404NotFound,
        AppErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError,
    };

    /// <summary>
    /// Writes a Problem body with the exact contract-required content type.
    /// <c>HttpResponse.WriteAsJsonAsync</c>'s no-content-type overload always
    /// stamps <c>application/json</c> over whatever was set beforehand — the
    /// conformance suite catches this exact mistake wherever a response is
    /// written outside the normal <c>Results.Problem(...)</c> path (a
    /// middleware-level exception handler or an authentication challenge).
    /// </summary>
    public static Task WriteAsync(HttpContext httpContext, ProblemDetails problem, CancellationToken cancellationToken = default)
    {
        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        return httpContext.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken);
    }

    private static string TitleKey(AppErrorType type) => type switch
    {
        AppErrorType.Validation => "problem.validation_failed.title",
        AppErrorType.Unauthorized => "problem.unauthorized.title",
        AppErrorType.Forbidden => "problem.forbidden.title",
        AppErrorType.NotFound => "problem.not_found.title",
        AppErrorType.Conflict => "problem.conflict.title",
        _ => "problem.server_error.title",
    };
}
