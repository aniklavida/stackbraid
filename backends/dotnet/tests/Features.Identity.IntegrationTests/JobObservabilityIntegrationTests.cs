using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using StackBraid.Features.Identity.Contracts.Requests;
using StackBraid.Shared.Jobs;

namespace StackBraid.Features.Identity.IntegrationTests;

/// <summary>
/// The correlation ID and trace context a request already carries must survive
/// the jump into background work: a job enqueued over real HTTP is stored with
/// that request's correlation ID and W3C trace context, and the worker's own
/// span and log line carry both — the worker's span joins the originating
/// request's trace, and its log line names the request's correlation ID.
///
/// Driven against the real running pipeline (real Postgres, real JWT
/// issuance, real OpenTelemetry instrumentation, real Serilog output) with two
/// additive capture points: everything written to the process's console, and
/// process-wide <see cref="ActivityListener"/> / <see cref="MeterListener"/>
/// subscribers recording every span and metric directly.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class JobObservabilityIntegrationTests : IDisposable
{
    private readonly List<Activity> _capturedActivities = [];
    private readonly List<Measurement> _capturedMeasurements = [];
    private readonly ActivityListener _activityListener;
    private readonly MeterListener _meterListener;
    private readonly WebApplicationFactory<Program> _factory;

    public JobObservabilityIntegrationTests(PostgresDatabaseFixture postgres)
    {
        _activityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                lock (_capturedActivities)
                {
                    _capturedActivities.Add(activity);
                }
            },
        };
        ActivitySource.AddActivityListener(_activityListener);

        _meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == JobTelemetry.MeterName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };
        _meterListener.SetMeasurementEventCallback<double>(RecordMeasurement);
        _meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            RecordMeasurement(instrument, value, tags, _));
        _meterListener.Start();

        var connectionString = Environment.GetEnvironmentVariable("STACKBRAID_TEST_POSTGRES_CONNECTION_STRING")
            ?? throw new InvalidOperationException("STACKBRAID_TEST_POSTGRES_CONNECTION_STRING is not set.");

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("ConnectionStrings:Postgres", connectionString);
            builder.UseSetting("Jwt:SigningKey", "MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTIzNDU2Nzg5MDE=");
            builder.UseSetting("Jwt:Issuer", "stackbraid-tests");
            builder.UseSetting("Jwt:Audience", "stackbraid-tests");
            builder.UseSetting("Jobs:ConformanceEnabled", "true");
            builder.UseSetting("Jobs:PollIntervalMilliseconds", "50");
            builder.UseSetting("Jobs:BaseRetryDelayMilliseconds", "50");
        });
    }

    [Fact]
    public async Task A_jobs_worker_span_and_log_line_carry_the_enqueuing_requests_correlation_id_and_trace_id()
    {
        var capturedOutput = new StringBuilder();
        var originalOut = Console.Out;
        var correlationId = Guid.NewGuid().ToString("N");
        Guid jobId;
        string requestTraceId = string.Empty;
        string jobTraceId = string.Empty;
        string jobLogLine = string.Empty;

        try
        {
            Console.SetOut(new SpyTextWriter(capturedOutput, originalOut));

            using var client = _factory.CreateClient();
            var (accessToken, _) = await RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var enqueue = new HttpRequestMessage(HttpMethod.Post, "/v1/jobs/scenarios");
            enqueue.Headers.Add("X-Correlation-Id", correlationId);
            enqueue.Content = JsonContent.Create(new { scenario = "succeeds" });
            var enqueueResponse = await client.SendAsync(enqueue);
            enqueueResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            jobId = (await enqueueResponse.Content.ReadFromJsonAsync<EnqueueResponse>())!.JobId;

            // A second job that always fails, so the failure counter is exercised too.
            using var failing = new HttpRequestMessage(HttpMethod.Post, "/v1/jobs/scenarios");
            failing.Headers.Add("X-Correlation-Id", correlationId);
            failing.Content = JsonContent.Create(new { scenario = "dead-letter" });
            (await client.SendAsync(failing)).StatusCode.ShouldBe(HttpStatusCode.Accepted);

            await WaitForTerminalAsync(client, jobId);
            await Task.Delay(500);

            List<Activity> activities;
            lock (_capturedActivities)
            {
                activities = [.. _capturedActivities];
            }

            var requestActivity = activities.FirstOrDefault(activity =>
                !activity.DisplayName.StartsWith("job.execute", StringComparison.Ordinal)
                && Equals(activity.GetTagItem(JobTelemetry.CorrelationIdTag), correlationId));
            requestActivity.ShouldNotBeNull("the enqueuing request's own span should carry the correlation ID");
            requestTraceId = requestActivity!.TraceId.ToString();

            var jobActivity = activities.FirstOrDefault(activity =>
                activity.DisplayName.StartsWith("job.execute", StringComparison.Ordinal)
                && Equals(activity.GetTagItem(JobTelemetry.CorrelationIdTag), correlationId));
            jobActivity.ShouldNotBeNull("the worker's span should carry the request's correlation ID");
            jobTraceId = jobActivity!.TraceId.ToString();

            jobLogLine = capturedOutput.ToString()
                .Split('\n')
                .FirstOrDefault(line => line.Contains(jobId.ToString(), StringComparison.OrdinalIgnoreCase)
                    && line.Contains("succeeded", StringComparison.Ordinal)) ?? string.Empty;
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        // The worker's span joins the request's trace, not a detached one.
        jobTraceId.ShouldBe(requestTraceId);

        // The one log line that names this job also names the request's
        // correlation ID and the trace it belongs to.
        jobLogLine.ShouldNotBeNullOrWhiteSpace("the worker should have logged the completed job");
        jobLogLine.ShouldContain(correlationId);
        jobLogLine.ShouldContain(requestTraceId);
        jobLogLine.ShouldContain("succeeded");

        // The duration histogram and the failure counter are emitted by the worker.
        _capturedMeasurements.ShouldContain(m =>
            m.Instrument == "stackbraid.jobs.duration"
            && m.Tags.GetValueOrDefault(JobTelemetry.JobTypeTag) as string == ConformanceSucceedsJobHandler.Type
            && m.Tags.GetValueOrDefault(JobTelemetry.JobOutcomeTag) as string == JobTelemetry.SucceededOutcome);
        _capturedMeasurements.ShouldContain(m =>
            m.Instrument == "stackbraid.jobs.failures"
            && m.Tags.GetValueOrDefault(JobTelemetry.JobTypeTag) as string == ConformanceDeadLetterJobHandler.Type
            && m.Tags.GetValueOrDefault(JobTelemetry.FailureReasonTag) as string == JobTelemetry.HandlerErrorReason);
    }

    public void Dispose()
    {
        _activityListener.Dispose();
        _meterListener.Dispose();
        _factory.Dispose();
    }

    private void RecordMeasurement(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? _)
    {
        var mapped = new Dictionary<string, object?>();
        foreach (var tag in tags)
        {
            mapped[tag.Key] = tag.Value;
        }

        lock (_capturedMeasurements)
        {
            _capturedMeasurements.Add(new Measurement(instrument.Name, value, mapped));
        }
    }

    private static async Task WaitForTerminalAsync(HttpClient client, Guid jobId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            var status = await client.GetFromJsonAsync<JobStatusBody>($"/v1/jobs/{jobId}");
            if (status?.Status is "succeeded" or "dead-lettered")
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Job {jobId} did not reach a terminal state in time.");
    }

    private static async Task<(string AccessToken, string Email)> RegisterAndLoginAsync(HttpClient client)
    {
        var email = $"job-observability-{Guid.NewGuid():N}@example.com";
        const string password = "Sup3rSecretPassw0rd!!";

        var register = await client.PostAsJsonAsync("/v1/auth/register", new RegisterRequest(email, password, "Job Observability"));
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/v1/auth/login", new LoginRequest(email, password));
        login.EnsureSuccessStatusCode();
        var tokens = (await login.Content.ReadFromJsonAsync<TokenPairResponse>())!;
        return (tokens.AccessToken, email);
    }

    private sealed record EnqueueResponse(Guid JobId);

    private sealed record JobStatusBody(string Status, int Attempts);

    private sealed record TokenPairResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);

    private sealed record Measurement(string Instrument, double Value, IReadOnlyDictionary<string, object?> Tags);

    /// <summary>Tees every write into <paramref name="buffer"/> while still forwarding to the real console, so `dotnet test`'s own output isn't silenced during this test.</summary>
    private sealed class SpyTextWriter(StringBuilder buffer, TextWriter inner) : TextWriter
    {
        public override Encoding Encoding => inner.Encoding;

        public override void Write(char value)
        {
            lock (buffer) buffer.Append(value);
            inner.Write(value);
        }

        public override void Write(string? value)
        {
            if (value is null) return;
            lock (buffer) buffer.Append(value);
            inner.Write(value);
        }
    }
}
