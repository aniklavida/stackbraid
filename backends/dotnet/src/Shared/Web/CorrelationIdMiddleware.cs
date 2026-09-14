using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace StackBraid.Shared.Web;

/// <summary>
/// Every request gets one correlation ID — read from an inbound
/// <c>X-Correlation-Id</c> header when a caller (or a gateway in front of
/// this service) already set one, otherwise minted here. It becomes the
/// contract's <c>Problem.traceId</c> on any error response and is pushed
/// into the structured-logging scope, so a bug report naming a
/// <c>traceId</c> can be grepped straight out of the logs.
///
/// It is also stamped onto the current OpenTelemetry <see cref="Activity"/>
/// (the span this request's own instrumentation already created) as
/// <c>app.correlation_id</c>, and the span's own trace/span id are pushed
/// into the same structured-logging context — so a trace exported to a
/// console/file/OTLP backend and a log line on disk can each be found from
/// the other: search logs for the correlation ID to get the trace ID, or
/// open the trace to read the correlation ID back off it.
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

        var activity = Activity.Current;
        activity?.SetTag("app.correlation_id", correlationId);

        using var _ = LogContext.PushProperty("CorrelationId", correlationId);
        using var __ = activity is null ? null : LogContext.PushProperty("TraceId", activity.TraceId.ToString());
        using var ___ = activity is null ? null : LogContext.PushProperty("SpanId", activity.SpanId.ToString());

        var logger = context.RequestServices.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()
            ?.CreateLogger("CorrelationId");

        using (logger?.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            var stopwatch = Stopwatch.StartNew();
            await _next(context).ConfigureAwait(false);
            stopwatch.Stop();

            // One structured line per request, win or lose — this is what
            // makes the correlation ID (and, through the two properties
            // pushed above, the trace/span id) actually findable in the
            // logs at all for a request that otherwise produced no log
            // output of its own. Every value is a template argument, never
            // string-interpolated, so a log sink can index on
            // `RequestMethod`/`RequestPath`/`StatusCode` structurally
            // instead of regex-parsing a sentence.
            logger?.LogInformation(
                "Handled {RequestMethod} {RequestPath} -> {StatusCode} in {ElapsedMilliseconds}ms",
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                stopwatch.Elapsed.TotalMilliseconds);
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
