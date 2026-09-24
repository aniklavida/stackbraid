using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace StackBraid.Shared.Jobs;

/// <summary>What a running handler knows about the job it is executing.</summary>
public sealed record JobContext(Guid JobId, string JobType, string? OwnerId, int Attempt);

/// <summary>
/// The job's serializable payload, deserialized on demand. A handler is
/// expected to be idempotent: the same job can be attempted more than once
/// (a retry, or a crash after the work finished but before the outcome was
/// recorded), so <see cref="JobContext.JobId"/> is the natural idempotency
/// key for work that must not happen twice.
/// </summary>
public sealed class JobPayload
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public JobPayload(string json) => Json = json;

    public string Json { get; }

    public T? Deserialize<T>() => JsonSerializer.Deserialize<T>(Json, Options);
}

/// <summary>One unit of background work, resolved by <see cref="JobType"/> from DI.</summary>
public interface IJobHandler
{
    string JobType { get; }

    Task HandleAsync(JobContext context, JobPayload payload, CancellationToken cancellationToken = default);
}

/// <summary>Maps a persisted job's type name back to the handler that runs it.</summary>
public interface IJobHandlerRegistry
{
    IJobHandler? Resolve(string jobType);
}

public sealed class JobHandlerRegistry : IJobHandlerRegistry
{
    private readonly IReadOnlyDictionary<string, IJobHandler> _handlers;

    public JobHandlerRegistry(IEnumerable<IJobHandler> handlers)
    {
        _handlers = handlers.GroupBy(handler => handler.JobType, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
    }

    public IJobHandler? Resolve(string jobType) =>
        _handlers.TryGetValue(jobType, out var handler) ? handler : null;
}
