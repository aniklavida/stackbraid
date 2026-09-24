using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace StackBraid.Shared.Jobs;

public sealed record ConformanceScenarioRequest(string Scenario);

/// <summary>
/// The status query a job's owner can call, plus the optional conformance
/// surface. Status is owner-scoped: a caller only ever sees a job it started.
/// </summary>
public static class JobEndpoints
{
    public static IEndpointRouteBuilder MapJobEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/jobs").WithTags("Jobs");
        group.MapGet("/{jobId:guid}", GetStatusAsync).RequireAuthorization();

        var options = app.ServiceProvider.GetService<IOptions<JobWorkerOptions>>()?.Value;
        if (options?.ConformanceEnabled == true)
        {
            group.MapPost("/scenarios", EnqueueScenarioAsync).RequireAuthorization();
        }

        return app;
    }

    private static async Task<IResult> GetStatusAsync(
        Guid jobId,
        ClaimsPrincipal user,
        IJobScheduler scheduler,
        CancellationToken cancellationToken)
    {
        var owner = CurrentUserId(user);
        var status = await scheduler.GetStatusAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (status is null || owner is null || !string.Equals(status.OwnerId, owner, StringComparison.Ordinal))
        {
            return Results.NotFound();
        }

        return Results.Ok(JobStatusResponse.From(status));
    }

    private static async Task<IResult> EnqueueScenarioAsync(
        ConformanceScenarioRequest request,
        ClaimsPrincipal user,
        IJobScheduler scheduler,
        CancellationToken cancellationToken)
    {
        var (type, maxAttempts) = request.Scenario switch
        {
            "succeeds" => (ConformanceSucceedsJobHandler.Type, 3),
            "retries" => (ConformanceRetriesJobHandler.Type, 3),
            "dead-letter" => (ConformanceDeadLetterJobHandler.Type, 2),
            _ => ((string?)null, 0),
        };

        if (type is null)
        {
            return Results.BadRequest(new { error = $"Unknown conformance scenario '{request.Scenario}'." });
        }

        var jobId = await scheduler.EnqueueAsync(new JobRequest(type, "{}", CurrentUserId(user), maxAttempts), cancellationToken).ConfigureAwait(false);
        return Results.Accepted($"/v1/jobs/{jobId}", new { jobId });
    }

    private static string? CurrentUserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
}
