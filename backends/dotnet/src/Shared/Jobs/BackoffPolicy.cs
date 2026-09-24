namespace StackBraid.Shared.Jobs;

/// <summary>The knobs every retry path shares — jobs and message consumers both use these exact numbers.</summary>
public sealed class JobWorkerOptions
{
    public const string SectionName = "Jobs";

    public int BatchSize { get; set; } = 10;

    public int PollIntervalMilliseconds { get; set; } = 1000;

    public int BaseRetryDelayMilliseconds { get; set; } = 1000;

    public int MaxRetryDelayMilliseconds { get; set; } = 300_000;

    /// <summary>When false, this process does not run the worker loop — a dedicated worker process does instead.</summary>
    public bool WorkerEnabled { get; set; } = true;

    /// <summary>
    /// Registers the deterministic scenario handlers the conformance suite
    /// drives. Off by default: they exist to prove retry/dead-letter
    /// behaviour, not to be a production surface.
    /// </summary>
    public bool ConformanceEnabled { get; set; }
}

/// <summary>
/// Exponential backoff with a ceiling: attempt 1 waits <c>base</c>, attempt 2
/// <c>2×base</c>, and so on, capped at <c>max</c>. Deliberately
/// deterministic (no jitter) so both backends — and the conformance suite —
/// can assert the exact delay sequence.
/// </summary>
public static class BackoffPolicy
{
    public static TimeSpan Compute(int attempt, TimeSpan baseDelay, TimeSpan maxDelay)
    {
        if (attempt < 1)
        {
            attempt = 1;
        }

        var exponent = Math.Min(attempt - 1, 20);
        var scaled = baseDelay.TotalMilliseconds * Math.Pow(2, exponent);
        var capped = Math.Min(scaled, maxDelay.TotalMilliseconds);
        return TimeSpan.FromMilliseconds(capped);
    }

    public static TimeSpan Compute(int attempt, JobWorkerOptions options) =>
        Compute(attempt, TimeSpan.FromMilliseconds(options.BaseRetryDelayMilliseconds), TimeSpan.FromMilliseconds(options.MaxRetryDelayMilliseconds));
}
