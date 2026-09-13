using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace StackBraid.Shared.Web;

/// <summary>
/// Every request gets one correlation ID — read from an inbound
/// <c>X-Correlation-Id</c> header when a caller (or a gateway in front of
/// this service) already set one, otherwise minted here. It becomes the
/// contract's <c>Problem.traceId</c> on any error response and is pushed
/// into the structured-logging scope, so a bug report naming a
/// <c>traceId</c> can be grepped straight out of the logs.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string HttpContextItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var header) && !string.IsNullOrWhiteSpace(header)
            ? header.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items[HttpContextItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (context.RequestServices.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()
                   ?.CreateLogger("CorrelationId")
                   ?.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context).ConfigureAwait(false);
        }
    }
}

public static class HttpContextCorrelationIdExtensions
{
    /// <summary>The correlation ID <see cref="CorrelationIdMiddleware"/> attached to this request.</summary>
    public static string GetCorrelationId(this HttpContext context) =>
        context.Items.TryGetValue(CorrelationIdMiddleware.HttpContextItemKey, out var value) && value is string id
            ? id
            : context.TraceIdentifier;
}
