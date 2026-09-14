using Mediator;
using Microsoft.Extensions.DependencyInjection;

// Every handler here takes a Scoped repository (backed by a Scoped
// DbContext) through its constructor. The library defaults to Singleton
// handlers for raw throughput, but a singleton handler is constructed once
// and holds whatever Scoped repository was active at that moment forever
// after — every later request would then reuse the same DbContext
// concurrently. This assembly attribute is what actually decides the
// lifetime the source generator emits; `Host/Program.cs`'s own
// `AddMediator(options => ...)` call must ask for the same lifetime or the
// generated registration code rejects the mismatch at startup.
[assembly: MediatorOptions(ServiceLifetime = ServiceLifetime.Scoped)]
