using Microsoft.AspNetCore.Http;
using StackBraid.Shared.Localization;

namespace StackBraid.Shared.Web;

/// <summary>
/// Turns an <see cref="AppError"/> into the same Problem response every
/// endpoint in every feature returns — so an endpoint file only has to
/// decide what a *success* looks like; failure is always this one call.
/// </summary>
public static class ResultHttpExtensions
{
    public static IResult ToProblemResult(this AppError error, HttpContext context, IAppLocalizer localizer)
    {
        var culture = context.Request.Headers.AcceptLanguage.ToString();
        var traceId = context.GetCorrelationId();
        var problem = ProblemDetailsMapper.Map(error, localizer, culture, traceId, context.Request.Path);

        return Results.Problem(
            statusCode: problem.Status,
            type: problem.Type,
            title: problem.Title,
            detail: problem.Detail,
            instance: problem.Instance,
            extensions: problem.Extensions);
    }
}
