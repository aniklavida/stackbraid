using StackBraid.Shared;
using StackBraid.Shared.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddShared();

var app = builder.Build();

app.UseSharedWeb();

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));

app.Run();

// Exposed for WebApplicationFactory<Program> in integration tests.
public partial class Program;
