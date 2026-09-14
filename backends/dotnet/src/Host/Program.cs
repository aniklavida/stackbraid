using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackBraid.Database.Postgres;
using StackBraid.Features.Identity.Application.Abstractions;
using StackBraid.Features.Identity.Endpoints;
using StackBraid.Host.HealthChecks;
using StackBraid.Host.Observability;
using StackBraid.Host.Security;
using StackBraid.Shared;
using StackBraid.Shared.Web;

var builder = WebApplication.CreateBuilder(args);

// Configured entirely in code — no Serilog.Settings.Configuration package,
// so Logging:LogLevel in appsettings.json governs the ASP.NET Core
// framework logger, and this governs Serilog's own minimum level.
builder.Host.UseSerilog((_, loggerConfiguration) => loggerConfiguration
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"));

builder.Services.AddStackBraidObservability(builder.Configuration);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

builder.Services.AddShared();
builder.Services.AddPostgresPersistence(connectionString);
// Every handler here takes a Scoped repository (itself backed by a Scoped
// DbContext) through its constructor. The library's own default is
// Singleton — its README recommends it for raw throughput — but a
// singleton handler is constructed once and holds whatever Scoped
// repository was active at that moment forever after, so every later
// request reuses the same DbContext concurrently. That is exactly what
// surfaced, twice, as an intermittent
// "a second operation was started on this context instance" exception
// under concurrent load, and is also why `WebApplicationFactory`-based
// tests (which validate the service graph on build, unlike a plain
// `dotnet run`) failed outright the moment one was tried. Handlers hold no
// state of their own beyond what DI gives them, so Scoped is free of that
// problem and correct for this graph.
builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
builder.Services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("The Jwt configuration section is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtOptions.SigningKey)),
            // No tolerance past the token's own `exp` — the contract's
            // conformance suite proves an expired token is rejected the
            // instant it expires, not up to a grace window later.
            ClockSkew = TimeSpan.Zero,
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = JwtProblemDetailsEvents.OnChallengeAsync,
            OnForbidden = JwtProblemDetailsEvents.OnForbiddenAsync,
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("postgres", tags: ["ready"]);

// No frontend origin is trusted by default — a frontend must be listed
// explicitly (Cors:AllowedOrigins, e.g. the Next.js or Angular dev server)
// before its browser requests are allowed to carry the httpOnly refresh
// cookie. An empty list keeps today's behaviour: no cross-origin access.
var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
{
    if (corsAllowedOrigins.Length > 0)
    {
        policy.WithOrigins(corsAllowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    }
}));

var app = builder.Build();

app.UseSharedWeb();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

app.MapIdentityEndpoints();

await app.Services.MigratePostgresDatabaseAsync();
await PostgresSeeder.SeedAsync(app.Services);

app.Run();

// Exposed for WebApplicationFactory<Program> in integration tests.
public partial class Program;
