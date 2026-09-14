using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using StackBraid.Features.Identity.Contracts.Requests;

namespace StackBraid.Features.Identity.IntegrationTests;

/// <summary>
/// No secret — a password, a refresh token, an <c>Authorization</c> header
/// — may ever reach a log line or an exported span attribute. This drives
/// every place a secret travels in the real Identity flow (register,
/// login, refresh via cookie, an authenticated call, logout via bearer
/// token) against the real running pipeline (real Postgres, real JWT
/// issuance, real OpenTelemetry instrumentation, real Serilog output) and
/// inspects everything the process actually recorded — not a mock standing
/// in for "what it probably does".
///
/// Two independent capture points, both wired additively (nothing here
/// replaces the app's own logging or tracing configuration):
///  - Every byte written to <see cref="Console.Out"/> for the duration of
///    the request — which is where both Serilog's console sink and
///    OpenTelemetry's console span/metric exporters actually write, so
///    this is what an operator watching this process's real output would
///    see, not a mock standing in for it.
///  - A process-wide <see cref="ActivityListener"/> — independent of, and
///    additional to, the OpenTelemetry SDK's own listener on the same
///    activity sources — recording every span's own tags directly,
///    structurally, rather than however the console exporter chooses to
///    render them as text.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class SecretRedactionTests : IClassFixture<PostgresDatabaseFixture>, IDisposable
{
    private readonly PostgresDatabaseFixture _postgres;
    private readonly List<Activity> _capturedActivities = [];
    private readonly ActivityListener _activityListener;
    private readonly WebApplicationFactory<Program> _factory;

    public SecretRedactionTests(PostgresDatabaseFixture postgres)
    {
        _postgres = postgres;

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

        var connectionString = Environment.GetEnvironmentVariable("STACKBRAID_TEST_POSTGRES_CONNECTION_STRING")
            ?? throw new InvalidOperationException("STACKBRAID_TEST_POSTGRES_CONNECTION_STRING is not set.");

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("ConnectionStrings:Postgres", connectionString);
            builder.UseSetting("Jwt:SigningKey", "MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTIzNDU2Nzg5MDE=");
            builder.UseSetting("Jwt:Issuer", "stackbraid-tests");
            builder.UseSetting("Jwt:Audience", "stackbraid-tests");
        });
    }

    [Fact]
    public async Task No_secret_ever_appears_in_a_log_line_or_a_span_attribute()
    {
        var capturedOutput = new StringBuilder();
        var captureWriter = new SpyTextWriter(capturedOutput, Console.Out);
        var originalOut = Console.Out;
        Console.SetOut(captureWriter);

        HttpResponseMessage refreshResponse;
        TokenPairResponse tokens;
        string cookieValue;
        const string password = "Sup3rSecretPassw0rd!!";

        try
        {
            using var client = _factory.CreateClient();

            var email = $"redaction-{Guid.NewGuid():N}@example.com";

            var registerResponse = await client.PostAsJsonAsync("/v1/auth/register", new RegisterRequest(email, password, "Redaction Test"));
            registerResponse.EnsureSuccessStatusCode();

            var loginResponse = await client.PostAsJsonAsync("/v1/auth/login", new LoginRequest(email, password));
            loginResponse.EnsureSuccessStatusCode();
            tokens = (await loginResponse.Content.ReadFromJsonAsync<TokenPairResponse>())!;
            var setCookie = loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies)
                ? cookies.FirstOrDefault(c => c.StartsWith("refreshToken=", StringComparison.Ordinal))
                : null;
            setCookie.ShouldNotBeNull();
            cookieValue = setCookie!.Split(';')[0];

            using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/auth/refresh");
            refreshRequest.Headers.Add("Cookie", cookieValue);
            refreshResponse = await client.SendAsync(refreshRequest);
            refreshResponse.EnsureSuccessStatusCode();

            using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/v1/auth/me");
            meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
            var meResponse = await client.SendAsync(meRequest);
            meResponse.EnsureSuccessStatusCode();

            using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/auth/logout");
            logoutRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
            var logoutResponse = await client.SendAsync(logoutRequest);
            logoutResponse.EnsureSuccessStatusCode();

            // Give the console exporter's periodic metric reader and any
            // fire-and-forget span export a moment, and make sure every
            // `ActivityStopped` callback above has actually run — spans
            // close asynchronously relative to the response being written.
            await Task.Delay(500);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        var capturedText = capturedOutput.ToString();

        var secrets = new[]
        {
            password,
            cookieValue,
            tokens.AccessToken,
            tokens.RefreshToken,
            $"Bearer {tokens.AccessToken}",
        };

        foreach (var secret in secrets)
        {
            if (capturedText.Contains(secret, StringComparison.Ordinal))
            {
                throw new Exception($"the process's own console output (Serilog and/or the OpenTelemetry console exporter) leaked a secret value: {secret}");
            }
        }

        List<Activity> activitiesSnapshot;
        lock (_capturedActivities)
        {
            activitiesSnapshot = [.. _capturedActivities];
        }

        foreach (var secret in secrets)
        {
            foreach (var activity in activitiesSnapshot)
            {
                foreach (var tag in activity.TagObjects)
                {
                    var tagValue = tag.Value?.ToString() ?? string.Empty;
                    if (tagValue.Contains(secret, StringComparison.Ordinal))
                    {
                        throw new Exception($"span '{activity.DisplayName}' tag '{tag.Key}' leaked a secret value: {tagValue}");
                    }
                }
            }
        }

        // The correlation ID itself is meant to travel everywhere — proving
        // it actually reached the captured output is what makes the
        // absence of a secret above meaningful, rather than the capture
        // simply having caught nothing at all (for example because
        // Serilog's console sink was buffering, or wasn't writing through
        // the swapped `Console.Out` for some reason).
        var correlationId = refreshResponse.Headers.TryGetValues("X-Correlation-Id", out var ids) ? ids.FirstOrDefault() : null;
        correlationId.ShouldNotBeNullOrWhiteSpace();
        capturedText.ShouldContain(correlationId!, customMessage: "expected the request's own correlation ID to appear somewhere in the process's real console output (the per-request structured log line)");
    }

    public void Dispose()
    {
        _activityListener.Dispose();
        _factory.Dispose();
    }

    private sealed record TokenPairResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);

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
