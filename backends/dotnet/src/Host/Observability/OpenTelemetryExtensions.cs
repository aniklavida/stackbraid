using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace StackBraid.Host.Observability;

/// <summary>
/// One instrumentation layer the operator can point anywhere, rather than a
/// hard wiring to one vendor: traces and metrics always go to the console
/// (or wherever the process's own stdout is redirected — a plain file
/// redirect is a file exporter for every practical local-verification
/// purpose, so no bespoke file-writing exporter is built here), and are
/// <em>additionally</em> exported over OTLP only when
/// <c>Otel:OtlpEndpoint</c> is actually configured — this process never
/// guesses at or dials a collector nobody asked it to.
/// </summary>
public static class OpenTelemetryExtensions
{
    public const string ServiceName = "stackbraid-dotnet-backend";

    public static IServiceCollection AddStackBraidObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var otlpEndpoint = configuration["Otel:OtlpEndpoint"];
        var hasOtlpEndpoint = !string.IsNullOrWhiteSpace(otlpEndpoint);

        var otelBuilder = services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        // The two liveness/readiness probes are polled
                        // constantly by nothing more interesting than a
                        // developer's terminal or a future orchestrator —
                        // tracing them forever would drown the one signal
                        // this is actually for: real requests.
                        options.Filter = httpContext =>
                            !httpContext.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation()
                    .AddConsoleExporter();

                if (hasOtlpEndpoint)
                {
                    tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint!));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddConsoleExporter((_, readerOptions) =>
                    {
                        readerOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 5_000;
                    });

                if (hasOtlpEndpoint)
                {
                    metrics.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint!));
                }
            });

        _ = otelBuilder;
        return services;
    }
}
