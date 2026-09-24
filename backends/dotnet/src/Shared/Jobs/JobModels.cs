namespace StackBraid.Shared.Jobs;

/// <summary>
/// The lifecycle states a persisted job moves through. Kept as plain string
/// values (not an enum) so the same names are what crosses the wire in the
/// status endpoint and what a Postgres row stores — no mapping layer to get
/// out of step between the two backends.
/// </summary>
public static class JobStates
{
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string DeadLettered = "dead-lettered";
}

/// <summary>
/// A durable unit of work, described in a form that survives a restart: a
/// handler type name plus a JSON payload (never a delegate, which cannot be
/// persisted). <see cref="OwnerId"/> is the caller who started the job, so a
/// status query can be scoped to them.
/// </summary>
public sealed record JobRequest(string Type, string PayloadJson = "{}", string? OwnerId = null, int MaxAttempts = 3);

/// <summary>The persisted row. Mutable because a worker claims it, runs it, and writes the outcome back.</summary>
public sealed class JobRecord
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string? OwnerId { get; set; }
    public string State { get; set; } = JobStates.Queued;
    public int Attempts { get; set; }
    public int MaxAttempts { get; set; } = 3;
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }

    public JobRecord Clone() => (JobRecord)MemberwiseClone();

    public JobStatus ToStatus() =>
        new(Id, Type, State, Attempts, MaxAttempts, LastError, CreatedAt, UpdatedAt, OwnerId);
}

/// <summary>The public view of a job — everything a status query returns, including the owning caller for the access check.</summary>
public sealed record JobStatus(
    Guid Id,
    string Type,
    string Status,
    int Attempts,
    int MaxAttempts,
    string? LastError,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? OwnerId);

/// <summary>The status endpoint's response shape — the same as <see cref="JobStatus"/> without the owner (which is an access-control detail, not a caller-facing one).</summary>
public sealed record JobStatusResponse(
    Guid Id,
    string Type,
    string Status,
    int Attempts,
    int MaxAttempts,
    string? LastError,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static JobStatusResponse From(JobStatus status) =>
        new(status.Id, status.Type, status.Status, status.Attempts, status.MaxAttempts, status.LastError, status.CreatedAt, status.UpdatedAt);
}
