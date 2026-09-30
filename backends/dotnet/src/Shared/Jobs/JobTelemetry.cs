using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace StackBraid.Shared.Jobs;

/// <summary>
/// The one place background jobs emit traces and metrics. The worker starts
/// an activity per attempt with the enqueuing request's own trace context as
/// its remote parent, so a job's span and its originating request share one
/// trace, and exposes the correlation ID that request already carried. Metric
/// instrument names here are the ones the checked-in Grafana dashboard
/// queries — see <c>infra/grafana/dashboards/background-jobs.json</c>.
/// </summary>
public static class JobTelemetry
{
    public const string SourceName = "StackBraid.Jobs";
    public const string MeterName = "StackBraid.Jobs";

    public const string CorrelationIdTag = "app.correlation_id";
    public const string JobTypeTag = "job.type";
    public const string JobOutcomeTag = "job.outcome";
    public const string FailureReasonTag = "job.reason";

    public const string SucceededOutcome = "succeeded";
    public const string RetryingOutcome = "retrying";
    public const string DeadLetteredOutcome = "dead-lettered";

    public const string HandlerErrorReason = "handler-error";
    public const string NoHandlerReason = "no-handler";

    public static readonly ActivitySource ActivitySource = new(SourceName);
    public static readonly Meter Meter = new(MeterName);

    /// <summary>Wall-clock seconds a single job attempt took, tagged by job type and outcome.</summary>
    public static readonly Histogram<double> Duration = Meter.CreateHistogram<double>(
        "stackbraid.jobs.duration",
        unit: "s",
        description: "Time a background job attempt took to run.");

    /// <summary>Failed job attempts, tagged by job type and reason (a handler error, or no registered handler).</summary>
    public static readonly Counter<long> Failures = Meter.CreateCounter<long>(
        "stackbraid.jobs.failures",
        description: "Background job attempts that failed.");

    /// <summary>The correlation ID stamped on the current request's span, if one is active.</summary>
    public static string? CurrentCorrelationId() => Activity.Current?.GetTagItem(CorrelationIdTag) as string;

    /// <summary>The W3C <c>traceparent</c> of the current activity, or null when none is active.</summary>
    public static string? CurrentTraceParent() => Activity.Current?.Id;

    /// <summary>The W3C <c>tracestate</c> of the current activity, or null when none is active.</summary>
    public static string? CurrentTraceState() => Activity.Current?.TraceStateString;

    /// <summary>Starts the span for one attempt, parented to the enqueuing request's trace when it was captured.</summary>
    public static Activity? StartAttempt(JobRecord job)
    {
        var parent = default(ActivityContext);
        var hasParent = !string.IsNullOrWhiteSpace(job.TraceParent)
            && ActivityContext.TryParse(job.TraceParent, job.TraceState, out parent);

        var activity = ActivitySource.StartActivity(
            $"job.execute {job.Type}", ActivityKind.Consumer, hasParent ? parent : default);

        activity?.SetTag(JobTypeTag, job.Type);
        if (!string.IsNullOrEmpty(job.CorrelationId))
        {
            activity?.SetTag(CorrelationIdTag, job.CorrelationId);
        }

        return activity;
    }

    public static void RecordDuration(JobRecord job, string outcome, double seconds) =>
        Duration.Record(seconds, new TagList
        {
            { JobTypeTag, job.Type },
            { JobOutcomeTag, outcome },
        });

    public static void RecordFailure(JobRecord job, string reason) =>
        Failures.Add(1, new TagList
        {
            { JobTypeTag, job.Type },
            { FailureReasonTag, reason },
        });
}
