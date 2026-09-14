"""One instrumentation layer the operator can point anywhere, rather than a
hard wiring to one vendor — the Python-side counterpart to the .NET
backend's ``Host/Observability/OpenTelemetryExtensions.cs``: traces and
metrics always go to the console (verifiable locally with no collector),
and are *additionally* exported over OTLP only when an endpoint is
actually configured — this process never guesses at or dials a collector
nobody asked it to.
"""

from __future__ import annotations

from fastapi import FastAPI
from opentelemetry import metrics, trace
from opentelemetry.exporter.otlp.proto.http.metric_exporter import OTLPMetricExporter
from opentelemetry.exporter.otlp.proto.http.trace_exporter import OTLPSpanExporter
from opentelemetry.instrumentation.fastapi import FastAPIInstrumentor
from opentelemetry.sdk.metrics import MeterProvider
from opentelemetry.sdk.metrics.export import ConsoleMetricExporter, PeriodicExportingMetricReader
from opentelemetry.sdk.resources import SERVICE_NAME, Resource
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor, ConsoleSpanExporter, SimpleSpanProcessor

SERVICE_NAME_VALUE = "stackbraid-python-backend"

# The two liveness/readiness probes are polled constantly by nothing more
# interesting than a developer's terminal or a future orchestrator —
# tracing them forever would drown the one signal this is actually for:
# real requests. Matches the .NET backend's own `AddAspNetCoreInstrumentation`
# filter.
_EXCLUDED_URLS = "health/live,health/ready"


def configure_opentelemetry(otlp_endpoint: str) -> None:
    resource = Resource.create({SERVICE_NAME: SERVICE_NAME_VALUE})

    tracer_provider = TracerProvider(resource=resource)
    tracer_provider.add_span_processor(SimpleSpanProcessor(ConsoleSpanExporter()))
    if otlp_endpoint:
        tracer_provider.add_span_processor(BatchSpanProcessor(OTLPSpanExporter(endpoint=otlp_endpoint)))
    trace.set_tracer_provider(tracer_provider)

    metric_readers = [PeriodicExportingMetricReader(ConsoleMetricExporter(), export_interval_millis=5_000)]
    if otlp_endpoint:
        metric_readers.append(PeriodicExportingMetricReader(OTLPMetricExporter(endpoint=otlp_endpoint), export_interval_millis=5_000))
    meter_provider = MeterProvider(resource=resource, metric_readers=metric_readers)
    metrics.set_meter_provider(meter_provider)


def instrument_app(app: FastAPI) -> None:
    FastAPIInstrumentor.instrument_app(app, excluded_urls=_EXCLUDED_URLS)
